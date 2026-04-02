namespace visualizer.Repositories;

public interface ICurrentDatabaseContext
{
    string ActiveConnectionString { get; set; }
}

public class CurrentDatabaseContext : ICurrentDatabaseContext
{
    public string ActiveConnectionString { get; set; } = "Data Source=data/database.db";
}

