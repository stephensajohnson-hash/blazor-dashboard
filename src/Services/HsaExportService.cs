using PuppeteerSharp;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using PdfSharp.Drawing;
using System.Text;

namespace Dashboard.Services;

public class HsaExportService
{
    public async Task<byte[]> CreateSubmissionPdf(List<HsaReceipt> receipts)
    {
        // 1. GENERATE THE LEDGER PAGE VIA PUPPETEER (HTML TO PDF)
        var html = BuildLedgerHtml(receipts);
        
        using var browserFetcher = new BrowserFetcher();
        await browserFetcher.DownloadAsync();
        using var browser = await Puppeteer.LaunchAsync(new LaunchOptions { Headless = true });
        using var page = await browser.NewPageAsync();
        await page.SetContentAsync(html);
        var ledgerPdfBytes = await page.PdfDataAsync(new PdfOptions { Format = PaperFormat.A4 });
        await browser.CloseAsync();

        // 2. MERGE EVERYTHING VIA PDFSHARP
        using var outputDocument = new PdfDocument();
        
        // Add the Ledger first
        using (var ledgerStream = new MemoryStream(ledgerPdfBytes))
        {
            var ledgerDoc = PdfReader.Open(ledgerStream, PdfDocumentOpenMode.Import);
            CopyPages(ledgerDoc, outputDocument);
        }

        // Add Attachments
        foreach (var r in receipts.Where(x => x.FileData != null && x.FileData.Length > 0))
        {
            try 
            {
                if (r.ContentType?.Contains("pdf") == true)
                {
                    using var ms = new MemoryStream(r.FileData);
                    var attachment = PdfReader.Open(ms, PdfDocumentOpenMode.Import);
                    CopyPages(attachment, outputDocument);
                }
                else if (r.ContentType?.Contains("image") == true)
                {
                    var newPage = outputDocument.AddPage();
                    using var ms = new MemoryStream(r.FileData);
                    using var img = XImage.FromStream(ms);
                    
                    // Basic scaling to fit page
                    var graphics = XGraphics.FromPdfPage(newPage);
                    graphics.DrawImage(img, 0, 0, newPage.Width, (newPage.Width / img.PixelWidth) * img.PixelHeight);
                }
            }
            catch { /* Log failure for specific attachment if needed */ }
        }

        using var finalMs = new MemoryStream();
        outputDocument.Save(finalMs);
        return finalMs.ToArray();
    }

    private void CopyPages(PdfDocument from, PdfDocument to)
    {
        for (int i = 0; i < from.PageCount; i++) to.AddPage(from.Pages[i]);
    }

    private string BuildLedgerHtml(List<HsaReceipt> receipts)
    {
        var sb = new StringBuilder();
        sb.Append(@"<html><head><style>
            body { font-family: sans-serif; padding: 40px; }
            h1 { color: #2563eb; margin-bottom: 5px; }
            table { width: 100%; border-collapse: collapse; margin-top: 20px; }
            th { text-align: left; border-bottom: 2px solid #000; padding: 10px; font-size: 12px; text-transform: uppercase; }
            td { padding: 10px; border-bottom: 1px solid #eee; font-size: 11px; }
            .amt { text-align: right; font-family: monospace; }
            .total { font-weight: bold; background: #f8fafc; }
        </style></head><body>");

        sb.Append("<h1>HSA Reimbursement Submission</h1>");
        sb.Append($"<p>Generated: {DateTime.Now:yyyy-MM-dd HH:mm}</p>");
        sb.Append("<table><thead><tr><th>Date</th><th>Provider</th><th>Category</th><th class='amt'>Amount</th></tr></thead><tbody>");

        foreach (var r in receipts)
        {
            sb.Append($"<tr><td>{r.ServiceDate:yyyy-MM-dd}</td><td>{r.Provider}</td><td>{r.Type}</td><td class='amt'>${r.Amount:N2}</td></tr>");
        }

        sb.Append($"<tr class='total'><td colspan='3' style='text-align:right'>Total Submission:</td><td class='amt'>${receipts.Sum(x => x.Amount):N2}</td></tr>");
        sb.Append("</tbody></table></body></html>");

        return sb.ToString();
    }
}