using SAPbobsCOM;
using System;
using System.Linq;
using Workbench.Agent.BusinessOne.Models;
using Workbench.Agent.BusinessOne.WorkbenchClient;

namespace Workbench.Agent.BusinessOne.Integrations
{
    public class ImportCompanies : ImportBase
    {
        public ImportCompanies(Client wbClient, Company sapCompany, WorkbenchTrfClient wbTrfclient) :
         base(wbClient, sapCompany, wbTrfclient)
        {
        }

        public override string Import()
        {
            var lastUpdateDate = SettingsModelList.GetUpdateDate("LastCompanieSyncDate");
            var result = ImportProcess(lastUpdateDate, sapCompany);
            SettingsModelList.SetUpdateDate("LastCompanieSyncDate", DateTime.Now);
            return result;
        }

        private string ImportProcess(DateTime lastUpdateDate, Company sapCompany)
        {
            int importedCompanyCount = 0;
            var recordset = (Recordset)sapCompany.GetBusinessObject(BoObjectTypes.BoRecordset);
            recordset.DoQuery($@"SELECT
                                   left(OCRD.CardCode, 20) as CardCode,
                                   left(CardName, 50) as CardName, 
                                   CardType,
                                   Currency,                                   
                                   left(LicTradNum, 20) as LicTradNum,
                                   RTRIM(LEFT(CntctPrsn, CHARINDEX(' ', CntctPrsn))) AS FirstName,
                                   SUBSTRING(CntctPrsn, CHARINDEX(' ', CntctPrsn) + 1, 8000) AS LastName,
                                   left(OCRD.Phone1, 30) as Phone1,
                                   left(OCRD.Fax, 30) as Fax,
                                   LEFT(OCRD.E_mail, 50) as E_mail,
                                   FrozenFor,
                                   OCRD.UpdateDate,
                                   left(OCPR .Tel1, 50) as BusinessPhone,
                                   left(OCPR.Cellolar,50) as MobilePhone, 
                                   left(OCPR.Fax, 50) as ContactFax, 
                                   left(OCPR.E_MailL, 100) as ContactEmail,
                                   CASE
                                      WHEN PayDuMonth = 'Y' 
		                                THEN ExtraDays + 1 
                                      ELSE ExtraDays 
                                   END AS PaymentTermsDays, 
                                   CASE
                                      WHEN (PayDuMonth = 'Y' OR PayDuMonth = 'E') AND ExtraDays <= 31 
		                                THEN 2 
                                      WHEN (PayDuMonth = 'E' AND ExtraMonth = 1 )
		                                THEN 3 
                                      WHEN (PayDuMonth = 'Y' OR PayDuMonth = 'E' ) AND ExtraDays > 31 
		                                THEN 4 
                                      ELSE 1 
                                   END AS PaymentTermsType
                                FROM  OCRD 
                                   LEFT OUTER JOIN OCTG ON OCRD.GroupNum = OCTG.GroupNum 
                                   LEFT OUTER JOIN OCPR ON OCRD.CardCode = OCPR.CardCode 
	                                AND SUBSTRING(OCPR.Name , CHARINDEX(' ', OCPR.Name) + 1, 8000) = SUBSTRING(OCRD.CntctPrsn, CHARINDEX(' ', OCRD.CntctPrsn) + 1, 8000)
                                WHERE  OCRD.CardName IS NOT NULL
                                        AND OCRD.CardType in ('S','C')
                                        AND (OCRD.updatedate >= '{lastUpdateDate.ToString(sapDateFormat)}' OR OCRD.CreateDate >= '{lastUpdateDate.ToString(sapDateFormat)}' )");

            while (!recordset.EoF)
            {
                var newCompanyTrf = BuildRequest(recordset);

                try
                {
                    var result = wbTrfclient.CompanyTrfApi_PostAsync(newCompanyTrf);
                    var test = result.Result.ToString();
                    importedCompanyCount++;
                }
                catch (Exception ex)
                {
                    Helpers.LogAppError($"Error importing company: {newCompanyTrf.CompanyCode}  \r\n{ex}");
                }

                recordset.MoveNext();
            };

            return $"ImportCompanies; Total count to be imported: {recordset.RecordCount}. \r\nTotal count successfully imported: {importedCompanyCount}";
        }

        private Transfer_CompanyTrfApiModel BuildRequest(Recordset recordset)
        {
            var newCompanyTrf = new Transfer_CompanyTrfApiModel();
            newCompanyTrf.FinCoCode = GetFinCocode();
            newCompanyTrf.CompanyCode = recordset.Fields.Item("CardCode").Value?.ToString();
            newCompanyTrf.CompanyName = recordset.Fields.Item("CardName").Value?.ToString();
            newCompanyTrf.ClientSupplier = recordset.Fields.Item("CardType").Value?.ToString().ToUpper() == "S" ? 0 : 1;
            newCompanyTrf.AlphaCode = recordset.Fields.Item("CardName").Value?.ToString().Length > 10 ? recordset.Fields.Item("CardName").Value?.ToString().Substring(0, 10) : recordset.Fields.Item("CardName").Value?.ToString();
            newCompanyTrf.CurrencyCode = recordset.Fields.Item("Currency").Value?.ToString();
            newCompanyTrf.PaymentTermsDays = (int)recordset.Fields.Item("PaymentTermsDays").Value;
            newCompanyTrf.PaymentTermsType = (int)recordset.Fields.Item("PaymentTermsType").Value;
            newCompanyTrf.GSTNumber = recordset.Fields.Item("LicTradNum").Value?.ToString();
            newCompanyTrf.ContactFirstName = recordset.Fields.Item("FirstName").Value?.ToString();
            newCompanyTrf.ContactSurname = recordset.Fields.Item("LastName").Value?.ToString();
            newCompanyTrf.PostalPhone = recordset.Fields.Item("Phone1").Value?.ToString();
            newCompanyTrf.PostalFax = recordset.Fields.Item("Fax").Value?.ToString();
            newCompanyTrf.PhysicalPhone = recordset.Fields.Item("Phone1").Value?.ToString();
            newCompanyTrf.PhysicalFax = recordset.Fields.Item("Fax").Value?.ToString();
            newCompanyTrf.EmailAddress = recordset.Fields.Item("E_mail").Value?.ToString();
            newCompanyTrf.Contact_BusinessPhone = recordset.Fields.Item("BusinessPhone").Value?.ToString();
            newCompanyTrf.Contact_MobilePhone = recordset.Fields.Item("MobilePhone").Value?.ToString();
            newCompanyTrf.Contact_Fax = recordset.Fields.Item("ContactFax").Value?.ToString();
            newCompanyTrf.Contact_EmailAddress = recordset.Fields.Item("ContactEmail").Value?.ToString();
            newCompanyTrf.Inactive = recordset.Fields.Item("CardType").Value?.ToString().ToUpper() == "Y" ? true : false;
            newCompanyTrf.ExternalID = recordset.Fields.Item("CardCode").Value?.ToString();

            SetBankAccount(newCompanyTrf);
            SetPostalAddress(newCompanyTrf);
            SetPhysicalAddress(newCompanyTrf);
            return newCompanyTrf;

        }

        private void SetBankAccount(in Transfer_CompanyTrfApiModel newCompanyTrf)
        {
            try
            {
                var recordset = (Recordset)sapCompany.GetBusinessObject(BoObjectTypes.BoRecordset);
                recordset.DoQuery($@"SELECT top 3 CONCAT(BankCode,'-',Branch,'-',Account,'-',UsrNumber1) as BankAccount, BankCode, Branch, Account, UsrNumber1 
                                        FROM OCRB 
                                        WHERE CardCode ='{newCompanyTrf.CompanyCode}' order by AbsEntry");
                recordset.MoveFirst();
                newCompanyTrf.BankAccountRef1 = SafeSubstring(recordset.Fields.Item("BankAccount").Value?.ToString(), 0, 25);
                if (!recordset.EoF && !recordset.BoF)
                {
                    recordset.MoveNext();
                    newCompanyTrf.BankAccountRef2 = SafeSubstring(recordset.Fields.Item("BankAccount").Value?.ToString(), 0, 25);
                    if (!recordset.EoF && !recordset.BoF)
                    {
                        recordset.MoveNext();
                        newCompanyTrf.BankAccountRef3 = SafeSubstring(recordset.Fields.Item("BankAccount").Value?.ToString(), 0, 25);
                        recordset.MoveLast();
                    }
                }
            }

            catch (Exception ex)
            {
                Helpers.LogAppError($"Error importing company: {newCompanyTrf.CompanyCode}  \r\n{ex}");
            }
        }

        public static string SafeSubstring(string value, int startIndex, int length)
        {
            return new string((value ?? string.Empty).Skip(startIndex).Take(length).ToArray());
        }
        private void SetPostalAddress(in Transfer_CompanyTrfApiModel newCompanyTrf)
        {
            try
            {
                var recordset = (Recordset)sapCompany.GetBusinessObject(BoObjectTypes.BoRecordset);
                recordset.DoQuery($@"select top 1 left(CRD1.Street, 40) as Street, 
                                    left(CRD1.Block, 40) as Block, 
                                    left(CRD1.City, 40) as City, 
                                    left(CRD1.Country, 40) as Country, 
                                    left(CRD1.State, 40) as State, 
                                    left(OCRY.Name, 40) as Name, 
                                    left(CRD1.ZipCode, 20) as ZipCode
                                    from CRD1
                                    left join OCRY on OCRY.Code = CRD1.Country
                                    where CardCode ='{newCompanyTrf.CompanyCode}' and AdresType = 'B'");

                while (!recordset.EoF)
                {
                    newCompanyTrf.PostalAddress1 = recordset.Fields.Item("Street").Value?.ToString();
                    newCompanyTrf.PostalAddress2 = recordset.Fields.Item("Block").Value?.ToString();
                    newCompanyTrf.PostalAddress3 = recordset.Fields.Item("City").Value?.ToString();
                    newCompanyTrf.PostalAddress4 = recordset.Fields.Item("Country").Value?.ToString();
                    newCompanyTrf.PostalAddress5 = recordset.Fields.Item("State").Value?.ToString();
                    newCompanyTrf.PostalAddress6 = recordset.Fields.Item("Name").Value?.ToString();
                    newCompanyTrf.PostalPostCode = recordset.Fields.Item("ZipCode").Value?.ToString();
                    recordset.MoveNext();
                }
            }

            catch (Exception ex)
            {
                Helpers.LogAppError($"Error importing company: {newCompanyTrf.CompanyCode}  \r\n{ex}");
            }
        }

        private void SetPhysicalAddress(in Transfer_CompanyTrfApiModel newCompanyTrf)
        {
            try
            {
                var recordset = (Recordset)sapCompany.GetBusinessObject(BoObjectTypes.BoRecordset);
                recordset.DoQuery($@"select top 1 left(CRD1.Street, 40) as Street, 
                                    left(CRD1.Block, 40) as Block, 
                                    left(CRD1.City, 40) as City, 
                                    left(CRD1.Country, 40) as Country, 
                                    left(CRD1.State, 40) as State, 
                                    left(OCRY.Name, 40) as Name, 
                                    left(CRD1.ZipCode, 20) as ZipCode
                                    from CRD1
                                    left join OCRY on OCRY.Code = CRD1.Country
                                    where CardCode ='{newCompanyTrf.CompanyCode}' and AdresType = 'S'");

                while (!recordset.EoF)
                {
                    newCompanyTrf.PhysicalAddress1 = recordset.Fields.Item("Street").Value?.ToString();
                    newCompanyTrf.PhysicalAddress2 = recordset.Fields.Item("Block").Value?.ToString();
                    newCompanyTrf.PhysicalAddress3 = recordset.Fields.Item("City").Value?.ToString();
                    newCompanyTrf.PhysicalAddress4 = recordset.Fields.Item("Country").Value?.ToString();
                    newCompanyTrf.PhysicalAddress5 = recordset.Fields.Item("State").Value?.ToString();
                    newCompanyTrf.PhysicalAddress6 = recordset.Fields.Item("Name").Value?.ToString();
                    newCompanyTrf.PhysicalPostCode = recordset.Fields.Item("ZipCode").Value?.ToString();
                    recordset.MoveNext();
                }
            }

            catch (Exception ex)
            {
                Helpers.LogAppError($"Error importing company: {newCompanyTrf.CompanyCode}  \r\n{ex}");
            }
        }
    }
}
