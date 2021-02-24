using SAPbobsCOM;
using System;
using System.Collections.Generic;
using System.Linq;
using Workbench.Agent.BusinessOne.Sap;
using Workbench.Agent.BusinessOne.WorkbenchClient;

namespace Workbench.Agent.BusinessOne.Integrations
{
    public class ImportBase
    {
        internal readonly Client wbClient;
        internal readonly Company sapCompany;
        public string sapDateFormat = "yyyy-MM-dd";
        internal readonly WorkbenchTrfClient wbTrfclient;

        public ImportBase(Client wbClient, Company sapCompany, WorkbenchTrfClient wbTrfclient)
        {
            this.wbClient = wbClient;
            this.sapCompany = sapCompany;
            this.wbTrfclient = wbTrfclient;            
        }

        public virtual string Import()
        {
            return "";
        }

        public string GetFinCocode()
        {
            var request = new TableApiRequest()
            {
                TableName = "FinancialCompanies",
                ColumnNames = "CompanyCode",
                PredicateRows = new List<DynamicPredicateRowApi>
                {
                    new DynamicPredicateRowApi
                    {
                        Display = true,
                        LeftOperand = "Description",
                        Operator = DynamicPredicateRowApiOperator.Eq,
                        RightOperand = new List<string> { ServerConnection.Current.GetCompany().CompanyName }
                    }
                },
                Page = 1,
                Rows = 1
            };

            var result = wbClient.TableApi_PostAsync(request);
            if (result.Result.Rows.Count() != 0)
            {
                return result.Result.Rows.FirstOrDefault().Key.KeyValue;
            }
            return "01";
        }
    }
}
