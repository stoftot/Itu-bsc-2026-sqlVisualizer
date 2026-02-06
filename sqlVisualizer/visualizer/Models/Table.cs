namespace visualizer.Models;

public class Table
{
    public required List<string> ColumnNames { get; init; }
    public required List<List<string>> Entries { get; init; }
}