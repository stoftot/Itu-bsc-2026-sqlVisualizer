namespace visualizer.service.Contracts;

/// <summary>
/// Represents one renderable display cell.
/// </summary>
public interface IDisplayTableCell
{
    /// <summary>
    /// Gets the display value of the cell.
    /// </summary>
    public string Value();

    /// <summary>
    /// Gets the inline style currently applied to the cell.
    /// </summary>
    public string Style();
}
