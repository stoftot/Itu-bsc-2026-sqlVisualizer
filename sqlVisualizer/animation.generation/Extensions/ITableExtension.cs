using visualizer.Models;
using visualizer.Repositories.Contracts;

namespace visualizer.Exstensions;

public static class ITableExtension
{
    public static List<DisplayTable> ToDisplay(this IReadOnlyList<ITable> tables)
        => tables.Select(t => new DisplayTable
        {
            Name = t.Name(),
            ColumnNames = t.ColumnNames(),
            ColumnsOriginalTableNames = t.ColumnsOriginalTableNames(),
            Rows = t.Data().Select(row => new DisplayTableRow
            {
                Cells = row.Select(cell => new DisplayTableTableCell
                {
                    Value = cell.Value(),
                    RawValue = cell.RawValue()
                }).ToList()
            }).ToList(),
            Aggregations = t.Aggregations().Select(a => new DisplayAggregation
            {
                 Name = a.Name(),
                 Value = a.Value()
            }).ToList()
        }).ToList();
}