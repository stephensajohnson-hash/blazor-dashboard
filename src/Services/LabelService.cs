using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Dashboard.Models;

namespace Dashboard.Services
{
    public class LabelService
    {
        public static async Task<byte[]> CreateAveryLabels(PPP_Owner owner, List<PPP_OrderItem> items, int startPos)
        {
            // Avery 5163: 2" x 4", 10 labels per page (2 columns, 5 rows)
            // Margins: Top 0.5", Bottom 0.5", Left 0.156", Right 0.156"
            
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
                        grid.Columns(2); // 2 columns

                        // 1. Handle Offset (Empty Labels)
                        for (int i = 1; i < startPos; i++)
                        {
                            grid.Item().Height(2, Unit.Inch).Padding(5).Text("");
                        }

                        // 2. Generate Labels grouped by Order
                        var orders = items.GroupBy(x => x.OrderId);

                        foreach (var orderGroup in orders)
                        {
                            var firstItem = orderGroup.First();
                            var order = firstItem.ParentOrderContainer;
                            var address = order?.Address;

                            // A. BAG LABEL
                            grid.Item().Height(2, Unit.Inch).Padding(10).Border(0.5f).BorderColor(Colors.Grey.Lighten3).Column(col =>
                            {
                                col.Item().Row(row => {
                                    row.RelativeItem().Column(c => {
                                        c.Item().Text(owner.BusinessName).FontSize(14).Black();
                                        c.Item().Text("BAG LABEL").FontSize(8).SemiBold().FontColor(Colors.Emerald.Medium);
                                    });
                                    if (owner.LogoId.HasValue) 
                                        row.ConstantItem(40).Height(40).Image($"/db-images-ppp/{owner.LogoId}");
                                });

                                col.Item().PaddingTop(5).Text(t => {
                                    t.Line($"Order #{orderGroup.Key}").FontSize(10).Bold();
                                    t.Line($"{order?.CustomerIdentifier}").FontSize(9);
                                    if (address != null) t.Line($"{address.Street}, {address.City}").FontSize(8);
                                });

                                col.Item().AlignBottom().Text($"{orderGroup.Count()} ITEMS IN BAG").FontSize(10).ExtraBold().FontColor(Colors.Grey.Medium);
                            });

                            // B. CONTAINER LABELS
                            foreach (var lineItem in orderGroup)
                            {
                                grid.Item().Height(2, Unit.Inch).Padding(10).Border(0.5f).BorderColor(Colors.Grey.Lighten3).Column(col =>
                                {
                                    col.Item().Row(row => {
                                        row.RelativeItem().Text($"Order #{lineItem.OrderId}").FontSize(8).Bold();
                                        if (!string.IsNullOrEmpty(lineItem.LabelName))
                                            row.ConstantItem(80).Background(Colors.Emerald.Lighten4).PaddingHorizontal(5).Text($"FOR: {lineItem.LabelName}").FontSize(9).Bold();
                                    });

                                    col.Item().PaddingTop(5).Text(lineItem.RecipeName).FontSize(12).ExtraBold().Uppercase();
                                    col.Item().Text(lineItem.SizeName).FontSize(8).Italic();

                                    if (lineItem.SelectedOptions.Any())
                                        col.Item().Text($"+ {string.Join(", ", lineItem.SelectedOptions.Select(o => o.OptionName))}").FontSize(7).FontColor(Colors.Blue.Medium);

                                    // Macros (Placeholder - pull from Recipe if loaded)
                                    col.Item().AlignBottom().Row(row => {
                                        row.RelativeItem().Text("Macros: TBD").FontSize(7).FontColor(Colors.Grey.Medium);
                                    });
                                });
                            }
                        }
                    });
                });
            });

            using var stream = new MemoryStream();
            document.GeneratePdf(stream);
            return stream.ToArray();
        }
    }
}