using SAPbobsCOM;
using System;
using Workbench.Agent.BusinessOne.Models;
using Workbench.Agent.BusinessOne.WorkbenchClient;

namespace Workbench.Agent.BusinessOne.Integrations
{
    public class ImportJobReceipts : ImportBase
    {
        public ImportJobReceipts(Client wbClient, Company sapCompany, WorkbenchTrfClient wbTrfclient) :
         base(wbClient, sapCompany, wbTrfclient)
        {
        }

        public override string Import()
        {
            var lastUpdateDate = SettingsModelList.GetUpdateDate("LastJobReceiptsSyncDate");
            var result = ImportProcess(lastUpdateDate, sapCompany);
            SettingsModelList.SetUpdateDate("LastJobReceiptsSyncDate", DateTime.Now);
            return result;
        }

        private string ImportProcess(DateTime lastUpdateDate, Company sapCompany)
        {
            int importedJobReiptsCount = 0;
            var recordset = (Recordset)sapCompany.GetBusinessObject(BoObjectTypes.BoRecordset);
            recordset.DoQuery($@"SELECT DISTINCT RCT2.DocNum as ExternalID,
                                                RCT2.InvoiceID ,
                                                CONCAT (RCT2.DocNum ,  '-' ,  RCT2.InvoiceID) AS ReceiptNumber,
                                                OINV.DocNum as InvoiceNumber,
                                                ORCT.DocDate as ReceiptDate,
                                                LEFT(ORCT.TrsfrRef, 20) as ReceiptReference,
                                                ROUND(RCT2.SumApplied / ( 1 - ( RCT2.Dcount / 100 ) ), 2) as Amount,
                                                CASE
                                                  WHEN ORCT.Canceled = 'Y' THEN 1
                                                  ELSE 0
                                                END as Status,
                                                0
                                FROM   ORCT
                                       JOIN RCT2 ON RCT2.DocNum = ORCT.DocNum
                                       JOIN OINV ON OINV.DocEntry = RCT2.DocEntry
                                       JOIN INV1 ON INV1.DocEntry = OINV.DocEntry 
                                WHERE  RCT2.InvType = 13 and ORCT.UpdateDate >= '{lastUpdateDate.ToString(sapDateFormat)}'
                                     AND INV1.Project > '' ");
            while (!recordset.EoF)
            {
                var request = BuildRequest(recordset);

                try
                {
                    var result = wbTrfclient.JobReceiptTrfApi_PostAsync(request);
                    importedJobReiptsCount++;
                }
                catch (Exception ex)
                {
                    Helpers.LogAppError($"Error importing job receipt: {request.ExternalID} \r\n{ex}");
                }

                recordset.MoveNext();
            };

            return $"ImportJobReceipts; Total count to be imported: {recordset.RecordCount}. \r\nTotal count successfully imported: {importedJobReiptsCount}";
        }

        private Transfer_JobReceiptTrfApiModel BuildRequest(Recordset recordset)
        {
            var request = new Transfer_JobReceiptTrfApiModel();
            request.FinCoCode = GetFinCocode();
            request.JobReceiptID = (int)recordset.Fields.Item("ExternalID").Value;
            request.ExternalID = recordset.Fields.Item("ExternalID").Value?.ToString();
            request.ReceiptNumber = recordset.Fields.Item("ReceiptNumber").Value?.ToString();
            request.InvoiceNumber = (int)recordset.Fields.Item("InvoiceNumber").Value;
            request.ReceiptReference = recordset.Fields.Item("ReceiptReference").Value?.ToString();
            request.ReceiptDate = (DateTime)recordset.Fields.Item("ReceiptDate").Value;
            request.Amount = (double)recordset.Fields.Item("Amount").Value; 
            return request;

        }
    }
}
