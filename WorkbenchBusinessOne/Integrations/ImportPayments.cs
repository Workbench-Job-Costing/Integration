using SAPbobsCOM;
using System;
using Workbench.Agent.BusinessOne.Models;
using Workbench.Agent.BusinessOne.WorkbenchClient;

namespace Workbench.Agent.BusinessOne.Integrations
{
    public class ImportPayments : ImportBase
    {
        public ImportPayments(Client wbClient, Company sapCompany, WorkbenchTrfClient wbTrfclient) :
         base(wbClient, sapCompany, wbTrfclient)
        {
        }

        public override string Import()
        {
            var lastUpdateDate = SettingsModelList.GetUpdateDate("LastPaymentSyncDate");
            var result = ImportProcess(lastUpdateDate, sapCompany);
            SettingsModelList.SetUpdateDate("LastPaymentSyncDate", DateTime.Now);
            return result;
        }

        private string ImportProcess(DateTime lastUpdateDate, Company sapCompany)
        {
            int importedInvoicePayment = 0;
            var recordset = (Recordset)sapCompany.GetBusinessObject(BoObjectTypes.BoRecordset);
            recordset.DoQuery($@"SELECT VPM2.DocNum as vpm2DocNum,
                                       VPM2.InvoiceID as InvoiceID,
                                       OPCH.DocNum as opchDocNum,
                                       OVPM.DocDate,
                                       LEFT(OVPM.TrsfrRef, 20) as TrsfrRef,
                                       round(VPM2.AppliedSys / (1 - (VPM2.Dcount / 100)), 2) as Amount,
                                       round(VPM2.AppliedFC / (1 - (VPM2.Dcount / 100)), 2) as FCAmount, 
                                       OVPM.DocRate as ExchangeRate,
	                                   OPCH.UpdateDate,
                                       OPCH.U_WB_ID_NEW as ApInvoiceId
                                FROM   OVPM
                                       JOIN VPM2 ON VPM2.DocNum = OVPM.DocNum
                                       JOIN OPCH ON OPCH.DocEntry = VPM2.DocEntry 
                                --WHERE OPCH.UpdateDate > '{lastUpdateDate.ToString(sapDateFormat)}' --based on Invoice date instead of payment date
                                WHERE OVPM.DocDate >= '{lastUpdateDate.ToString(sapDateFormat)}'
                                        AND LEFT(OPCH.U_WB_ID_NEW, 3) = 'AP:'"); //comment this for testing current data

            while (!recordset.EoF)
            {
                var request = BuildRequest(recordset);

                try
                {
                    var result = wbTrfclient.PaymentTrfApi_PostAsync(request);
                    importedInvoicePayment++;
                }
                catch (Exception ex)
                {
                    Helpers.LogAppError($"Error importing invoice payment: {request.PaymentID} \r\n{ex}");
                }

                recordset.MoveNext();
            };

            Helpers.LogInfo($"ImportPayments; Total count to be invoice imported: {recordset.RecordCount}. \r\nTotal count successfully imported:{importedInvoicePayment}");


            int importedCreditPayment = 0;
            var creditRecordset = (Recordset)sapCompany.GetBusinessObject(BoObjectTypes.BoRecordset);
            creditRecordset.DoQuery($@"SELECT VPM2.DocNum as vpm2DocNum,
                                       VPM2.InvoiceID as InvoiceID,
                                       ORPC.DocNum as opchDocNum,
                                       OVPM.DocDate,
                                       LEFT(OVPM.TrsfrRef, 20) as TrsfrRef,
                                       round(VPM2.AppliedSys / (1 - (VPM2.Dcount / 100)), 2) as Amount,
                                       round(VPM2.AppliedFC / (1 - (VPM2.Dcount / 100)), 2) as FCAmount, 
                                       OVPM.DocRate as ExchangeRate,
	                                   ORPC.UpdateDate,
                                       ORPC.U_WB_ID_NEW as ApInvoiceId
                                FROM   OVPM
                                       JOIN VPM2 ON VPM2.DocNum = OVPM.DocNum
                                       JOIN ORPC ON ORPC.DocEntry = VPM2.DocEntry 
                                --WHERE ORPC.UpdateDate > '{lastUpdateDate.ToString(sapDateFormat)}' --based on Invoice date instead of payment date
                                WHERE OVPM.DocDate >= '{lastUpdateDate.ToString(sapDateFormat)}'
                                        AND LEFT(ORPC.U_WB_ID_NEW, 3) = 'AP:'"); //comment this for testing current data

            while (!creditRecordset.EoF)
            {
                var request = BuildRequest(creditRecordset);

                try
                {
                    var result = wbTrfclient.PaymentTrfApi_PostAsync(request);
                    importedCreditPayment++;
                }
                catch (Exception)
                {
                    Helpers.LogAppError($"Error importing credit payment: {request.PaymentID}");
                    throw;
                }

                creditRecordset.MoveNext();
            };
            Helpers.LogInfo($"ImportPayments; Total count to be credit imported: {creditRecordset.RecordCount}. \r\nTotal count successfully imported:{importedCreditPayment}");

            return $"ImportPayments; Total count to be imported: {recordset.RecordCount + creditRecordset.RecordCount}. \r\nTotal count successfully imported:{importedInvoicePayment + importedCreditPayment}";
        }

        private Transfer_PaymentTrfApiModel BuildRequest(Recordset recordset)
        {
            //var invoiceId = recordset.Fields.Item("ApInvoiceId").Value.Split(':').Last();
            var invoiceId = recordset.Fields.Item("ApInvoiceId").Value;
            var request = new Transfer_PaymentTrfApiModel();
            request.FinCoCode = GetFinCocode();
            request.PaymentID = (int)recordset.Fields.Item("opchDocNum").Value;
            request.ExternalID = recordset.Fields.Item("vpm2DocNum").Value?.ToString();
            request.APInvoiceID = Convert.ToInt32(invoiceId.ToString().Split(':')[1]);
            request.PaymentReference = recordset.Fields.Item("TrsfrRef").Value?.ToString();
            request.PaymentDate = (DateTime)recordset.Fields.Item("DocDate").Value;
            request.Amount = (double)recordset.Fields.Item("Amount").Value;
            return request;
        }
    }
}
