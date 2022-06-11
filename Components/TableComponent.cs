using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Data;

namespace magestack.Components
{    
    public class TableComponent : IComponent
    {
        private readonly DataTable data;

        public TableComponent(DataTable data)
        {
            this.data = data;
        }

        public void Compose(IContainer container)
        {
            container.PaddingBottom(20).Column(column =>
            {
                column.Item().Text(data.TableName).Bold().FontSize(16);
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                    });

                    table.Header(header =>
                    {
                        foreach (DataColumn dataColumn in data.Columns)
                        {
                            header.Cell().Element(CellStyle).Text(dataColumn.ColumnName);

                        }

                        static IContainer CellStyle(IContainer container)
                        {
                            return container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);
                        }
                    });

                    foreach (DataRow row in data.Rows)
                    {
                        foreach (DataColumn column in data.Columns)
                        {
                            if (column.DataType == typeof(DateTime))
                            {
                                DateTime date = DateTime.Parse($"{row[column.ColumnName]}");
                                table.Cell().Text(date.ToString("MM-dd-yyyy"));
                            } else
                            {
                                table.Cell().Text(row[column.ColumnName]);
                            }
                        }
                    }
                });
            });
        }
    }
}
