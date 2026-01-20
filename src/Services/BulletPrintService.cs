using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using PuppeteerSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Dashboard.Services
{
    public class BulletPrintService
    {
        // RENAMED: Matches the call in BulletCalendar.razor
        public async Task<byte[]> GenerateYearlyCalendarAsync(int year, string baseUrl = "http://localhost")
        {
            using var finalDocument = new PdfDocument();
            
            var options = new LaunchOptions
            {
                ExecutablePath = "/usr/bin/chromium", 
                Headless = true,
                Args = new[] { 
                    "--no-sandbox", 
                    "--disable-setuid-sandbox",
                    "--disable-dev-shm-usage", 
                    "--single-process" 
                }
            };

            await using var browser = await Puppeteer.LaunchAsync(options);
            await using var page = await browser.NewPageAsync();

            for (int month = 1; month <= 12; month++)
            {
                // Ensure this route matches your page @page definition
                string targetUrl = $"{baseUrl.TrimEnd('/')}/year-book-export?year={year}&month={month}";
                
                await page.GoToAsync(targetUrl, WaitUntilNavigation.Networkidle2);
                
                var monthPdfData = await page.PdfDataAsync(new PdfOptions { 
                    Format = PuppeteerSharp.Media.PaperFormat.Letter, 
                    PrintBackground = true,
                    MarginOptions = new PuppeteerSharp.Media.MarginOptions
                    {
                        Top = "0in", Bottom = "0in", Left = "0in", Right = "0in"
                    }
                });

                using var monthStream = new MemoryStream(monthPdfData);
                using var monthDoc = PdfReader.Open(monthStream, PdfDocumentOpenMode.Import);
                
                // Spread logic for professional printing
                if (finalDocument.PageCount > 0 && finalDocument.PageCount % 2 != 0)
                {
                    finalDocument.AddPage(); 
                }

                foreach (PdfPage p in monthDoc.Pages)
                {
                    finalDocument.AddPage(p);
                }
            }

            using var outStream = new MemoryStream();
            finalDocument.Save(outStream);
            return outStream.ToArray();
        }
    }
}