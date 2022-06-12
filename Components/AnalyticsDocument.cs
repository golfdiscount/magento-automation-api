using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Data;

namespace magestack.Components
{
    public class AnalyticsDocument : IDocument
    {
        private readonly DataSet dataSet;
        public AnalyticsDocument(DataSet dataSet)
        {
            this.dataSet = dataSet;
        }

        public void Compose(IDocumentContainer container)
        {
            container.Page(page=>
            {
                page.Margin(20);
                page.Header().ShowOnce().PaddingBottom(20).Element(ComposeHeader);
                page.Content().Element(ComposeContent);
                page.Footer().AlignCenter().Text(x =>
                {
                    x.CurrentPageNumber();
                    x.Span(" / ");
                    x.TotalPages();
                });
            });
        }

        public void ComposeHeader(IContainer container)
        {
            TextStyle titleStyle = TextStyle.Default.FontSize(20).SemiBold();

            container.Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    column.Item().Column(column =>
                    {
                        column.Item().Text("Pro Golf Discount Analytics Report").Style(titleStyle);
                        column.Item().Text("Pro Golf Discount");
                        column.Item().Text("13405 SE 30th St Suite 1A");
                        column.Item().Text("Bellevue, WA 98005");
                    });

                    column.Item().PaddingTop(10).Column(column =>
                    {
                        DateTime today = DateTime.Today;
                        column.Item().Text("Report generated on: " + today.ToString("MM-dd-yyyy")).FontColor(Colors.Grey.Darken1);
                    });
                });
            });
        }

        public void ComposeContent(IContainer container)
        {
            container.Column(column =>
            {
                foreach (DataTable table in dataSet.Tables)
                {
                    column.Item().Component(new TableComponent(table));
                }
            });
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;
    }
}
