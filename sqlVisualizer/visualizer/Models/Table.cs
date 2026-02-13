namespace visualizer.Models;

public class Table
{
    /// <summary>
    /// A table is only given a name if is fetched with a single from clause,
    /// otherwise it is considered a result table and has an empty string as name.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    public bool IsResultTable => Name == string.Empty;
    public List<string> OrginalTableNames { get; } = [];
    public required IReadOnlyList<string> ColumnNames { get; init; }
    public required IReadOnlyList<TableEntry> Entries { get; init; }
}