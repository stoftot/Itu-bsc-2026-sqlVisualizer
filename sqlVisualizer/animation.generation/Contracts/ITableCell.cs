namespace animationGeneration.Contracts;

/// <summary>
/// Represents one cell in an execution-stage table.
/// </summary>
public interface ITableCell
{
    /// <summary>
    /// Gets the string representation of the cell.
    /// </summary>
    public string Value();

    /// <summary>
    /// Gets the original typed value from the database engine.
    /// </summary>
    public object? RawValue();
}
