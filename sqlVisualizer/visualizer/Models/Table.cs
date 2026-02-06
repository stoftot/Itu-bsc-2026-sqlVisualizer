namespace visualizer.Models;

public class Table
{
    public required List<string> ColumnNames { get; init; }
    public required List<TableEntry> Entries { get; init; }
}