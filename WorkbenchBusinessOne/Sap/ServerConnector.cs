using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Workbench.Agent.BusinessOne.Properties;

namespace Workbench.Agent.BusinessOne.Sap
{
    public class ServerConnection : IDisposable
    {
        private static ServerConnection current = null;

        private SAPbobsCOM.Company company = new SAPbobsCOM.Company();
        private int connectionResult;
        private int errorCode = 0;
        private string errorMessage = "";

        public bool Connected { get; set; }

        /// <summary>
        /// Initialises server settings, sets up connection credentials and attempts
        /// a new connection to SAP Business One server.
        /// </summary>
        /// <returns>Connection result as integer. Returns 0 if connection was successful</returns>
        public int Connect()
        {
            /*
            All the server settings and user credentials used below are stored in App.config file.
            ConfigurationManager is being used to read the App.config file. 
            You can store you own settings in App.config or use actual values directly in the code:
            company.Server = "sapb1server";
            Example.App.config is included in this project, rename it to App.config and populate it with your own values.
            */


            //Helpers.LogInfo(ConfigurationManager.AppSettings["BusinessOneServer"] + ConfigurationManager.AppSettings["BusinessOneLicenseServer"] + ConfigurationManager.AppSettings["BusinessOneCompanyDb"]);
            company.Server = ConfigurationManager.AppSettings["BusinessOneServer"];
            company.LicenseServer = ConfigurationManager.AppSettings["BusinessOneLicenseServer"];
            company.CompanyDB = ConfigurationManager.AppSettings["BusinessOneCompanyDb"];
            company.DbServerType = (SAPbobsCOM.BoDataServerTypes)Enum.Parse(typeof(SAPbobsCOM.BoDataServerTypes), ConfigurationManager.AppSettings["BusinessOneDbServerType"]);
            company.DbUserName = ConfigurationManager.AppSettings["BusinessOneDbUserName"];
            company.DbPassword = Helpers.FromBase64(ConfigurationManager.AppSettings["BusinessOneDbPassword"]);
            company.UserName = ConfigurationManager.AppSettings["BusinessOneUserName"];
            company.Password = Helpers.FromBase64(ConfigurationManager.AppSettings["BusinessOnePassword"]);
            company.language = SAPbobsCOM.BoSuppLangs.ln_English_Gb;
            company.UseTrusted = Convert.ToBoolean(ConfigurationManager.AppSettings["BusinessOneUseTrusted"]);


            connectionResult = company.Connect();

            if (connectionResult != 0)
            {
                company.GetLastError(out errorCode, out errorMessage);
                //Console.WriteLine("There was an error in Sap connection", errorMessage);
            } 
            else
            {
                Connected = true;
                //Console.WriteLine("SAP is connected");
            }

            return connectionResult;
        }
        /// <summary>
        /// Returns SAP Business One Company Object
        /// </summary>
        /// <returns>SAPbobsCOM.Company object</returns>
        public SAPbobsCOM.Company GetCompany()
        {
            return this.company;
        }

        /// <summary>
        /// Returns last error code
        /// </summary>
        /// <returns>Last error code as integer</returns>
        public int GetErrorCode()
        {
            return this.errorCode;
        }

        /// <summary>
        /// Returns last error message
        /// </summary>
        /// <returns>Last error message as String</returns>
        public String GetErrorMessage()
        {
            return this.errorMessage;
        }

        public void Dispose()
        {
            if (this.company != null && this.company.Connected)
                this.company.Disconnect();
        }

        public bool isConnected()
        {
            return this.company != null && this.company.Connected;
        }

        public static ServerConnection Current
        {
            get
            {
                if (current == null)
                    current = new ServerConnection();
                return current;
            }
        }
    }
}
