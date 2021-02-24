using SAPbobsCOM;
using System;
using System.Collections.Generic;
using System.Linq;
using Workbench.Agent.BusinessOne.Models;
using Workbench.Agent.BusinessOne.WorkbenchClient;

namespace Workbench.Agent.BusinessOne.Integrations
{
    public class ExportGLJournals : ExportBase
    {
        public ExportGLJournals(Client wbClient, Company sapCompany, WorkbenchTrfClient wbTrfclient) :
         base(wbClient, sapCompany, wbTrfclient)
        {
        }

        public override string Export(int batchNo)
        {
            var result = ExportProcess(batchNo);
            SettingsModelList.SetUpdateDate("LastGLJournalsSyncDate", DateTime.Now);
            return result;
        }

        public string Export(int batchNo, string wbSessionId)
        {
            this.wbSessionId = wbSessionId;
            var result = ExportProcess(batchNo);
            SettingsModelList.SetUpdateDate("LastGLJournalsSyncDate", DateTime.Now);
            return result;
        }

        private string ExportProcess(int batchNo)
        {
            var apiResult = wbTrfclient.GLJournalTrfApi_GetAsync(batchNo);
            var glJournals = apiResult.Result;

            Helpers.LogInfo($"{batchNo}\r\nTotal Count new: {glJournals.Count}");

            int insertedRecords = 0;
            int existingRecords = 0;

            var oCompServ = sapCompany.GetCompanyService();
            var projectService = oCompServ.GetBusinessService(ServiceTypes.ProjectsService);

            var groupJournals = glJournals.GroupBy(a => new { a.BatchNo, a.PostingDate });
            if (Validate(glJournals, batchNo))
            {
                foreach (var journal in groupJournals)
                {
                    try
                    {
                        //if (GLBatchExists(journal.FirstOrDefault().BatchNo.Value))
                        //{
                        //    ExportLogTrf(batchNo, batchNo, "GLJournals", Type2.Error, $"{batchNo}\r\nGL Journal Batch {journal.FirstOrDefault().BatchNo.Value} already exists in SBO");
                        //    continue;
                        //}

                        var journalEntries = (JournalEntries)sapCompany.GetBusinessObject(BoObjectTypes.oJournalEntries);
                        journalEntries.ReferenceDate = Convert.ToDateTime(journal.FirstOrDefault().PostingDate?.ToString(sapDateFormat));
                        journalEntries.Reference = $"WB:{journal.FirstOrDefault().BatchNo.Value}";
                        journalEntries.UserFields.Fields.Item("U_WB_Batch").Value = journal.FirstOrDefault().BatchNo.Value;
                        var journalEntriesLines = journalEntries.Lines;

                        int lineNo = 1;
                        foreach (var line in journal)
                        {

                            try
                            {
                                var glCode = GetGLCode(line.GLCode);

                                //if (glCode == "")
                                //{
                                //    ExportLogTrf(batchNo, batchNo, "GLJournals", Type2.Error, $"{batchNo}\r\nGL Account {line.GLCode} not found gl journal {line.ID}");
                                //    continue;
                                //}
                                journalEntriesLines.AccountCode = glCode;
                                journalEntriesLines.ShortName = glCode;
                                journalEntriesLines.ProjectCode = line.Analysis2;
                                if (Convert.ToDouble(line.Amount.ToString()) >= 0)
                                {

                                    journalEntriesLines.Debit = Math.Round(Convert.ToDouble(line.Amount.ToString()), 2);
                                }
                                else
                                {
                                    journalEntriesLines.Credit = Math.Round(Math.Abs(Convert.ToDouble(line.Amount.ToString())), 2);
                                }

                                journalEntriesLines.Add();
                                journalEntriesLines.SetCurrentLine(lineNo);
                                lineNo++;
                            }

                            catch (Exception)
                            {
                                ExportLogTrf(batchNo, batchNo, "GLJournals", Type2.Error, $"{batchNo}\r\nError exporting gl journal : {line.ID}");
                            }
                        }

                        if (journalEntries.Add() != 0)
                        {
                            var error = sapCompany.GetLastErrorDescription();
                            ExportLogTrf(batchNo, batchNo, "GLJournals", Type2.Error, $"{batchNo}\r\nError from SBO : {error}. For batch {journal.FirstOrDefault().BatchNo.Value}");
                        }
                        else
                        {
                            insertedRecords++;
                        }

                    }
                    catch (Exception)
                    {
                        ExportLogTrf(batchNo, batchNo, "GLJournals", Type2.Error, $"{batchNo}\r\nError exporting gl journal batch : {journal.FirstOrDefault().BatchNo.Value}");

                    }
                }
            }

            if (groupJournals?.ToList().Count == insertedRecords && groupJournals?.ToList().Count != 0)
            {
                ExportLogTrf(batchNo, batchNo, "GLJournals", Type2.Info, $"{batchNo}\r\nSuccess");
                TransferLogTrf(wbSessionId, batchNo, Type.Info);
            }
            else
            {
                TransferLogTrf(wbSessionId, batchNo, Type.Error);
            }
            return $"{batchNo}\r\nTotal count to be exported: {glJournals.Count}. \r\nTotal count successfully exported: {insertedRecords + existingRecords}. \r\nWith {insertedRecords} newly inserted";
        }

        private bool Validate(ICollection<Transfer_GLJournalTrfApiModel> glJournals, int batchNo)
        {
            var groupJournals = glJournals.GroupBy(a => new { a.BatchNo, a.PostingDate });

            var errorCount = 0;
            foreach (var journal in groupJournals)
            {
                if (GLBatchExists(journal.FirstOrDefault().BatchNo.Value))
                {
                    errorCount++;
                    ExportLogTrf(batchNo, batchNo, "GLJournals", Type2.Error, $"{batchNo}\r\nGL Journal Batch {journal.FirstOrDefault().BatchNo.Value} already exists in SBO");
                    continue;
                }

                foreach (var line in journal)
                {
                    var glCode = GetGLCode(line.GLCode);
                    if (glCode == "")
                    {
                        errorCount++;
                        ExportLogTrf(batchNo, batchNo, "GLJournals", Type2.Error, $"{batchNo}\r\nGL Account {line.GLCode} not found gl journal {line.ID}");
                        continue;
                    }
                }
            }

            return errorCount == 0;
        }
    }
}
