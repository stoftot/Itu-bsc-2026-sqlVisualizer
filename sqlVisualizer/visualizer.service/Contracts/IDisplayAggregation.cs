namespace visualizer.service.Contracts;

/// <summary>
/// Represents a renderable aggregate value shown next to a display table.
/// </summary>
public interface IDisplayAggregation
{
    /// <summary>
    /// Gets the aggregate label.
    /// </summary>
    public string Name();

    /// <summary>
    /// Gets the aggregate value as display text.
    /// </summary>
    public string Value();

    /// <summary>
    /// Gets whether the aggregate is currently highlighted.
    /// </summary>
    public bool IsHighlighted();

    /// <summary>
    /// Gets the current background color used for rendering.
    /// </summary>
    public string HexBackgroundColor();
}
