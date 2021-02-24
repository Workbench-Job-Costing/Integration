using SAPbobsCOM;
using System;
using System.Collections.Generic;
using System.Linq;
using Workbench.Agent.BusinessOne.Models;
using Workbench.Agent.BusinessOne.WorkbenchClient;

namespace Workbench.Agent.BusinessOne.Integrations
{
    public class ExportARInvoices : ExportBase
    {
        public ExportARInvoices(Client wbClient, Company sapCompany, WorkbenchTrfClient wbTrfclient) :
         base(wbClient, sapCompany, wbTrfclient)
        {
        }

        public override string Export(int batchNo)
        {
            var result = ExportProcess(batchNo);
            SettingsModelList.SetUpdateDate("LastARInvoicesSyncDate", DateTime.Now);
            return result;
        }

        public string Export(int batchNo, string wbSessionId)
        {
            this.wbSessionId = wbSessionId;
            var result = ExportProcess(batchNo);
            SettingsModelList.SetUpdateDate("LastARInvoicesSyncDate", DateTime.Now);
            return result;
        }

        private string ExportProcess(int batchNo)
        {
            var apiResult = wbTrfclient.ARInvoiceTrfApi_GetAsync(batchNo);
            var arInvoices = apiResult.Result;

            Helpers.LogInfo($"{batchNo}\r\nTotal Count new: {arInvoices.Count}");

            int insertedRecords = 0;
            int invoices = 0;
            int errorCount = 0;

            var allowFurturePostingDate = sapCompany.GetCompanyService().GetAdminInfo().AllowFuturePostingDate;

            if (Validate(arInvoices, batchNo))
            {
                foreach (var invoice in arInvoices)
                {
                    try
                    {
                        var docType = BoObjectTypes.oCreditNotes;
                        if (invoice.InvoiceCredit == 1) docType = BoObjectTypes.oInvoices;

                        sapCompany.StartTransaction();

                        var arInvoiceDoc = (Documents)sapCompany.GetBusinessObject(docType);
                        arInvoiceDoc.DocNum = invoice.InvoiceNo.Value;
                        arInvoiceDoc.CardCode = invoice.CompanyCode;
                        arInvoiceDoc.DocRate = invoice.ExchangeRate.Value;
                        arInvoiceDoc.DocType = BoDocumentTypes.dDocument_Service;
                        arInvoiceDoc.UserFields.Fields.Item("U_WB_ID").Value = $"AR:{invoice.InvoiceNo}";
                        arInvoiceDoc.UserFields.Fields.Item("U_WB_Batch").Value = invoice.BatchNo;
                        arInvoiceDoc.UserFields.Fields.Item("U_WB_ClmNo").Value = invoice.ClaimNo != null ? invoice.ClaimNo.Value.ToString() : "";
                        arInvoiceDoc.HandWritten = BoYesNoEnum.tYES;
                        arInvoiceDoc.Series = 23;
                        arInvoiceDoc.NumAtCard = invoice.InvoiceRef;
                        arInvoiceDoc.Comments = invoice.Details;
                        arInvoiceDoc.DocDate = allowFurturePostingDate == BoYesNoEnum.tYES ? Convert.ToDateTime(invoice.PostingDate?.ToString(sapDateFormat)) : sapCompany.GetCompanyDate();
                        arInvoiceDoc.TaxDate = Convert.ToDateTime(invoice.InvoiceDate?.ToString(sapDateFormat));
                        //arInvoiceDoc.DocCurrency = invoice.CurrencyCode; //error when ar credit
                        arInvoiceDoc.DocDueDate = Convert.ToDateTime(invoice.DueDate?.ToString(sapDateFormat));
                        var arInvoiceLines = arInvoiceDoc.Lines;

                        int lineNo = 1;
                        foreach (var line in invoice.ARInvoiceLinesTrf)
                        {
                            try
                            {
                                var glCode = GetGLCode(line.CrGLAccount);
                                ////TODO: remove this once glCoding is considered working
                                //Helpers.LogInfo($"SAP glCode:{glCode} WB glCode: {line.CrGLAccount}");

                                //if (glCode == "")
                                //{

                                //    errorCount++;
                                //    arInvoices.ToList().ForEach(a =>
                                //    {
                                //        ExportLogTrf(batchNo, invoice.ID.Value, "ARInvoices", Type2.Error, $"{batchNo}\r\nGL Account {line.CrGLAccount} not found of ARInvoice line {line.ID}:");
                                //    });
                                //    break;
                                //}

                                //if (!JobExists(line.JobCode))
                                //{
                                //    errorCount++;
                                //    arInvoices.ToList().ForEach(a =>
                                //    {
                                //        ExportLogTrf(batchNo, a.ID.Value, "ARInvoices", Type2.Error, $"{batchNo}\r\nJob {line.JobCode} not found of ARInvoice line {line.ID}");
                                //    });
                                //    break;
                                //}

                                arInvoiceLines.TaxCode = line.TaxCode;
                                arInvoiceLines.VatGroup = line.TaxCode;
                                arInvoiceLines.ItemDescription = line.LineDescription;
                                arInvoiceLines.UserFields.Fields.Item("U_WB_ID").Value = line.InvoiceLineID.Value.ToString();
                                arInvoiceLines.Price = line.FCAmount.Value;
                                arInvoiceLines.UnitPrice = line.FCAmount.Value / line.Quantity.Value;
                                arInvoiceLines.AccountCode = glCode;
                                arInvoiceLines.Quantity = line.Quantity.Value;
                                arInvoiceLines.PriceAfterVAT = line.FCAmount.Value + line.FCGST.Value;
                                arInvoiceLines.TaxTotal = line.FCGST.Value;
                                arInvoiceLines.ProjectCode = line.JobCode;
                                arInvoiceLines.GrossTotalFC = line.Amount.Value;
                                arInvoiceLines.LineTotal = line.FCAmount.Value;
                                if (line.WorkCentreCode != null) arInvoiceLines.UserFields.Fields.Item("U_WB_WC").Value = line.WorkCentreCode;
                                //arInvoiceLines.Currency = invoice.CurrencyCode; //error when ar credit
                                arInvoiceDoc.Project = invoice.ARInvoiceLinesTrf.FirstOrDefault().JobCode;

                                arInvoiceLines.Add();
                                arInvoiceLines.SetCurrentLine(lineNo);
                                lineNo++;
                            }

                            catch (Exception ex)
                            {
                                arInvoices.ToList().ForEach(a =>
                                {
                                    ExportLogTrf(batchNo, a.ID.Value, "ARInvoices", Type2.Error, $"{batchNo}\r\nError exception when exporting ARInvoice line: {line.ID}\r\n{ex.Message}");
                                });
                                TransferLogTrf(wbSessionId, batchNo, Type.Error);
                            }
                        }
                        if (errorCount == 0)
                        {
                            if (arInvoiceDoc.Add() != 0)
                            {
                                var error = sapCompany.GetLastErrorDescription();
                                arInvoices.ToList().ForEach(a =>
                                {
                                    ExportLogTrf(batchNo, a.ID.Value, "ARInvoices", Type2.Error, $"{batchNo}\r\nError from SBO: {error}. For ARInvoice {invoice.ID.Value}");
                                    //ExportLogTrf(batchNo, invoice.ID.Value, "ARInvoices", Type2.Error, $"{batchNo}\r\nError from SBO : {error}. For ARInvoice {invoice.InvoiceNo.Value}");
                                });
                            }
                            else
                            {
                                string docEntry = sapCompany.GetNewObjectKey();
                                invoice.ExternalID = docEntry;
                                wbTrfclient.ARInvoiceTrfApi_PutAsync(invoice);
                                ExportLogTrf(batchNo, invoice.ID.Value, "ARInvoices", Type2.Info, $"{batchNo}\r\nSuccess");
                                insertedRecords++;
                            }
                        }

                        sapCompany.EndTransaction(BoWfTransOpt.wf_Commit);
                    }
                    catch (Exception ex)
                    {
                        arInvoices.ToList().ForEach(a =>
                        {
                            ExportLogTrf(batchNo, a.ID.Value, "ARInvoices", Type2.Error, $"{batchNo}\r\nError exporting ARInvoice: {invoice.ID.Value}\r\n{ex.Message}\r\n{ex.InnerException}\r\n{ex.StackTrace}\r\n{ex.Source}");
                        });
                        //ExportLogTrf(batchNo, invoice.ID.Value, "ARInvoices", Type2.Error, $"{batchNo}\r\nError exporting ARInvoice : {invoice.InvoiceNo.Value}");
                        TransferLogTrf(wbSessionId, batchNo, Type.Error);
                    }
                }
            }

            if (arInvoices.Count == insertedRecords) TransferLogTrf(wbSessionId, batchNo, Type.Info);
            else TransferLogTrf(wbSessionId, batchNo, Type.Error);
            return $"{batchNo}\r\nTotal count to be exported: {arInvoices.Count}. \r\nTotal count successfully exported: {insertedRecords + invoices}. \r\nWith {insertedRecords} newly inserted";
        }

