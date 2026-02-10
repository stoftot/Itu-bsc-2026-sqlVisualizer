namespace visualizer.Models;

public class Database
{
    public required String Name { get; init; }
    public required List<String> TableNames { get; init; }
    public required List<Table> Tables { get; init; }
}