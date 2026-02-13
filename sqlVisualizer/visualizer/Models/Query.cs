namespace visualizer.Models;

public class Query
{
    public required String Type { get; init; }
    public required String SQL { get; set; }
}