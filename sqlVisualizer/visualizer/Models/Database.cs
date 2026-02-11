namespace visualizer.Models;

public class Database
{
    public required string Name { get; init; }
    public required List<Table> Tables { get; init; }
}