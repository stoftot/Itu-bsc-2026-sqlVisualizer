namespace commonDataModels;

/// <summary>
/// Represents a browsable database exposed to the UI schema view.
/// </summary>
public interface IDatabase
{
    /// <summary>
    /// Gets the logical database name.
    /// </summary>
    public string Name();

    /// <summary>
    /// Gets the tables available in the database.
    /// </summary>
    public IReadOnlyList<IDatabaseTable> Tables();
}
