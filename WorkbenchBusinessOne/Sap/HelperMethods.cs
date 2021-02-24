using SAPbobsCOM;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using Workbench.Agent.BusinessOne.Models;
using Workbench.Agent.BusinessOne.Properties;
using Workbench.Agent.BusinessOne.WorkbenchClient;

namespace Workbench.Agent.BusinessOne.Sap
{
    public class HelperMethods
    {
        private Company sapCompany;

        public HelperMethods(Company sapCompany)
        {
            this.sapCompany = sapCompany;
        }
        public void AddField(string TableName, string FieldName, string FieldDescription, SAPbobsCOM.BoFieldTypes FieldType, SAPbobsCOM.BoFldSubTypes FieldSubType, Int32 FieldSize)
        {
            var oUserFieldsMD = (SAPbobsCOM.UserFieldsMD)this.sapCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oUserFields);

            oUserFieldsMD.TableName = TableName;
            oUserFieldsMD.Name = FieldName;
            oUserFieldsMD.Description = FieldDescription;
            oUserFieldsMD.Type = FieldType;
            oUserFieldsMD.SubType = FieldSubType;
            if (System.Convert.ToInt32(FieldSize) > 0)
                oUserFieldsMD.EditSize = System.Convert.ToInt32(FieldSize);

            var lRetCode = oUserFieldsMD.Add();
            oUserFieldsMD = null/* TODO Change to default(_) if this is not a reference type */;
            GC.Collect();
            if (lRetCode != 0)
            {
                var error = this.sapCompany.GetLastErrorDescription();
                if (error != "")
                {
                    //Console.WriteLine($"Error Adding UDF {TableName} {FieldName} : {error}");
                }
            }
            //else
                //Console.WriteLine($"Table:  {TableName} successfully added column: { FieldName}");
        }

        public void AddUserDefinedFields()
        {
            //on header level of all marketing documents
            AddField("OPCH", "WB_ID_NEW", "Source ID new", SAPbobsCOM.BoFieldTypes.db_Alpha, SAPbobsCOM.BoFldSubTypes.st_None, 10);
            AddField("OPCH", "WB_ID", "Source ID new", SAPbobsCOM.BoFieldTypes.db_Alpha, SAPbobsCOM.BoFldSubTypes.st_None, 10);
            AddField("OPCH", "WB_Batch", "Batch No", SAPbobsCOM.BoFieldTypes.db_Numeric, SAPbobsCOM.BoFldSubTypes.st_None, 10);
            AddField("OPCH", "WB_ClmNo", "Claim No", SAPbobsCOM.BoFieldTypes.db_Numeric, SAPbobsCOM.BoFldSubTypes.st_None, 10);
            //on row level of all marketing documents
            AddField("INV1", "WB_ID", "Source ID", SAPbobsCOM.BoFieldTypes.db_Alpha, SAPbobsCOM.BoFldSubTypes.st_None, 10);
            AddField("INV1", "WB_ACT", "Activity", SAPbobsCOM.BoFieldTypes.db_Alpha, SAPbobsCOM.BoFldSubTypes.st_None, 10);
            AddField("INV1", "WB_WC", "Work Centre", SAPbobsCOM.BoFieldTypes.db_Alpha, SAPbobsCOM.BoFldSubTypes.st_None, 10);

            //on header of journal
            AddField("OJDT", "WB_Batch", "Batch No", SAPbobsCOM.BoFieldTypes.db_Numeric, SAPbobsCOM.BoFldSubTypes.st_None, 10);

        }

        public void SaveFinCocode()
        {
            var httpClient = new HttpClient()
            {
                BaseAddress = new Uri(ConfigurationManager.AppSettings["WorkbenchUrl"])
            };

            httpClient.DefaultRequestHeaders.Authorization = !string.IsNullOrEmpty(ConfigurationManager.AppSettings["WorkbenchApiKey"]) ?
                new AuthenticationHeaderValue("Bearer ", ConfigurationManager.AppSettings["WorkbenchApiKey"]) :
                new AuthenticationHeaderValue("Basic", (ConfigurationManager.AppSettings["WorkbenchUserName"] + ":" + ConfigurationManager.AppSettings["WorkbenchPassword"].FromBase64()).ToBase64());


            var wbClient = new Client(httpClient);
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
            var finCo = "";
            if (result.Result.Rows.Count() != 0)
            {
                finCo = result.Result.Rows.FirstOrDefault().Key.KeyValue;
            }

            SettingsModelList.SetFinCoCode(finCo);
        }
    }
}
