namespace visualizer.service.Contracts;

public interface IDisplayAggregation
{
    public string Name();
    public string Value();
    public bool IsHighlighted();
    public string HexBackgroundColor();
}