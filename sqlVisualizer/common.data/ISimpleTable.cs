namespace commonDataModels;

/// <summary>
/// Minimal tabular data contract shared across execution, schema inspection, and display conversion.
/// </summary>
public interface ISimpleTable
{
    /// <summary>
    /// Gets the column names in result order.
    /// </summary>
    IList<string> ColumnNames();

    /// <summary>
    /// Gets the raw rows where each inner list represents one row.
    /// </summary>
    IList<IList<object?>> Rows();
}
