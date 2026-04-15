namespace visualizer.Repositories;

public class DummyUserRepository : IUserRepository
{
    public void SaveUserQuery(string sessionId, string databaseName, string query)
    { }

    public string? GetUserQuery(string sessionId, string databaseName)
    {
        return null;
    }

    public void SaveUserDatabaseName(string sessionId, string databaseName)
    { }

    public List<string> GetUserDatabaseNames(string sessionId)
    {
        return ["Example Database"];
    }
}
