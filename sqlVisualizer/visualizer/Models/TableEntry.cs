namespace visualizer.Models;

public class TableEntry
{
    public bool IsHighlighted { get; set; } = false;
    public required List<string> Values { get; set; }
}