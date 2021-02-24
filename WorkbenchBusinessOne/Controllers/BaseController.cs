using Nancy;
using Nancy.Extensions;
using Nancy.Security;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;
using Workbench.Agent.BusinessOne.HubClients;
using Workbench.Agent.BusinessOne.Models;
using Workbench.Agent.BusinessOne.Properties;
using Workbench.Agent.BusinessOne.Sap;

namespace Workbench.Agent.BusinessOne.Controllers
{
    public abstract class BaseController<TModel> : NancyModule where TModel : BaseModel
    {
        public TModel Model { get; set; }

        public BaseController()
        {
            Model = Init();

            Model.ApplicationPath = ConfigurationManager.AppSettings["ApplicationPath"];

            Model.Workbench = new WorkbenchModel
            {
                Connected = EchoClient.Current.Success,
                LastHeartBeat = EchoClient.Current.LastHeartBeat ?? DateTime.MinValue
            };

            Model.Sap = new SapModel
            {
                Connected = ServerConnection.Current.Connected,
                //CompanyName = ServerConnection.Current.GetCompany().CompanyName,
                Message = ServerConnection.Current.Connected ?
                        "Workbench  is connected to Business One " + ServerConnection.Current.GetCompany().CompanyName
                            : "Error " + ServerConnection.Current.GetErrorCode() + ": " + ServerConnection.Current.GetErrorMessage()
            };
            
            Model.LastCompanieSyncDate = SettingsModelList.GetUpdateDate("LastCompanieSyncDate").ToString("yyyy-MM-ddThh:mm");
            Model.LastPaymentSyncDate = SettingsModelList.GetUpdateDate("LastPaymentSyncDate").ToString("yyyy-MM-ddThh:mm");
            Model.LastJobReceiptsSyncDate = SettingsModelList.GetUpdateDate("LastJobReceiptsSyncDate").ToString("yyyy-MM-ddThh:mm");
            Model.LastAPInvoiceSyncDate = SettingsModelList.GetUpdateDate("LastAPInvoiceSyncDate").ToString("yyyy-MM-ddThh:mm");
            Model.LastJobSyncDate = SettingsModelList.GetUpdateDate("LastJobSyncDate").ToString("yyyy-MM-ddThh:mm");
            this.RequiresAuthentication();
        }

        public abstract TModel Init();

        public dynamic RequestJson
        {
            get
            {
                var jsonString = Request.Body.AsString();
                return JsonConvert.DeserializeObject(jsonString);
            }
        }
    }
}
