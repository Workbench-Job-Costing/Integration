using Nancy;
using System;
using System.Net.Http;
using Workbench.Agent.BusinessOne.Models;

namespace Workbench.Agent.BusinessOne.Integrations
{
    public class ExportProcessBatch : NancyModule
    {
        private readonly ExportJobs exportJobs;
        private readonly ExportGLJournals exportGLJournals;
        private readonly ExportAPInvoices exportAPInvoices;
        private readonly ExportARInvoices exportARInvoices;

        private string[] batches;
        private string type;
        private string sessionId;
        private string exportTable;
        public ExportProcessBatch(ExportJobs exportJobs, 
            ExportGLJournals exportGLJournals,
            ExportAPInvoices exportAPInvoices,
            ExportARInvoices exportARInvoices)
        {
            this.exportJobs = exportJobs;
            this.exportGLJournals = exportGLJournals;
            this.exportAPInvoices = exportAPInvoices;
            this.exportARInvoices = exportARInvoices;
        }
        public string ProcessBatch(string message)
        {
          
            var exportString = message.Split(':');
            var batchList = exportString[0];
            sessionId = exportString[1];
            var exportUserId = exportString[2];
            batches = batchList.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            try
            {
                var finCoCode = exportString[3];
                var settingFinCoCode = SettingsModelList.GetFinCoCode();
                if (settingFinCoCode != finCoCode)
                {
                    Helpers.LogAppError($"Invalid FinCoCode settings:{settingFinCoCode}, singalR: {finCoCode}");
                    batchList = "0";
                    return "Invalid FinCo";
                }
          
                foreach (var batch in batches)
                {
                    var batchNo = Convert.ToInt32(batch);
                    type = exportAPInvoices.GetBatchType(batchNo);
                    var result = "";

                    if (type == "ApInvoice" || type == "ApCredit" || type == "SubClaim")
                    {
                        result = exportAPInvoices.Export(batchNo, sessionId);
                    }

                    if (type == "ArInvoice" || type == "ArCredit")
                    {
                        result = exportARInvoices.Export(batchNo, sessionId);
                    }

                    if (type == "Adjustment" || type == "WipAccrual" || type == "CccAccrual" || type == "PoAccrual")
                    {
                        result = exportGLJournals.Export(batchNo, sessionId);
                    }

                    if (type == "Disbursement" || type == "PlantIssue" || type == "Docket" || type == "Timesheet")
                    {
                        result = exportGLJournals.Export(batchNo, sessionId);
                    }

                    if (type == "StockReturn" || type == "StockIssue" || type == "StockTrf" || type == "StockAdjust")
                    {
                        result = exportGLJournals.Export(batchNo, sessionId);
                    }

                    if(result == "")
                    {
                        Helpers.LogAppError($"Batch type \"{type}\" is not supported yet");
                        var wbTrfClient = new WorkbenchTrfClient(new HttpClient());
                        _ = wbTrfClient.LogTrfApi_Post2Async(batchNo, exportId:null, exportTable:null, Type2.Error, $"Batch type \"{type}\" is not supported yet").Result;
                        _ = wbTrfClient.LogTrfApi_PostAsync(sessionId, "Export log marked as error", batchNo, Type.Error).Result;
                    }
                   
                    Helpers.LogInfo($"type: {type}, result: {result}");
                }
                return "success";
            }
            catch (Exception ex)
            {
                foreach (var batch in batches)
                {
                    var wbTrfClient = new WorkbenchTrfClient(new HttpClient());
                    var batchNo = Convert.ToInt32(batch);
                    type = exportAPInvoices.GetBatchType(batchNo);

                    if (type == "ApInvoice" || type == "ApCredit")
                    {
                        var apiResult = wbTrfClient.APInvoiceTrfApi_GetAsync(batchNo);
                        var apInvoices = apiResult.Result;
                        exportTable = "APInvoices";
                        foreach (var invoice in apInvoices)
                        {
                            _ = wbTrfClient.LogTrfApi_Post2Async(batchNo, invoice.ID.Value, exportTable, Type2.Error, $"Error exporting batch {ex.Message}").Result;
                        }
                        _ = wbTrfClient.LogTrfApi_PostAsync(sessionId, "Export log marked as error", batchNo, Type.Error).Result;
                    }
                    if (type == "ArInvoice" || type == "ArCredit")
                    {
                        var apiResult = wbTrfClient.ARInvoiceTrfApi_GetAsync(batchNo);
                        var arInvoices = apiResult.Result;
                        exportTable = "ARInvoices";
                        foreach (var invoice in arInvoices)
                        {
                            _ = wbTrfClient.LogTrfApi_Post2Async(batchNo, invoice.ID.Value, exportTable, Type2.Error, $"Error exporting batch {ex.Message}").Result;
                        }
                        _ = wbTrfClient.LogTrfApi_PostAsync(sessionId, "Export log marked as error", batchNo, Type.Error).Result;
                    }
                    if (type == "Adjustment" || type == "WipAccrual" || type == "CccAccrual" || type == "PoAccrual")
                    {
                        exportTable = "GLJournals";
                        _ = wbTrfClient.LogTrfApi_Post2Async(batchNo, batchNo, exportTable, Type2.Error, $"Error exporting batch {ex.Message}").Result;
                    }

                    if (type == "Disbursement" || type == "PlantIssue" || type == "Docket" || type == "Timesheet")
                    {
                        exportTable = "GLJournals";
                        _ = wbTrfClient.LogTrfApi_Post2Async(batchNo, batchNo, exportTable, Type2.Error, $"Error exporting batch {ex.Message}").Result;
                    }

                    if (type == "StockReturn" || type == "StockIssue" || type == "StockTrf" || type == "StockAdjust")
                    {
                        exportTable = "GLJournals";
                        _ = wbTrfClient.LogTrfApi_Post2Async(batchNo, batchNo, exportTable, Type2.Error, $"Error exporting batch {ex.Message}").Result;
                    }
                }

                Helpers.LogAppError($"catch {ex.InnerException.Message}");
                _ = exportAPInvoices.wbTrfclient.BatchTrfApi_PostAsync(batchList, Convert.ToInt32(exportUserId)).Result;

            }
            finally
            {
                Helpers.LogInfo($"finally {batchList}");
                _ = exportAPInvoices.wbTrfclient.BatchTrfApi_PostAsync(batchList, Convert.ToInt32(exportUserId)).Result;

            }

            Helpers.LogInfo($"exit {batchList}");
            _ = exportAPInvoices.wbTrfclient.BatchTrfApi_PostAsync(batchList, Convert.ToInt32(exportUserId)).Result;
            return "exit";
        }
    }
}
