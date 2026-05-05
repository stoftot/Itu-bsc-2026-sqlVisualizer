namespace visualizer.Models;

public class Query
{
    public required ActionType Type { get; init; }
    public required String SQL { get; set; }
}