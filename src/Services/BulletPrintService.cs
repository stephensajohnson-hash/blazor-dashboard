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
            var cleanBaseUrl = baseUrl.TrimEnd('/');

            var options = new LaunchOptions
            {
                ExecutablePath = "/usr/bin/chromium", 
                Headless = true,
                Args = new[] { 
                    "--no-sandbox", 
                    "--disable-setuid-sandbox",
                    "--disable-dev-shm-usage", 
                    "--disable-web-security", // Added to help internal navigation
                    "--single-process" 
                }
            };

            await using var browser = await Puppeteer.LaunchAsync(options);
            await using var page = await browser.NewPageAsync();

            // SURGICAL FIX: Bypass CSP to prevent the Referrer Policy protocol error
            await page.SetBypassCSPAsync(true);
            
            await page.SetViewportAsync(new ViewPortOptions { Width = 816, Height = 1056 });

            for (int month = 1; month <= 12; month++)
            {
                // Clean URL string to avoid object-based encoding issues
                string targetUrl = $"{cleanBaseUrl}/year-book-export?year={year}&month={month}";
                
                // Use 'Load' instead of 'Networkidle2' to avoid timeouts on Render.com
                await page.GoToAsync(targetUrl, new NavigationOptions { 
                    WaitUntil = new[] { WaitUntilNavigation.Load },
                    Timeout = 60000 
                });

                // Wait an extra second for Tailwind/JS to settle
                await Task.Delay(1000);
                
                var monthPdfData = await page.PdfDataAsync(new PdfOptions { 
                    Format = PuppeteerSharp.Media.PaperFormat.Letter, 
                    PrintBackground = true,
                    MarginOptions = new PuppeteerSharp.Media.MarginOptions { Top = "0in", Bottom = "0in", Left = "0in", Right = "0in" }
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