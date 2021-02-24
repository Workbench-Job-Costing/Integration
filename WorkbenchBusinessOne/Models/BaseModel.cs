using System;

namespace Workbench.Agent.BusinessOne.Models
{
    public class BaseModel
    {
        public string ApplicationPath { get; set; }
        public WorkbenchModel Workbench { get; set; }
        public SapModel Sap { get; set; }
        public string LastCompanieSyncDate { get; set; }
        public string LastPaymentSyncDate { get; set; }
        public string LastJobReceiptsSyncDate { get; set; }
        public string LastAPInvoiceSyncDate { get; set; }
        public string LastJobSyncDate { get; set; }
    }

    public class WorkbenchModel
    {
        public bool Connected { get; set; }
        public string WorkbenchName { get; set; }
        public DateTime LastHeartBeat { get; set; }
    }

    public class SapModel
    {
        public bool Connected { get; set; }
        public string CompanyName { get; set; }
        public string Message { get; set; }
    }
}
