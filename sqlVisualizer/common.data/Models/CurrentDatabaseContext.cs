namespace commonDataModels.Models;

/// <summary>
/// Stores the active connection string for the current scoped application context.
/// </summary>
public interface ICurrentDatabaseContext
{
    /// <summary>
    /// Gets or sets the connection string used by query execution.
    /// </summary>
    string ActiveConnectionString { get; set; }
}

internal class CurrentDatabaseContext : ICurrentDatabaseContext
{
    public string ActiveConnectionString { get; set; } = "Data Source=data/database.db";
}
