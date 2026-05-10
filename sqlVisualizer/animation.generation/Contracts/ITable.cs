namespace animationGeneration.Contracts;

/// <summary>
/// Represents an execution-stage table consumed by the animation generator.
/// </summary>
public interface ITable
{
    /// <summary>
    /// Gets the table name.
    /// </summary>
    public string Name();

    /// <summary>
    /// Gets the original source table name for each column.
    /// </summary>
    public List<string> ColumnsOriginalTableNames();

    /// <summary>
    /// Gets the column names in display order.
    /// </summary>
    public List<string> ColumnNames();

    /// <summary>
    /// Gets the table data as rows of cells.
    /// </summary>
    public IReadOnlyList<IReadOnlyList<ITableCell>> Data();

    /// <summary>
    /// Gets aggregate values associated with this table.
    /// </summary>
    public IReadOnlyList<IAggregation> Aggregations();
}
