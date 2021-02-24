using Microsoft.AspNet.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Workbench.Agent.BusinessOne.Properties;

namespace Workbench.Agent.BusinessOne.HubClients
{
    public class IntegrationClient : IDisposable
    {
        private static IntegrationClient current = null;

        private HubConnection connection = null;
        private System.Timers.Timer refreshConnectionTimer;

        public bool Success;
        public DateTime? LastHeartBeat;

        private IntegrationClient()
        {
            refreshConnectionTimer = new System.Timers.Timer(1000 * 60 * Convert.ToInt32(ConfigurationManager.AppSettings["SyncIntervalInMinutes"])); //Every 60 min
            refreshConnectionTimer.Enabled = true;

            refreshConnectionTimer.Elapsed += (obj, arg) => {
                Connect();
            };
        }

        private void Connect() {

            if (connection != null)
                connection.Stop();

            connection = new HubConnection(ConfigurationManager.AppSettings["WorkbenchUrl"]);
            var echoHub = connection.CreateHubProxy("BusinessOneHub");

            connection.Start().ContinueWith(task => {
                if (task.IsFaulted)
                {
                    task.Exception.GetBaseException()
                        .LogError("There was an error opening the connection to Business One Hub");
                }
                else
                {
                    //Console.WriteLine("SignalR Integration is Connected");

                    echoHub.On<string>("sync", (message) => {
                        
                    });

                    refreshConnectionTimer.Start();
                }

            }).Wait();
        }

        public void Dispose()
        {
            refreshConnectionTimer.Stop();

            if (connection != null)
                connection.Dispose();
        }

        public static IntegrationClient Current
        {
            get
            {
                if (current == null)
                    current = new IntegrationClient();
                return current;
            }
        }
    }
}
