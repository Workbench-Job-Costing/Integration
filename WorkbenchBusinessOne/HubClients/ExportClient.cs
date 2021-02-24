using Microsoft.AspNet.SignalR.Client;
using System;
using System.Configuration;
using Workbench.Agent.BusinessOne.Integrations;


namespace Workbench.Agent.BusinessOne.HubClients
{
    public class ExportClient : IDisposable
    {
        private static ExportClient current = null;
        private HubConnection connection;
        private System.Timers.Timer timer;
        //private System.Timers.Timer timer2;

        public bool Success;
        public DateTime? LastHeartBeat;

        public ExportClient()
        {
        }

        public ExportClient(
            ExportJobs exportJobs,
            ExportGLJournals exportGLJournals,
            ExportAPInvoices exportAPInvoices,
            ExportARInvoices exportARInvoices)
        {
            timer = new System.Timers.Timer(1000 * 60 * 20); 
            timer.Enabled = true;

            connection = new HubConnection(ConfigurationManager.AppSettings["WorkbenchUrl"]);
            var exportHub = connection.CreateHubProxy("ExportHub");

            connection.Start().ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    Helpers.LogAppError($"SignalR Export There was an error opening the connection {task.Exception.GetBaseException()}");
                }
                else
                {

                    exportHub.On<string>("export", (message) =>
                    {

                        var exportProcessBatch = new ExportProcessBatch(exportJobs, exportGLJournals, exportAPInvoices, exportARInvoices);                        

                        if(message !="Ping")
                        {
                            exportProcessBatch.ProcessBatch(message);
                            Helpers.LogInfo($"Batches: {message}");
                        }
                    });


                    timer.Elapsed += (obj, arg) =>
                    {
                        try
                        {
                            Helpers.LogInfo($"SignalR Export timer elapsed");

                            exportHub.Invoke<string>("Send", "Ping").ContinueWith(result =>
                            {
                                if (result.IsFaulted)
                                {
                                    Helpers.LogAppError($"SignalR Export reconnect failed {task.Exception.GetBaseException()}");
                                    Success = false;
                                }
                                else
                                {
                                    LastHeartBeat = DateTime.Now;
                                    Success = true;
                                }
                            });
                        }
                        catch(Exception ex)
                        {
                            Helpers.LogAppError($"SignalR Export {ex.Message}");
                            Success = false;
                        }

                    };

                    timer.Start();
                }
            }).Wait();
        }


        public void Dispose()
        {
            if (connection != null)
            {
                connection.Dispose();
                Success = false;
            }
        }

        public void Connect()
        {
            if (!Success)
                connection.Start();
        }

        public bool isConnected()
        {
            return connection != null;
        }

        public static ExportClient Current
        {
            get
            {
                if (current == null)
                    current = new ExportClient();
                return current;
            }
        }

    }
}
