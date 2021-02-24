using SAPbobsCOM;
using System;
using System.Collections.Generic;
using Workbench.Agent.BusinessOne.Models;
using Workbench.Agent.BusinessOne.WorkbenchClient;

namespace Workbench.Agent.BusinessOne.Integrations
{
    public class ExportJobs : ExportBase
    {
        public ExportJobs(Client wbClient, Company sapCompany, WorkbenchTrfClient wbTrfclient) :
         base(wbClient, sapCompany, wbTrfclient)
        {
        }

        public override string Export()
        {
            var lastUpdateDate = SettingsModelList.GetUpdateDate("LastJobSyncDate");
            var result = ExportProcess(lastUpdateDate);
            SettingsModelList.SetUpdateDate("LastJobSyncDate", DateTime.Now);
            return result;
        }

        private string ExportProcess(DateTime lastUpdateDate)
        {
            var jobs = GetJobsForExport(lastUpdateDate);

            int insertedRecords = 0;
            int existingRecords = 0;

            var oCompServ = sapCompany.GetCompanyService();
            var projectService = (IProjectsService)oCompServ.GetBusinessService(ServiceTypes.ProjectsService);

            foreach (var job in jobs)
            {
                try
                {
                    if(!JobExists(job.JobCode))
                    {
                        var project = (Project)projectService.GetDataInterface(ProjectsServiceDataInterfaces.psProject);
                        project.Code = job.JobCode;
                        project.Name = job.Description;
                        var result = projectService.AddProject(project); 
                        insertedRecords++;
                    }
                    else existingRecords++; 
                  
                }
                catch (Exception)
                {
                    Console.WriteLine($"Error exporting job: {job.JobCode}");
                }                
            }
            return $"Total count to be exported: {jobs.Count}. \r\nTotal count successfully imported: {insertedRecords + existingRecords}. \r\nWith {insertedRecords} newly inserted";
        }

        private ICollection<GeneralJobLine> GetJobsForExport(DateTime lastUpdateDate)
        {
            var parameters = new GridRequestParametersApi()
            {
                Predicate = new DynamicPredicateApi()
                {
                    PredicateRows = new List<DynamicPredicateRowApi>()
                    {
                        new DynamicPredicateRowApi()
                        {
                            LeftOperand = "Finalised",
                            Operator = DynamicPredicateRowApiOperator.Eq,
                            RightOperand = new List<string>() { "0" },
                            Display = true
                        },
                        new DynamicPredicateRowApi()
                        {
                            LeftOperand = "UpdatedDate",
                            Operator = DynamicPredicateRowApiOperator.Gt,
                            RightOperand = new List<string>() { lastUpdateDate.ToString(sapDateFormat) },
                            Display = true
                        }
                    }
                },
                Sidx = "JobCode",
                Sord = "asc",
                Page = 1,
                Rows = 1000,
                FunctionalCode = GridRequestParametersApiFunctionalCode.General
            };
            var jobApiResult = wbClient.JobListApi_PostAsync(parameters);

            return jobApiResult.Result.Rows;
        }
    }
}
