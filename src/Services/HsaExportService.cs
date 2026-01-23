using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Dashboard;

namespace Dashboard.Services;

public class HsaExportService
{
    public byte[] CreateSubmissionPdf(List<HsaReceipt> receipts)
    {
        // QuestPDF License (Community is free for personal use)
        QuestPDF.Settings.License = LicenseType.Community;

        return Document.Create(container =>
        {
            // PAGE 1: THE LEDGER
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Verdana));

                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("HSA Reimbursement Submission").FontSize(20).SemiBold().FontColor(Colors.Blue.Medium);
                        col.Item().Text($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}");
                    });
                });

                page.Content().PaddingVertical(10).Column(col =>
                {
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(80);  // Date
                            columns.RelativeColumn();    // Provider
                            columns.ConstantColumn(100); // Category
                            columns.ConstantColumn(80);  // Amount
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(CellStyle).Text("Date");
                            header.Cell().Element(CellStyle).Text("Provider");
                            header.Cell().Element(CellStyle).Text("Category");
                            header.Cell().Element(CellStyle).AlignRight().Text("Amount");
                        });

                        foreach (var r in receipts)
                        {
                            table.Cell().Element(RowStyle).Text(r.ServiceDate.ToString("yyyy-MM-dd"));
                            table.Cell().Element(RowStyle).Text(r.Provider ?? "---");
                            table.Cell().Element(RowStyle).Text(r.Type ?? "---");
                            table.Cell().Element(RowStyle).AlignRight().Text($"${r.Amount:N2}");
                        }

                        table.Footer(footer =>
                        {
                            footer.Cell().ColumnSpan(3).Padding(5).AlignRight().Text("Total Submission:").SemiBold();
                            footer.Cell().Padding(5).AlignRight().Text($"${receipts.Sum(x => x.Amount):N2}").SemiBold();
                        });
                    });
                });
            });

            // SUBSEQUENT PAGES: THE ATTACHMENTS
            foreach (var r in receipts.Where(x => x.FileData != null))
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1, Unit.Centimetre);
                    page.Header().Text($"Attachment: {r.Provider} ({r.ServiceDate:yyyy-MM-dd})").FontSize(12).Grey();
                    
                    page.Content().PaddingTop(10).AlignCenter().Element(e => {
                        try {
                            // If it's an image, QuestPDF handles it directly
                            if (r.ContentType?.Contains("image") == true)
                                e.Image(r.FileData);
                            else
                                e.Text("[PDF Attachment - See following pages in combined view]");
                        } catch {
                            e.Text("Error rendering attachment.");
                        }
                    });
                });
            }
        }).GeneratePdf();
    }

    // Table Helpers
    static IContainer CellStyle(IContainer container) => container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);
    static IContainer RowStyle(IContainer container) => container.PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Grey.Lighten2);
}