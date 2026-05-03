using visualizer.Utility;

namespace visualizer.Models;

public class DisplayAggregation : IDisplayAggregation
{
    public required string Name { get; init; }
    public required string Value { get; init; }
    
    private bool isHighlighted = false;
    public void ToggleHighlight() => isHighlighted = !isHighlighted;

    public void ResetStyleAndVisual() => isHighlighted = false;
    
    string IDisplayAggregation.Name() => Name;

    string IDisplayAggregation.Value() => Value;

    public bool IsHighlighted() => isHighlighted;

    public string HexBackgroundColor()
    {
        throw new NotImplementedException();
    }
}