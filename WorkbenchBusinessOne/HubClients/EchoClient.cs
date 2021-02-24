using Microsoft.AspNet.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Workbench.Agent.BusinessOne.Properties;


namespace Workbench.Agent.BusinessOne.HubClients
{
    /// <summary>
    /// This is for client connectivity tests only
    /// </summary>
    public class EchoClient : IDisposable
    {
        private static EchoClient current = null;

        private HubConnection connection;
        private System.Timers.Timer timer;

        public bool Success;
        public DateTime? LastHeartBeat;

        private EchoClient()
        {
            timer = new System.Timers.Timer(1000 * 60 * 20);
            timer.Enabled = true;

            connection = new HubConnection(ConfigurationManager.AppSettings["WorkbenchUrl"]);
            var echoHub = connection.CreateHubProxy("EchoHub");

            connection.Start().ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    Helpers.LogAppError($"SignalR Echo: There was an error opening the connection {task.Exception.GetBaseException()}"); 
                    Success = false;
                }
                else
                {
                    echoHub.On<string>("ping", (message) =>
                    {
                        //Console.WriteLine("Pong : " + message);
                    });

                    timer.Elapsed += (obj, arg) =>
                    {

                        echoHub.Invoke<string>("Send", "Pinging at: " + DateTime.Now).ContinueWith(result =>
                        {
                            if (result.IsFaulted)
                            {
                                Helpers.LogAppError($"SignalR Echo reconnect failed {task.Exception.GetBaseException()}");
                                Success = false;
                            }
                            else
                            {
                                LastHeartBeat = DateTime.Now;
                                Success = true;
                            }
                        });
                    };

                    timer.Start();
                }

            }).Wait();
        }

        public void Dispose()
        {
            timer.Stop();

            if (connection != null)
                connection.Dispose();
        }

        public static EchoClient Current
        {
            get
            {
                if (current == null)
                    current = new EchoClient();
                return current;
            }
        }
    }
}
