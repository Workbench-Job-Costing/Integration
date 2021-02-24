using SAPbobsCOM;
using System;
using Workbench.Agent.BusinessOne.WorkbenchClient;

namespace Workbench.Agent.BusinessOne.Integrations
{
    public class ExportBase
    {
        internal readonly Client wbClient;
        internal readonly Company sapCompany;
        public string sapDateFormat = "yyyy-MM-dd";
        internal readonly WorkbenchTrfClient wbTrfclient;
        protected string error = "";
        protected string wbSessionId = "";

        public ExportBase(Client wbClient, Company sapCompany, WorkbenchTrfClient wbTrfclient)
        {
            this.wbClient = wbClient;
            this.sapCompany = sapCompany;
            this.wbTrfclient = wbTrfclient;
        }

        public virtual string Export()
        {
            Console.WriteLine("BASE");
            return "";
        }

        public virtual string Export(int BatchNo)
        {
            Console.WriteLine("BASE");
            return "";
        }

        public bool JobExists(string jobCode)
        {
            var recordset = (Recordset)sapCompany.GetBusinessObject(BoObjectTypes.BoRecordset);
            recordset.DoQuery($"SELECT top 1 PrjCode FROM OPRJ WHERE PrjCode = '{jobCode}'");

            return !recordset.RecordCount.Equals(0);
        }

        public string GetGLCode(string glCode)
        {

            var useSegmentedCodeRecordset = (Recordset)sapCompany.GetBusinessObject(BoObjectTypes.BoRecordset);
            useSegmentedCodeRecordset.DoQuery($"select EnbSgmnAct from CINF where EnbSgmnAct = 'Y'");
            var useSegmentdCode = !useSegmentedCodeRecordset.RecordCount.Equals(0);
            var queryStr = "";
            if (useSegmentdCode)
            {
                var segments = glCode.Split('-');
                var count = 0;
                foreach(var segment in segments)
                {                    
                    queryStr += $" Segment_{count} = {segment} and ";
                    count++;
                }
                queryStr = queryStr.Substring(0, queryStr.Length - 4);
            }
            else
            {
                queryStr = $"AcctCode = '{glCode}' or Segment_0 = '{glCode}'";
            }

            var recordset = (Recordset)sapCompany.GetBusinessObject(BoObjectTypes.BoRecordset);
            recordset.DoQuery($"select top 1 AcctCode from OACT where {queryStr}");

            return recordset.Fields.Item("AcctCode").Value.ToString();
        }

        public bool GLBatchExists(int batchNo)
        {
            var recordset = (Recordset)sapCompany.GetBusinessObject(BoObjectTypes.BoRecordset);
            recordset.DoQuery($"select top 1 TransID from OJDT where U_WB_Batch = '{batchNo}'");

            return !recordset.RecordCount.Equals(0);
        }

        public bool CompanyExists(string companyCode)
        {
            var recordset = (Recordset)sapCompany.GetBusinessObject(BoObjectTypes.BoRecordset);
            recordset.DoQuery($"select top 1 CardCode from OCRD where CardCode =  '{companyCode}'");

            return !recordset.RecordCount.Equals(0);
        }


        public bool SupplierCompanyExists(string companyCode)
        {
            var recordset = (Recordset)sapCompany.GetBusinessObject(BoObjectTypes.BoRecordset);
            recordset.DoQuery($"select top 1 CardCode from OCRD where CardCode =  '{companyCode}' AND CardType = 'S'");

            return !recordset.RecordCount.Equals(0);
        }

        public bool CustomerCompanyExists(string companyCode)
        {
            var recordset = (Recordset)sapCompany.GetBusinessObject(BoObjectTypes.BoRecordset);
            recordset.DoQuery($"select top 1 CardCode from OCRD where CardCode =  '{companyCode}' AND CardType = 'C'");

            return !recordset.RecordCount.Equals(0);
        }

        public bool APInvoiceExists(int invoiceId, int invoiceCredit)
        {
            string table = "ORPC";
            if (invoiceCredit == 1) table = "OPCH";

            var recordset = (Recordset)sapCompany.GetBusinessObject(BoObjectTypes.BoRecordset);
            recordset.DoQuery($"select top 1 DocEntry from {table} where U_WB_ID_NEW  =  'AP:{invoiceId}'");

            return !recordset.RecordCount.Equals(0);
        }

        public bool ARInvoiceExists(int invoiceId, int invoiceCredit)
        {
            string table = "ORIN";
            if (invoiceCredit == 1) table = "OINV";

            var recordset = (Recordset)sapCompany.GetBusinessObject(BoObjectTypes.BoRecordset);
            recordset.DoQuery($"select top 1 DocEntry from {table} where DocNum  =  {invoiceId}");

            return !recordset.RecordCount.Equals(0);
        }

        public string GetBatchType(int batchNo)
        {
            var batchApiResult = wbClient.BatchDetailApi_GetAsync(batchNo, null);
            return batchApiResult.Result.Key.BatchType;
        }

        public void ExportLogTrf(int batchNo, int exportId, string exportTable, Type2 type, string message)
        {
            if (type == Type2.Error) Helpers.LogAppError($"{message}");
            if (type == Type2.Info) Helpers.LogInfo($"{message}");

            _ = wbTrfclient.LogTrfApi_Post2Async(batchNo, exportId, exportTable, type, message).Result;

        }
        public void TransferLogTrf(string sessionId, int batchNo, Type type)
        {
            var description = "";
            if (type == Type.Error) description = "Export log marked as error.";
            if (type == Type.Info) description = "Export log marked as successful.";
            Helpers.LogInfo($"{batchNo}\r\n{description}");

            _ = wbTrfclient.LogTrfApi_PostAsync(sessionId, description, batchNo, type).Result;
        }
    }
}
