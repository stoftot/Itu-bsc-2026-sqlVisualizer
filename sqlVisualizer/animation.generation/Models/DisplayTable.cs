using commonDataModels;
using visualizer.service.Contracts;

namespace animationGeneration.Models;

public class DisplayTable : IDisplayTable
{
    public DisplayTableRow this[int row] => Rows[row];

    public required string Name;
    
    public required List<DisplayTableRow> Rows { get; init; }
    public required List<string> ColumnNames { get; init; }
    public List<DisplayAggregation> Aggregations { get; init; } = [];
    public const string RowIndexColumnName = "RowIndex";
    
    /// <summary>
    /// The names of the tables the columns originally comes from so things like table.columnName
    /// can be handled.
    /// If a name is "()" it means it's an aggregate function and it therefore dost have an original table
    /// </summary>
    public List<string> ColumnsOriginalTableNames { get; init; } = [];
    
    public DisplayTable DeepClone()
    {
        return new DisplayTable
        {
            Name = Name,
            ColumnNames = ColumnNames.ToList(),
            ColumnsOriginalTableNames = ColumnsOriginalTableNames.ToList(),
            Rows = Rows
                .Select(e => e.DeepClone())
                .ToList()
        };
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="column"></param>
    /// <returns>The index of the column specified, -1 if the column is '*'</returns>
    /// <exception cref="ArgumentException"></exception>
    public int IndexOfColumn(string column)
    {
        var parts = column.Trim().Split(" ", 2)[0].Split('.', 2);
        var tableName = parts.Length == 2 ? parts[0] : null;
        var columnName = parts.Length == 2 ? parts[1] : parts[0];

        if (columnName.Equals("*")) return -1;
        columnName = columnName.Replace("\"", "");

        for (int i = 0; i < ColumnNames.Count; i++)
        {
            if (ColumnNames[i].Equals(columnName, StringComparison.InvariantCultureIgnoreCase) &&
                (tableName == null ||
                 ColumnsOriginalTableNames[i]
                     .Equals(tableName, StringComparison.InvariantCultureIgnoreCase)))
            {
                return i;
            }
        }

        throw new ArgumentException($"Column {column} not found in table {tableName}");
    }

    public List<int> IndexOfOriginTableColumns(string columnOrTable)
    {
        var tableName = columnOrTable.Trim().Split('.', 2)[0];
        var indexes = new List<int>();
        for (int i = 0; i < ColumnsOriginalTableNames.Count; i++)
        {
            if (ColumnsOriginalTableNames[i].Equals(tableName, StringComparison.InvariantCultureIgnoreCase))
                indexes.Add(i);
        }

        if (indexes.Count == 0)
            throw new ArgumentException($"origin table '{tableName}' not found");

        return indexes;
    }

    public IList<int> IndexOfColumns(IEnumerable<string> columns, bool ignoreColumnsNotFound = false)
    {
        var indexes = new List<int>();
        foreach (var column in columns)
        {
            try
            {
                indexes.Add(IndexOfColumn(column));
            }
            catch (Exception e)
            {
                if (!ignoreColumnsNotFound)
                    throw;
            }
        }

        return indexes;
    }

    public DisplayTable OrderBy(string column, bool ascending)
    {
        var columnIndex = IndexOfColumn(column);
        var rawValueComparer = Comparer<object?>.Create(DisplayTableTableCell.CompareRawValues);
        var orderedEntries = ascending
            ? Rows.OrderBy(e => e.Cells[columnIndex].RawValue, rawValueComparer).ToList()
            : Rows.OrderByDescending(e => e.Cells[columnIndex].RawValue, rawValueComparer).ToList();

        return new DisplayTable
        {
            Name = Name,
            ColumnNames = ColumnNames,
            ColumnsOriginalTableNames = ColumnsOriginalTableNames,
            Rows = orderedEntries
        };
    }

    public DisplayTable AppendRowIndex()
    {
        List<DisplayTableRow> entries = Rows.ToList();
        for (int i = 0; i < entries.Count; i++)
        {
            entries[i] = entries[i].AppendRowIndex(i.ToString());
        }

        List<string> names = ColumnNames.ToList();
        names.Add(RowIndexColumnName);

        List<string> originNames = ColumnsOriginalTableNames.ToList();
        originNames.Add(RowIndexColumnName);

        return new DisplayTable
        {
            Name = Name,
            ColumnNames = names,
            ColumnsOriginalTableNames = originNames,
            Rows = entries
        };
    }
    
    IReadOnlyList<IDisplayAggregation> IDisplayTable.Aggregations() => Aggregations;
    IReadOnlyList<string> IDisplayTable.ColumnNames() => ColumnNames;
    public IEnumerable<IDisplayTableRow> VisibleRows() => Rows.Where(r => r.IsVisible);
}

public class DisplayTableGenerator : IDisplayTableGenerator
{
    public IDisplayTable Generate(ISimpleTable table)
        => new DisplayTable
        {
            Name = "",
            ColumnNames = table.ColumnNames().ToList(),
            Rows = table.Rows().Select(row => new DisplayTableRow
            {
                Cells = row.Select(value => new DisplayTableTableCell
                {
                    Value = value?.ToString() ?? "NULL",
                    RawValue = value
                }).ToList()
            }).ToList()
        };
}