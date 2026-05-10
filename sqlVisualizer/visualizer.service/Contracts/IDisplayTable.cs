using commonDataModels;

namespace visualizer.service.Contracts;

/// <summary>
/// Represents a UI-facing table that can be rendered and animated.
/// </summary>
public interface IDisplayTable
{
    /// <summary>
    /// Gets aggregate values associated with the table.
    /// </summary>
    public IReadOnlyList<IDisplayAggregation> Aggregations();

    /// <summary>
    /// Gets the column names displayed in the UI.
    /// </summary>
    public IReadOnlyList<string> ColumnNames();

    /// <summary>
    /// Gets the rows currently visible to the user.
    /// </summary>
    public IEnumerable<IDisplayTableRow> VisibleRows();
}

/// <summary>
/// Converts a raw query result into a UI-facing display table.
/// </summary>
public interface IDisplayTableGenerator
{
    /// <summary>
    /// Converts a simple table into its display representation.
    /// </summary>
    public IDisplayTable Generate(ISimpleTable table);
}
