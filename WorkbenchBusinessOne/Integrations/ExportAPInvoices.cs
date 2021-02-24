using SAPbobsCOM;
using System;
using System.Collections.Generic;
using System.Linq;
using Workbench.Agent.BusinessOne.Models;
using Workbench.Agent.BusinessOne.WorkbenchClient;

namespace Workbench.Agent.BusinessOne.Integrations
{
    public class ExportAPInvoices : ExportBase
    {
        public ExportAPInvoices(Client wbClient, Company sapCompany, WorkbenchTrfClient wbTrfclient) :
         base(wbClient, sapCompany, wbTrfclient)
        {
        }

        public override string Export(int batchNo)
        {
            var result = ExportProcess(batchNo);
            SettingsModelList.SetUpdateDate("LastAPInvoicesSyncDate", DateTime.Now);
            return result;
        }

        public string Export(int batchNo, string wbSessionId)
        {
            this.wbSessionId = wbSessionId;
            var result = ExportProcess(batchNo);
            return result;
        }

        private string ExportProcess(int batchNo)
        {
            var apiResult = wbTrfclient.APInvoiceTrfApi_GetAsync(batchNo);
            var apInvoices = apiResult.Result;

            Helpers.LogInfo($"{batchNo}\r\nTotal Count new: {apInvoices.Count}");

            int insertedRecords = 0;
            int invoices = 0;
            int errorCount = 0;

            var allowFurturePostingDate = sapCompany.GetCompanyService().GetAdminInfo().AllowFuturePostingDate;

            if (Validate(apInvoices, batchNo))
            {
                foreach (var invoice in apInvoices)
                {
                    try
                    {
                        var docType = BoObjectTypes.oPurchaseCreditNotes;
                        if (invoice.InvoiceCredit == 1) docType = BoObjectTypes.oPurchaseInvoices;

                        var apInvoiceDoc = (Documents)sapCompany.GetBusinessObject(docType);
                        apInvoiceDoc.CardCode = invoice.CompanyCode;
                        apInvoiceDoc.DocType = BoDocumentTypes.dDocument_Service;
                        apInvoiceDoc.UserFields.Fields.Item("U_WB_ID_NEW").Value = $"AP:{invoice.APInvoiceID}";
                        apInvoiceDoc.UserFields.Fields.Item("U_WB_Batch").Value = invoice.BatchNo;
                        apInvoiceDoc.UserFields.Fields.Item("U_WB_ClmNo").Value = invoice.ClaimNo != null ? invoice.ClaimNo.Value.ToString() : "";
                        apInvoiceDoc.HandWritten = BoYesNoEnum.tNO;
                        apInvoiceDoc.NumAtCard = invoice.InvoiceRef;
                        apInvoiceDoc.Comments = invoice.Details;
                        apInvoiceDoc.JournalMemo = invoice.InvoiceCredit == 1 ? $"A/P Credit - {invoice.FinCoCode} - {invoice.InvoiceRef}" : "";
                        apInvoiceDoc.DocDate = allowFurturePostingDate == BoYesNoEnum.tYES ? Convert.ToDateTime(invoice.PostingDate?.ToString(sapDateFormat)) : sapCompany.GetCompanyDate();
                        apInvoiceDoc.TaxDate = Convert.ToDateTime(invoice.InvoiceDate?.ToString(sapDateFormat));
                        apInvoiceDoc.Rounding = BoYesNoEnum.tYES;
                        apInvoiceDoc.DocCurrency = invoice.CurrencyCode;
                        apInvoiceDoc.DocDueDate = Convert.ToDateTime(invoice.DueDate?.ToString(sapDateFormat));
                        var apInvoiceLines = apInvoiceDoc.Lines;

                        int lineNo = 1;
                        foreach (var line in invoice.APInvoiceLinesTrf)
                        {
                            try
                            {
                                var glCode = GetGLCode(line.DrGLAccount);
                                ////TODO: remove this once glCoding is considered working
                                //Helpers.LogInfo($"{batchNo}\r\nSAP glCode:{glCode} WB glCode: {line.DrGLAccount}");

                                //if (glCode == "")
                                //{

                                //    Console.WriteLine($"glcode {glCode} {glCode == ""}");

                                //    errorCount++;
                                //    apInvoices.ToList().ForEach(a =>
                                //    {
                                //        ExportLogTrf(batchNo, a.ID.Value, "APInvoices", Type2.Error, $"{batchNo}\r\nGL Account {line.DrGLAccount}, {glCode}  not found of APInvoice line {line.ID}");
                                //    });
                                //    break;
                                //}

                                //if (!JobExists(line.JobCode))
                                //{
                                //    errorCount++;
                                //    apInvoices.ToList().ForEach(a =>
                                //    {
                                //        ExportLogTrf(batchNo, a.ID.Value, "APInvoices", Type2.Error, $"{batchNo}\r\nJob {line.JobCode} not found of APInvoice line {line.ID}");
                                //    });
                                //    break;
                                //}

                                apInvoiceLines.TaxCode = line.TaxCode;
                                apInvoiceLines.VatGroup = line.TaxCode;
                                apInvoiceLines.ItemDescription = line.LineDescription;
                                apInvoiceLines.UserFields.Fields.Item("U_WB_ID").Value = line.JobTranID.Value.ToString();
                                apInvoiceLines.UserFields.Fields.Item("U_WB_ACT").Value = line.ActivityCode;
                                apInvoiceLines.Price = line.FCCost.Value;
                                apInvoiceLines.UnitPrice = line.FCCost.Value / line.Quantity.Value;
                                apInvoiceLines.AccountCode = glCode;
                                apInvoiceLines.Quantity = line.Quantity.Value;
                                apInvoiceLines.LineTotal = line.FCCost.Value;
                                apInvoiceLines.PriceAfterVAT = line.FCCost.Value + line.FCGST.Value;
                                apInvoiceLines.TaxTotal = line.FCGST.Value;
                                apInvoiceLines.ProjectCode = line.JobCode;
                                apInvoiceLines.UserFields.Fields.Item("U_WB_WC").Value = line.WorkCentreCode;
                                apInvoiceDoc.DocRate = invoice.ExchangeRate.Value;
                                apInvoiceDoc.Project = invoice.APInvoiceLinesTrf.FirstOrDefault().JobCode;

                                apInvoiceLines.Add();
                                apInvoiceLines.SetCurrentLine(lineNo);
                                lineNo++;
                            }

                            catch (Exception ex)
                            {
                                apInvoices.ToList().ForEach(a =>
                                {
                                    ExportLogTrf(batchNo, a.ID.Value, "APInvoices", Type2.Error, $"{batchNo}\r\nError exception when exporting APInvoice line: {line.ID}\r\n{ex.Message}");
                                });
                                TransferLogTrf(wbSessionId, batchNo, Type.Error);
                                throw;
                            }
                        }
                        if (errorCount == 0)
                        {
                            if (apInvoiceDoc.Add() != 0)
                            {
                                var error = sapCompany.GetLastErrorDescription();
                                apInvoices.ToList().ForEach(a =>
                                {
                                    ExportLogTrf(batchNo, a.ID.Value, "APInvoices", Type2.Error, $"{batchNo}\r\nError from SBO: {error}. For APInvoice {invoice.APInvoiceID.Value}");
                                });
                            }
                            else
                            {
                                string docEntry = sapCompany.GetNewObjectKey();
                                invoice.ExternalID = docEntry;
                                wbTrfclient.APInvoiceTrfApi_PutAsync(invoice);
                                ExportLogTrf(batchNo, invoice.ID.Value, "APInvoices", Type2.Info, $"{batchNo}\r\nSuccess");
                                insertedRecords++;
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        apInvoices.ToList().ForEach(a =>
                        {
                            ExportLogTrf(batchNo, a.ID.Value, "APInvoices", Type2.Error, $"{batchNo}\r\nError exporting APInvoice: {invoice.APInvoiceID.Value}\r\n{ex.Message}");
                        });
                        //ExportLogTrf(batchNo, invoice.ID.Value, "APInvoices", Type2.Error, $"{batchNo}\r\nError exporting APInvoice : {invoice.APInvoiceID.Value}");
                        TransferLogTrf(wbSessionId, batchNo, Type.Error);
                        throw;
                    }
                }
            }

            if (apInvoices.Count == insertedRecords)
            {
                TransferLogTrf(wbSessionId, batchNo, Type.Info);
            }
            else
            {
                TransferLogTrf(wbSessionId, batchNo, Type.Error);
            }
            return $"{batchNo}\r\nTotal count to be exported: {apInvoices.Count}. \r\nTotal count successfully exported: {insertedRecords + invoices}. \r\nWith {insertedRecords} newly inserted";
        }

        private bool Validate(ICollection<Transfer_APInvoiceTrfApiModel> apInvoices, int batchNo)
        {
            var errorCount = 0;
            foreach (var invoice in apInvoices)
            {
                if (!SupplierCompanyExists(invoice.CompanyCode))
                {
                    errorCount++;
                    apInvoices.ToList().ForEach(a =>
                    {
                        ExportLogTrf(batchNo, a.ID.Value, "APInvoices", Type2.Error, $"{batchNo}\r\nBatch contains invalid invioice.\r\nSupplier Business Partner {invoice.CompanyCode} not found for APInvoice {invoice.APInvoiceID}.");
                    });
                    break;
                }

                if (APInvoiceExists(invoice.APInvoiceID.Value, invoice.InvoiceCredit.Value))
                {
                    errorCount++;
                    apInvoices.ToList().ForEach(a =>
                    {
                        ExportLogTrf(batchNo, a.ID.Value, "APInvoices", Type2.Error, $"{batchNo}\r\nBatch contains invioice already exported.\r\nDocument with WB ID {invoice.APInvoiceID.Value} already exists in SBO");
                    });
                    break;
                }

                foreach (var line in invoice.APInvoiceLinesTrf)
                {
                    var glCode = GetGLCode(line.DrGLAccount);

                    if (glCode == "")
                    {

                        Console.WriteLine($"glcode {glCode} {glCode == ""}");

                        errorCount++;
                        apInvoices.ToList().ForEach(a =>
                        {
                            ExportLogTrf(batchNo, a.ID.Value, "APInvoices", Type2.Error, $"{batchNo}\r\nBatch contains invalid invioice.\r\nGL Account {line.DrGLAccount}, {glCode}  not found of APInvoice line {line.ID}");
                        });
                        break;
                    }

                    if (!JobExists(line.JobCode))
                    {
                        errorCount++;
                        apInvoices.ToList().ForEach(a =>
                        {
                            ExportLogTrf(batchNo, a.ID.Value, "APInvoices", Type2.Error, $"{batchNo}\r\nBatch contains invalid invioice.\r\nJob {line.JobCode} not found of APInvoice line {line.ID}");
                        });
                        break;
                    }
                }
            }

            return errorCount == 0;
        }
    }
}
