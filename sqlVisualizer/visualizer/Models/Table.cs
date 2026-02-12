namespace visualizer.Models;

public class Table
{
    /// <summary>
    /// A table is only given a name if is fetched with a single from clause,
    /// otherwise it is considered a result table and has an empty string as name.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    public bool IsResultTable => Name == string.Empty;
    public required List<string> ColumnNames { get; init; }
    public required List<TableEntry> Entries { get; init; }
}