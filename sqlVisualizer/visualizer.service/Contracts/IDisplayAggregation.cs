namespace visualizer.Models;

public interface IDisplayAggregation
{
    public string Name();
    public string Value();
    public bool IsHighlighted();
    public string HexBackgroundColor();
}