using animationGeneration.Contracts;

namespace tableGeneration.Models;

public class Table : ITable
{
    /// <summary>
    /// A table is only given a name if is fetched with a single from clause,
    /// otherwise it is considered a result table and has an empty string as name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The names of the tables the columns originally comes from so things like table.columnName
    /// can be handled.
    /// If a name is "()" it means it's an aggregate function and it therefore dost have an original table
    /// </summary>
    public List<string> ColumnsOriginalTableNames { get; private init; } = [];
    
    public required List<string> ColumnNames { get; init; }
    public required List<TableEntry> Entries { get; init; }

    public List<Aggregation> Aggregations { get; set; } = [];
    public Table DeepClone()
    {
        return new Table
        {
            Name = Name,
            ColumnNames = ColumnNames.ToList(),
            ColumnsOriginalTableNames = ColumnsOriginalTableNames.ToList(),
            Entries = Entries
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

    string ITable.Name() => Name;
    List<string> ITable.ColumnsOriginalTableNames() => ColumnsOriginalTableNames;
    List<string> ITable.ColumnNames() => ColumnNames;
    public IReadOnlyList<IReadOnlyList<ITableCell>> Data() => Entries.Select(e => e.Values).ToList();
    IReadOnlyList<IAggregation> ITable.Aggregations() => Aggregations;
}

public class Aggregation : IAggregation
{
    public required string Name { get; init; }
    public required string Value { get; init; }
    
    string IAggregation.Name() => Name;
    string IAggregation.Value() => Value;
}
