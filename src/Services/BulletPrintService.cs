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
        public async Task<byte[]> GenerateYearlyCalendarAsync(int year, string baseUrl = "http://localhost")
        {
            using var finalDocument = new PdfDocument();
            
            // Ensure baseUrl is not empty and has no trailing slash to prevent double-slashes
            if (string.IsNullOrWhiteSpace(baseUrl)) baseUrl = "http://localhost";
            var cleanBaseUrl = baseUrl.TrimEnd('/');

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

            // Set a default Referrer Policy to avoid the "Invalid referrerPolicy" error
            await page.SetExtraHTTPHeadersAsync(new Dictionary<string, string> {
                { "Referrer-Policy", "no-referrer" }
            });

            for (int month = 1; month <= 12; month++)
            {
                // SURGICAL: Build URL explicitly to avoid protocol parsing errors
                string targetUrl = $"{cleanBaseUrl}/year-book-export?year={year}&month={month}";
                
                // Use WaitUntil.Networkidle0 for more reliable Tailwind/CSS loading
                await page.GoToAsync(targetUrl, new NavigationOptions { 
                    WaitUntil = new[] { WaitUntilNavigation.Networkidle0 } 
                });
                
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