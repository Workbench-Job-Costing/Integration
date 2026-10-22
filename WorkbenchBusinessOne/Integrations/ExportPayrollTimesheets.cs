using SAPbobsCOM;
using System;
using System.Collections.Generic;
using System.Linq;
using Workbench.Agent.BusinessOne.Models;
using Workbench.Agent.BusinessOne.WorkbenchClient;

namespace Workbench.Agent.BusinessOne.Integrations
{
    /// <summary>
    /// Example of exporting a Workbench Payroll batch. Reads the rows staged for the batch from
    /// GET api/PayrollTimesheetTrf/{BatchNo} and creates one SAP B1 timesheet per employee.
    /// The endpoint is read-only (the staging table has no ExternalID), so nothing is written back
    /// other than the export log and the batch status.
    /// </summary>
    public class ExportPayrollTimesheets : ExportBase
    {
        private const string ExportTable = "PayrollTimesheets";

        public ExportPayrollTimesheets(Client wbClient, Company sapCompany, WorkbenchTrfClient wbTrfclient) :
         base(wbClient, sapCompany, wbTrfclient)
        {
        }

        public override string Export(int batchNo)
        {
            var result = ExportProcess(batchNo);
            SettingsModelList.SetUpdateDate("LastPayrollTimesheetsSyncDate", DateTime.Now);
            return result;
        }

        public string Export(int batchNo, string wbSessionId)
        {
            this.wbSessionId = wbSessionId;
            var result = ExportProcess(batchNo);
            SettingsModelList.SetUpdateDate("LastPayrollTimesheetsSyncDate", DateTime.Now);
            return result;
        }

        private string ExportProcess(int batchNo)
        {
            var apiResult = wbTrfclient.PayrollTimesheetTrfApi_GetAsync(batchNo);
            var timesheets = apiResult.Result;

            Helpers.LogInfo($"{batchNo}\r\nTotal Count new: {timesheets.Count}");

            int insertedRecords = 0;

            // One SAP timesheet per employee, covering every row Workbench staged for them in the batch
            var employeeTimesheets = timesheets.GroupBy(a => a.EmployeeNo).ToList();

            if (Validate(timesheets, batchNo))
            {
                foreach (var employee in employeeTimesheets)
                {
                    try
                    {
                        if (PostTimesheet(employee.Key, employee.ToList(), batchNo))
                        {
                            insertedRecords++;
                        }
                    }
                    catch (Exception)
                    {
                        ExportLogTrf(batchNo, batchNo, ExportTable, Type2.Error, $"{batchNo}\r\nError exporting timesheet for employee : {employee.Key}");
                    }
                }
            }

            if (employeeTimesheets.Count == insertedRecords && employeeTimesheets.Count != 0)
            {
                ExportLogTrf(batchNo, batchNo, ExportTable, Type2.Info, $"{batchNo}\r\nSuccess");
                TransferLogTrf(wbSessionId, batchNo, Type.Info);
            }
            else
            {
                TransferLogTrf(wbSessionId, batchNo, Type.Error);
            }
            return $"{batchNo}\r\nTotal count to be exported: {employeeTimesheets.Count}. \r\nTotal count successfully exported: {insertedRecords}. \r\nWith {insertedRecords} newly inserted";
        }

        /// <summary>
        /// Create the SAP B1 timesheet for one employee. Map PayrollCode to your own SAP activity or
        /// pay element here; the rows carry the Workbench payroll code, not an SAP code.
        /// </summary>
        private bool PostTimesheet(string employeeNo, ICollection<Transfer_PayrollTimesheetTrfApiModel> rows, int batchNo)
        {
            var timesheet = (TimeSheet)sapCompany.GetBusinessObject(BoObjectTypes.oTimeSheet);
            timesheet.UserID = GetEmployeeId(employeeNo);
            timesheet.DateFrom = rows.Min(r => r.TimesheetDate.Value.DateTime);
            timesheet.DateTo = rows.Max(r => r.TimesheetDate.Value.DateTime);

            foreach (var row in rows.OrderBy(r => r.TimesheetDate))
            {
                timesheet.TimeSheetLines.Add();
                timesheet.TimeSheetLines.Date = row.TimesheetDate.Value.DateTime;
                timesheet.TimeSheetLines.Effort = row.Hours.GetValueOrDefault();
                timesheet.TimeSheetLines.UserFields.Fields.Item("U_WB_Batch").Value = batchNo;
                timesheet.TimeSheetLines.UserFields.Fields.Item("U_WB_PayrollCode").Value = row.PayrollCode;
            }

            if (timesheet.Add() != 0)
            {
                var error = sapCompany.GetLastErrorDescription();
                ExportLogTrf(batchNo, batchNo, ExportTable, Type2.Error, $"{batchNo}\r\nError from SBO : {error}. For employee {employeeNo}");
                return false;
            }
            return true;
        }

        private bool Validate(ICollection<Transfer_PayrollTimesheetTrfApiModel> timesheets, int batchNo)
        {
            var errorCount = 0;

            foreach (var employeeNo in timesheets.Select(a => a.EmployeeNo).Distinct())
            {
                if (!EmployeeExists(employeeNo))
                {
                    errorCount++;
                    ExportLogTrf(batchNo, batchNo, ExportTable, Type2.Error, $"{batchNo}\r\nEmployee {employeeNo} not found in SBO");
                }
            }

            return errorCount == 0;
        }
    }
}
