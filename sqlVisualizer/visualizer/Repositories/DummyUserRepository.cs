namespace visualizer.Repositories;

public class DummyUserRepository : IUserRepository
{
    public void SaveUserQuery(string sessionId, string query)
    { }

    public string? GetUserQuery(string sessionId)
    {
        return null;
    }
}