        private bool Validate(ICollection<Transfer_ARInvoiceTrfApiModel> arInvoices, int batchNo)
        {
            var errorCount = 0;
            foreach (var invoice in arInvoices)
            {
                if (!CustomerCompanyExists(invoice.CompanyCode))
                {
                    errorCount++;
                    arInvoices.ToList().ForEach(a =>
                    {
                        ExportLogTrf(batchNo, a.ID.Value, "ARInvoices", Type2.Error, $"{batchNo}\r\nBatch contains invalid invioice.\r\nCustomer Business Partner {invoice.CompanyCode} not found for ARInvoice {invoice.ID}.");
                    });
                    //ExportLogTrf(batchNo, invoice.ID.Value, "ARInvoices", Type2.Error, $"{batchNo}\r\nCustomer Business Partner {invoice.CompanyCode} not found.");
                    continue;
                }

                if (ARInvoiceExists(invoice.InvoiceNo.Value, invoice.InvoiceCredit.Value))
                {
                    errorCount++;
                    arInvoices.ToList().ForEach(a =>
                    {
                        ExportLogTrf(batchNo, a.ID.Value, "ARInvoices", Type2.Error, $"{batchNo}\r\nBatch contains invioice already exported.\r\nDocument with WB ID {invoice.ID.Value} already exists in SBO");
                    });
                    //ExportLogTrf(batchNo, invoice.ID.Value, "ARInvoices", Type2.Error, $"{batchNo}\r\nDocument with WB ID {invoice.InvoiceNo.Value} already exists in SBO");
                    continue;
                }

                foreach (var line in invoice.ARInvoiceLinesTrf)
                {
                    var glCode = GetGLCode(line.CrGLAccount);

                    if (glCode == "")
                    {
                        errorCount++;
                        arInvoices.ToList().ForEach(a =>
                        {
                            ExportLogTrf(batchNo, invoice.ID.Value, "ARInvoices", Type2.Error, $"{batchNo}\r\nBatch contains invalid invioice.\r\nGL Account {line.CrGLAccount} not found of ARInvoice line {line.ID}");
                        });
                        break;
                    }

                    if (!JobExists(line.JobCode))
                    {
                        errorCount++;
                        arInvoices.ToList().ForEach(a =>
                        {
                            ExportLogTrf(batchNo, a.ID.Value, "ARInvoices", Type2.Error, $"{batchNo}\r\nBatch contains invalid invioice.\r\nJob {line.JobCode} not found of ARInvoice line {line.ID}");
                        });
                        break;
                    }
                }
            }

            return errorCount == 0;
        }

    }
}
