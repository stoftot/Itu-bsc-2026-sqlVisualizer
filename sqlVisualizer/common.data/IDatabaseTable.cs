namespace commonDataModels;

/// <summary>
/// Represents one database table including schema metadata and raw row data.
/// </summary>
public interface IDatabaseTable : ISimpleTable
{
    /// <summary>
    /// Gets the table name.
    /// </summary>
    public string Name();

    /// <summary>
    /// Gets the database-specific column type names aligned with <see cref="ISimpleTable.ColumnNames"/>.
    /// </summary>
    public IReadOnlyList<string> ColumnTypes();
}
