namespace visualizer.Models;

public class Table
{
    public required List<List<string>> Entries { get; init; }

    public List<string> GetColumnNames()
    {
        return Entries[0];
    }
}