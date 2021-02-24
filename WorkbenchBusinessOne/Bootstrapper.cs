using System;
using System.Security.Claims;
using System.Security.Principal;
using System.Net.Http;
using System.Net.Http.Headers;

using Nancy;
using Nancy.Authentication.Basic;
using Nancy.Bootstrapper;
using Nancy.TinyIoc;
using Workbench.Agent.BusinessOne.Properties;
using Workbench.Agent.BusinessOne.Sap;
using Workbench.Agent.BusinessOne.Integrations;
using Workbench.Agent.BusinessOne.WorkbenchClient;
using SAPbobsCOM;
using Workbench.Agent.BusinessOne.Models;
using Workbench.Agent.BusinessOne.HubClients;
using System.Diagnostics;
using Nancy.Configuration;
using System.Configuration;

namespace Workbench.Agent.BusinessOne
{
    public class Bootstrapper : DefaultNancyBootstrapper
    {
        public static ExportClient current;
        public static WorkbenchTrfClient workbenchTrfClient;
        protected override void ApplicationStartup(TinyIoCContainer container, IPipelines pipelines)
        {
            try
            {

                base.ApplicationStartup(container, pipelines);

                pipelines.EnableBasicAuthentication(new BasicAuthenticationConfiguration(
                    container.Resolve<IUserValidator>(),
                    "MyRealm"));

                var httpClient = new HttpClient()
                {
                    BaseAddress = new Uri(ConfigurationManager.AppSettings["WorkbenchUrl"])
                };

                httpClient.DefaultRequestHeaders.Authorization = !string.IsNullOrEmpty(ConfigurationManager.AppSettings["WorkbenchApiKey"]) ?
                    new AuthenticationHeaderValue("Bearer ", ConfigurationManager.AppSettings["WorkbenchApiKey"]) :
                    new AuthenticationHeaderValue("Basic", (ConfigurationManager.AppSettings["WorkbenchUserName"] + ":" + ConfigurationManager.AppSettings["WorkbenchPassword"].FromBase64()).ToBase64());

                var sapClient = ServerConnection.Current;
                var wbtrfClient = new WorkbenchTrfClient(new HttpClient());
                var client = new Client(httpClient);
                var company = sapClient.GetCompany();

                container.Register((c, p) =>
                {
                    return new Client(httpClient);
                });

                container.Register((c, p) =>
                {
                    return ServerConnection.Current;
                });

                container.Register((c, p) =>
                {
                    return ServerConnection.Current.GetCompany();
                });


                container.Register((c, p) =>
                {

                    return new ImportBase(client, company, wbtrfClient);
                });


                container.Register((c, p) =>
                {

                    return new ExportBase(client, company, wbtrfClient);
                });


                container.Register((c, p) =>
                {
                    return new ExportJobs(client, company, wbtrfClient);
                });

                container.Register((c, p) =>
                {
                    return new ExportClient(new ExportJobs(client, company, wbtrfClient),
                       new ExportGLJournals(client, company, wbtrfClient),
                       new ExportAPInvoices(client, company, wbtrfClient),
                       new ExportARInvoices(client, company, wbtrfClient));
                });

                container.Register((c, p) =>
                {
                    return new ExportProcessBatch(new ExportJobs(client, company, wbtrfClient),
                       new ExportGLJournals(client, company, wbtrfClient),
                       new ExportAPInvoices(client, company, wbtrfClient),
                       new ExportARInvoices(client, company, wbtrfClient));
                });

                container.Register((c, p) =>
                {
                    return new ExportAPInvoices(client, company, wbtrfClient);
                });

                current = (ExportClient)container.Resolve(typeof(ExportClient));
            }
            catch (Exception ex)
            {
                Helpers.LogAppError($"{ex.InnerException} {ex.Message}");
            }

        }

        public override void Configure(INancyEnvironment environment)
        {
            environment.Tracing(enabled: false, displayErrorTraces: true);
        }
    }

    public class UserValidator : IUserValidator
{
    public ClaimsPrincipal Validate(string username, string password)
    {
        if (username == ConfigurationManager.AppSettings["BusinessOneUserName"] && password == Helpers.FromBase64(ConfigurationManager.AppSettings["BusinessOnePassword"]))
        {
            return new ClaimsPrincipal(new GenericIdentity(username));
        }

        // Not recognised => anonymous.
        return null;
    }
}
}
