using Nancy.Hosting.Self;
using System;
using System.Configuration;
using System.ServiceProcess;
using Workbench.Agent.BusinessOne.HubClients;
using Workbench.Agent.BusinessOne.Sap;

namespace Workbench.Agent.BusinessOne.Service
{
    static class Program
    {
        public static void Main(string[] args)
        {
            //Starting Nancy service here as console application
            if (args != null && args.Length > 0 && args[0] == "debug")
            {
                using (var host = new NancyHost(new Uri(ConfigurationManager.AppSettings["ManagementConsole"])))
                {
                    host.Start();

                    Console.WriteLine("Running on " + ConfigurationManager.AppSettings["ManagementConsole"]);

                    using (var echoClient = EchoClient.Current)

                    using (var sapClient = ServerConnection.Current)
                    {
                        // attempt connection; 0 = success
                        if (sapClient.Connect() == 0)
                        {
                            Console.WriteLine("Successfully connected to " + sapClient.GetCompany().CompanyName + "!");
                            var helper = new HelperMethods(sapClient.GetCompany());
                            helper.AddUserDefinedFields();
                            helper.SaveFinCocode();

                        }
                        else
                        {
                            Console.WriteLine("Error " + sapClient.GetErrorCode() + ": " + sapClient.GetErrorMessage());
                        }

                        Console.ReadLine();
                    }
                }
            }
            else //Starting a normal windows service by default
            {
                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[]
                {
                new WorkbenchSapAgentService()
                };
                ServiceBase.Run(ServicesToRun);
            }
        }
    }
}
