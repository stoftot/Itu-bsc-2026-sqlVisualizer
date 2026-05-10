namespace visualizer.service.Contracts;

/// <summary>
/// Represents one renderable display row.
/// </summary>
public interface IDisplayTableRow
{
    /// <summary>
    /// Gets whether the row is currently highlighted.
    /// </summary>
    public bool IsHighlighted();

    /// <summary>
    /// Gets the inline style used when the row is highlighted.
    /// </summary>
    public string HighlightedStyle();

    /// <summary>
    /// Gets the cells contained in the row.
    /// </summary>
    public IReadOnlyList<IDisplayTableCell> Cells();
}
