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
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.Letter);
                    page.MarginTop(0.5f, Unit.Inch);
                    page.MarginBottom(0.5f, Unit.Inch);
                    page.MarginLeft(0.16f, Unit.Inch);
                    page.MarginRight(0.16f, Unit.Inch);

                    page.Content().Grid(grid =>
                    {
                        grid.Columns(2); 

                        // 1. Handle Offset
                        for (int i = 1; i < startPos; i++)
                        {
                            grid.Item().Height(2, Unit.Inch).Text("");
                        }

                        var orders = items.GroupBy(x => x.OrderId);

                        foreach (var orderGroup in orders)
                        {
                            var firstItem = orderGroup.First();
                            var order = firstItem.ParentOrderContainer;
                            var address = order?.Address;

                            // A. BAG LABEL (With Padding for "Safe Zone")
                            grid.Item().Height(2, Unit.Inch).Padding(10).Column(col =>
                            {
                                col.Item().Row(row => {
                                    row.RelativeItem().Column(c => {
                                        c.Item().Text(owner.BusinessName).FontSize(14).Bold();
                                        c.Item().Text("BAG LABEL").FontSize(8).SemiBold().FontColor(Colors.Green.Medium);
                                    });
                                    
                                    // Properly handle the logo bytes
                                    if (logoBytes != null && logoBytes.Length > 0)
                                    {
                                        row.ConstantItem(40).Height(40).Image(logoBytes);
                                    }
                                });

                                col.Item().PaddingTop(5).Text(t => {
                                    t.Line($"Order #{orderGroup.Key}").FontSize(10).Bold();
                                    t.Line($"{order?.CustomerIdentifier}").FontSize(9);
                                    if (address != null) t.Line($"{address.Street}, {address.City}").FontSize(8);
                                });

                                col.Item().AlignBottom().Text($"{orderGroup.Count()} ITEMS IN BAG").FontSize(10).Bold().FontColor(Colors.Grey.Medium);
                            });

                            // B. CONTAINER LABELS (With Padding for "Safe Zone")
                            foreach (var lineItem in orderGroup)
                            {
                                grid.Item().Height(2, Unit.Inch).Padding(10).Column(col =>
                                {
                                    col.Item().Row(row => {
                                        row.RelativeItem().Text($"Order #{lineItem.OrderId}").FontSize(8).Bold();
                                        if (!string.IsNullOrEmpty(lineItem.LabelName))
                                            row.ConstantItem(80).Background(Colors.Grey.Lighten4).PaddingHorizontal(5).Text($"FOR: {lineItem.LabelName}").FontSize(9).Bold();
                                    });

                                    col.Item().PaddingTop(5).Text(lineItem.RecipeName.ToUpper()).FontSize(12).Bold();
                                    col.Item().Text(lineItem.SizeName).FontSize(8);

                                    if (lineItem.SelectedOptions != null && lineItem.SelectedOptions.Any())
                                    {
                                        var optString = string.Join(", ", lineItem.SelectedOptions.Select(o => o.OptionName));
                                        col.Item().Text($"+ {optString}").FontSize(7).FontColor(Colors.Blue.Medium);
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