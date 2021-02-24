using SAPbobsCOM;
using System;
using Workbench.Agent.BusinessOne.Models;
using Workbench.Agent.BusinessOne.WorkbenchClient;

namespace Workbench.Agent.BusinessOne.Integrations
{
    public class ImportExchangeRates : ImportBase
    {
        public ImportExchangeRates(Client wbClient, Company sapCompany, WorkbenchTrfClient wbTrfclient) :
         base(wbClient, sapCompany, wbTrfclient)
        {
        }

        public override string Import()
        {
            var lastUpdateDate = SettingsModelList.GetUpdateDate("LastExchangeRatesSyncDate");
            var result = ImportProcess(sapCompany);
            SettingsModelList.SetUpdateDate("LastExchangeRatesSyncDate", DateTime.Now);
            return result;
        }

        private string ImportProcess(Company sapCompany)
        {
            #region test code

            Recordset bo = (Recordset)sapCompany.GetBusinessObject(BoObjectTypes.BoRecordset);

            bo.DoQuery($"SELECT * from ORTT where ratedate = '{DateTime.Now.ToString(sapDateFormat)}' ");
            while (!bo.EoF)
            {
                var date = bo.Fields.Item("RateDate").Value?.ToString();
                var currency = bo.Fields.Item("Currency").Value?.ToString();
                var rate = bo.Fields.Item("Rate").Value?.ToString();

                try
                {
                    var wbCurrency = wbClient.TableRowApi_GetAsync("Currencies", "CurrencyCode", currency, "CurrencyCode").Result;

                    var result = wbClient.TableRowApi_PostAsync(new TableApiLine()
                    {
                        Key = new TableApiKey()
                        {
                            TableName = "Currencies",
                            KeyName = "CurrencyCode",
                            KeyValue = currency,
                            ColumnNames = "FCConversionRate"
                        },
                        Col01 = rate,
                    }).Result;
                }
                catch (Exception) {  }

                bo.MoveNext();
            }
            #endregion



            return $"ImportExchangeRates; Synced";
        }
    }
}
