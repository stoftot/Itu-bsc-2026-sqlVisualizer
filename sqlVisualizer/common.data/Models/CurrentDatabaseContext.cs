namespace commonDataModels.Models;

public interface ICurrentDatabaseContext
{
    string ActiveConnectionString { get; set; }
}

internal class CurrentDatabaseContext : ICurrentDatabaseContext
{
    public string ActiveConnectionString { get; set; } = "Data Source=data/database.db";
}

