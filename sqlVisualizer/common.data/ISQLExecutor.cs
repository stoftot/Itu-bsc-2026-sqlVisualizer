namespace commonDataModels;

/// <summary>
/// Executes SQL against the currently selected database and exposes database inspection helpers.
/// </summary>
public interface ISQLExecutor
{
    /// <summary>
    /// Executes a SQL query and returns the raw result table.
    /// </summary>
    public Task<ISimpleTable> Execute(string sqlQuery);

    /// <summary>
    /// Reads the available schema and table contents for a database.
    /// </summary>
    /// <param name="connectionString">
    /// Optional override for reading a different database than the currently active one.
    /// </param>
    public Task<IDatabase> GetDatabase(string? connectionString = null);
}
