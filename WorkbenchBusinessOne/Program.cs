using Microsoft.AspNet.SignalR.Client;
using Nancy.Hosting.Self;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Workbench.Agent.BusinessOne.HubClients;
using Workbench.Agent.BusinessOne.Properties;
using Workbench.Agent.BusinessOne.Sap;

namespace Workbench.Agent.BusinessOne
{
    public class Program
    {
        public static void Main(string[] args)
        {
            #if DEBUG
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
            #endif
        }
    }
}
