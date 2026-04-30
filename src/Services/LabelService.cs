using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Dashboard; 

namespace Dashboard.Services
{
    public class LabelService
    {
        public static async Task<byte[]> CreateAveryLabels(PPP_Owner owner, byte[]? logoBytes, List<PPP_OrderItem> items, int startPos)
        {
            // Avery 5163: 2 columns, 5 rows
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.Letter);
                    page.MarginTop(0.5f, Unit.Inch);
                    page.MarginBottom(0.5f, Unit.Inch);
                    page.MarginLeft(0.16f, Unit.Inch);
                    page.MarginRight(0.16f, Unit.Inch);

                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                        });

                        // 1. Handle Offset (Empty Labels)
                        for (int i = 1; i < startPos; i++)
                        {
                            table.Cell().Height(2, Unit.Inch).Padding(10).Text("");
                        }

                        var orders = items.GroupBy(x => x.OrderId);

                        foreach (var orderGroup in orders)
                        {
                            var firstItem = orderGroup.First();
                            var order = firstItem.ParentOrderContainer;
                            var address = order?.Address;
                            var itemCount = orderGroup.Count();
                            
                            // Handling pluralization for order summary
                            var itemText = itemCount == 1 ? "1 Item in Order" : $"{itemCount} Items in Order";

                            // A. MAIN ORDER SUMMARY LABEL (formerly Bag Label)
                            table.Cell().Height(2, Unit.Inch).Padding(10).Column(col =>
                            {
                                col.Item().Row(row => {
                                    row.RelativeItem().Column(c => {
                                        c.Item().Text($"ORDER #{orderGroup.Key}").FontSize(10).ExtraBold();
                                        c.Item().PaddingTop(2).Text(owner.BusinessName).FontSize(12).Bold();
                                    });
                                    
                                    if (logoBytes != null && logoBytes.Length > 0)
                                    {
                                        row.ConstantItem(35).Height(35).Image(logoBytes).FitArea();
                                    }
                                });

                                col.Item().PaddingTop(5).Text(t => {
                                    t.Line($"{order?.CustomerIdentifier}").FontSize(9);
                                    if (address != null) 
                                        t.Line($"{address.Street}, {address.City}").FontSize(8);
                                });

                                col.Item().AlignBottom().Text(itemText).FontSize(10).Bold().FontColor(Colors.Grey.Medium);
                            });

                            // B. INDIVIDUAL CONTAINER LABELS
                            foreach (var lineItem in orderGroup)
                            {
                                table.Cell().Height(2, Unit.Inch).Padding(10).Column(col =>
                                {
                                    col.Item().Row(row => {
                                        row.RelativeItem().Text($"ORDER #{lineItem.OrderId}").FontSize(10).ExtraBold();
                                        
                                        if (!string.IsNullOrEmpty(lineItem.LabelName))
                                            row.RelativeItem().AlignRight().Text($"FOR: {lineItem.LabelName}").FontSize(9).Bold();
                                    });

                                    col.Item().PaddingTop(4).Text(lineItem.RecipeName.ToUpper()).FontSize(11).Bold();
                                    col.Item().Text(lineItem.SizeName).FontSize(8);

                                    if (lineItem.SelectedOptions != null && lineItem.SelectedOptions.Any())
                                    {
                                        var optString = string.Join(", ", lineItem.SelectedOptions.Select(o => o.OptionName));
                                        // Using a high-readability Red (Red.Darken2) for contrast on white background
                                        col.Item().Text($"+ {optString}").FontSize(8).Bold().FontColor(Colors.Red.Darken2);
                                    }
                                    
                                    col.Item().AlignBottom().Row(row => {
                                        row.RelativeItem().Text("Prep Date: " + lineItem.ScheduledDate.ToString("MM/dd/yy")).FontSize(7).FontColor(Colors.Grey.Medium);
                                    });
                                });
                            }
                        }
                    });
                });
            });

            return document.GeneratePdf();
        }
    }
}