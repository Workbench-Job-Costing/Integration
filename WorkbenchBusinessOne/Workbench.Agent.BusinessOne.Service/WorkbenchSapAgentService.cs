using Nancy.Hosting.Self;
using System;
using System.Configuration;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Timers;
using Workbench.Agent.BusinessOne.HubClients;
using Workbench.Agent.BusinessOne.Sap;

namespace Workbench.Agent.BusinessOne.Service
{
    public partial class WorkbenchSapAgentService : ServiceBase
    {
        private int eventId = 1;
        static string scheduledSyncTime = "6:00";
        public WorkbenchSapAgentService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            // Update the service state to Start Pending.
            ServiceStatus serviceStatus = new ServiceStatus();
            serviceStatus.dwCurrentState = ServiceState.SERVICE_START_PENDING;
            serviceStatus.dwWaitHint = 60000;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);

            Helpers.LogInfo($"In OnStart..");

            #region Timers

            Timer timer = new Timer(1000 * 60 * Convert.ToInt32(ConfigurationManager.AppSettings["SyncIntervalInMinutes"]));
            timer.Elapsed += new ElapsedEventHandler(this.OnTimer);
            timer.Start();

            Timer reconnectTimer = new Timer();
            reconnectTimer.Interval = 420000; // 15 mins
            reconnectTimer.Elapsed += new ElapsedEventHandler(this.OnReconnect);
            reconnectTimer.Start();

            #endregion

            var host = new NancyHost(new Uri(ConfigurationManager.AppSettings["ManagementConsole"]));
            host.Start();

            var echoClient = EchoClient.Current;
            var sapClient = ServerConnection.Current;

            // attempt connection; 0 = success
            if (sapClient.Connect() == 0)
            {
                Helpers.LogInfo($"Successfully connected to {sapClient.GetCompany().CompanyName}!");

                var helper = new HelperMethods(sapClient.GetCompany());
                helper.AddUserDefinedFields();
                helper.SaveFinCocode();
            }
            else
            {
                Helpers.LogAppError($"{sapClient.GetErrorCode()}: {sapClient.GetErrorMessage()}");
            }

            // Update the service state to Running.
            serviceStatus.dwCurrentState = ServiceState.SERVICE_RUNNING;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);
        }

        protected override void OnStop()
        {
            Helpers.LogInfo($"In OnStop.");
        }

        protected override void OnContinue()
        {
            Helpers.LogInfo($"In OnContinue.");
        }

        public void OnTimer(object sender, ElapsedEventArgs args)
        {
            //eventLog1.WriteEntry("Monitoring the System", EventLogEntryType.Information, eventId++);
            //Helpers.LogInfo($"Monitoring the System");
            //TimeSpan scheduledTime = TimeSpan.Parse(scheduledSyncTime);
            //TimeSpan currentTime = TimeSpan.Parse(DateTime.Now.ToString("H:mm"));

            //if (currentTime == scheduledTime)
            //{
            //    Sync();
            //}

            TimeSpan workHourStartTime = TimeSpan.Parse(ConfigurationManager.AppSettings["WorkHourStartTime"]);
            TimeSpan workHourEndTime = TimeSpan.Parse(ConfigurationManager.AppSettings["WorkHourEndTime"]);
            TimeSpan ratesSyncTime = TimeSpan.Parse(ConfigurationManager.AppSettings["RatesSyncTime"]);
            TimeSpan currentTime = TimeSpan.Parse(DateTime.Now.ToString("H:mm"));
            if (currentTime >= workHourStartTime && currentTime <= workHourEndTime)
            {
                Sync();

                //assuming that SAP company refresh their rates within working hours
                Helpers.LogInfo($"{currentTime} , {ratesSyncTime}");
                Helpers.LogInfo($"{currentTime <= ratesSyncTime}");
                if (currentTime <= ratesSyncTime)
                {
                    Helpers.LogInfo($"rates sync");

                    SyncRates();
                }
            }
        }

        public void OnReconnect(object sender, ElapsedEventArgs args)
        {
            var sapClient = ServerConnection.Current;

            Helpers.LogInfo($"Reconnect the System {eventId++}");


            if (sapClient.Connect() == 0)
            {
                Helpers.LogInfo($"Successfully reconnected to {sapClient.GetCompany().CompanyName}!");

            }
            else
            {
                Helpers.LogInfo($"Error {sapClient.GetErrorCode()}: {sapClient.GetErrorMessage()}");
            }

            if (!Bootstrapper.current.Success)
            {
                Bootstrapper.current.Connect();
                //eventLog1.WriteEntry($"Singal R ReConnected");
                Helpers.LogInfo($"Singal R Export reConnected");
            }
        }

        private void Sync()
        {
            HttpHelper.Post($"/SyncCompanies").Wait();
            HttpHelper.Post($"/SyncPayments").Wait();
            HttpHelper.Post($"/SyncJobReceipts").Wait();
            HttpHelper.Post($"/SyncJobs").Wait();
        }

        private void SyncRates()
        {
            HttpHelper.Post($"/SyncExchangeRates").Wait();
        }

        public enum ServiceState
        {
            SERVICE_STOPPED = 0x00000001,
            SERVICE_START_PENDING = 0x00000002,
            SERVICE_STOP_PENDING = 0x00000003,
            SERVICE_RUNNING = 0x00000004,
            SERVICE_CONTINUE_PENDING = 0x00000005,
            SERVICE_PAUSE_PENDING = 0x00000006,
            SERVICE_PAUSED = 0x00000007,
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ServiceStatus
        {
            public int dwServiceType;
            public ServiceState dwCurrentState;
            public int dwControlsAccepted;
            public int dwWin32ExitCode;
            public int dwServiceSpecificExitCode;
            public int dwCheckPoint;
            public int dwWaitHint;
        };

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool SetServiceStatus(System.IntPtr handle, ref ServiceStatus serviceStatus);


    }
}
