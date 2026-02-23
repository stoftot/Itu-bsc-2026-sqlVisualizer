namespace visualizer.Models;

public class Table
{
    /// <summary>
    /// A table is only given a name if is fetched with a single from clause,
    /// otherwise it is considered a result table and has an empty string as name.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    public bool IsResultTable => Name == string.Empty;
    /// <summary>
    /// The names of the tables the columns originally comes from so things like table.columnName
    /// can be handled.
    /// If a name is "()" it means it's an aggregate function and it therefore dost have an original table
    /// </summary>
    public List<string> ColumnsOriginalTableNames { get; } = [];
    public required IReadOnlyList<string> ColumnNames { get; init; }
    public required IReadOnlyList<TableEntry> Entries { get; init; }

    public Table DeepClone()
    {
        return new Table
        {
            Name = Name,
            ColumnNames = ColumnNames.ToList(),
            Entries = Entries
                .Select(e => e.DeepClone()) // you must implement this
                .ToList()
        };
    }

    public int IndexOfColumn(string column)
    {
        var parts = column.Trim().Split('.', 2);
        var tableName = parts.Length == 2 ? parts[0] : null;
        var columnName = parts.Length == 2 ? parts[1] : parts[0];

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
}