using Nancy;
using Workbench.Agent.BusinessOne.Integrations;
using Workbench.Agent.BusinessOne.Models;

namespace Workbench.Agent.BusinessOne.Controllers
{
    public class MainController : BaseController<MainModel>
    {
        public MainController(ImportCompanies importCompanies,
            ImportPayments importPayments,
            ImportJobReceipts importJobReceipts,
            ExportJobs exportJobs,
            ImportExchangeRates importExchangeRates,
            ExportGLJournals exportGLJournals,
            ExportAPInvoices exportAPInvoices,
            ExportARInvoices exportARInvoices)
        {
            Get("/", args => View["Index.html", Model]);

            #region Imports

            Post("/SyncCompanies", args =>
            {
                Helpers.LogInfo($"Start Sync Companies");
                var result = importCompanies.Import();
                return Response.AsJson(new { message = $"Companies have been imported \r\n{result}" });

            });

            Post("/SyncPayments", args =>
            {
                Helpers.LogInfo($"Start Sync Payments");
                var result = importPayments.Import();
                return Response.AsJson(new { message = $"Payments have been imported \r\n{result}" });

            });

            Post("/SyncJobReceipts", args =>
            {
                Helpers.LogInfo($"Start Sync Job Receipts");
                var result = importJobReceipts.Import();
                return Response.AsJson(new { message = $"JobReceipts have been imported \r\n{result}" });

            });

            Post("/UpdateTime", args =>
            {
                var type = this.Request.Form["type"];
                var date = this.Request.Form["val"];
                SettingsModelList.SetUpdateDate(type, date);

                return Response.AsJson(new { message = $"{type} updated" });

            });

            Post("/SyncExchangeRates", args =>
            {
                Helpers.LogInfo($"Start Sync Exchange Rates");
                var result = importExchangeRates.Import();

                return Response.AsJson(new { message = $"Exchange Rates synced" });

            });

            #endregion

            #region Exports

            Post("/SyncJobs", args =>
            {
                Helpers.LogInfo($"Start Sync Jobs");
                var result = exportJobs.Export();
                return Response.AsJson(new { message = $"Jobs have been exported \r\n{result}" });

            });

            Post("/SyncGLJournals", args =>
            {
                int batchNo = Request.Form["batchNo"];
                var result = exportGLJournals.Export(batchNo);
                return Response.AsJson(new { message = $"GLJournals have been exported \r\n{result}" });

            });

            Post("/SyncAPInvoices", args =>
            {
                int batchNo = Request.Form["batchNo"];
                var result = exportAPInvoices.Export(batchNo);
                return Response.AsJson(new { message = $"APInvoices have been exported \r\n{result}" });

            });

            Post("/SyncARInvoices", args =>
            {
                int batchNo = Request.Form["batchNo"];
                var result = exportARInvoices.Export(batchNo);
                return Response.AsJson(new { message = $"APInvoices have been exported \r\n{result}" });

            });

            #endregion
        }

        public override MainModel Init()
        {
            return new MainModel()
            {

            };
        }
    }
}
