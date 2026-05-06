using DuckDB.NET.Data;

namespace visualizer.service.Repositories;

public interface IUserRepository
{
    void SaveUserQuery(string sessionId, string databaseName, string query);
    string? GetUserQuery(string sessionId, string databaseName);
    void SaveUserDatabaseName(string sessionId, string databaseName);
    List<string> GetUserDatabaseNames(string sessionId);
}

internal class UserRepository(string connectionString) : IUserRepository
{
    
    public void SaveUserQuery(string sessionId, string databaseName, string query)
    {
        using var connection = new DuckDBConnection(connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        
        command.CommandText = """
                              INSERT OR REPLACE INTO user_queries_by_database (session_id, database_name, query)
                              VALUES ($sessionId, $databaseName, $query)
                              """;
        command.Parameters.Add(new DuckDBParameter("sessionId", sessionId));
        command.Parameters.Add(new DuckDBParameter("databaseName", databaseName));
        command.Parameters.Add(new DuckDBParameter("query", query));
        command.ExecuteNonQuery();
    }
    
    public string? GetUserQuery(string sessionId, string databaseName)
    {
        using var connection = new DuckDBConnection(connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        
        command.CommandText = """
                              SELECT query
                              FROM user_queries_by_database
                              WHERE session_id = $sessionId AND database_name = $databaseName
                              """;
        command.Parameters.Add(new DuckDBParameter("sessionId", sessionId));
        command.Parameters.Add(new DuckDBParameter("databaseName", databaseName));
        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            return reader.GetString(0);
        }
        return null;
    }
    
    public void SaveUserDatabaseName(string sessionId, string databaseName)
    {
        using var connection = new DuckDBConnection(connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        
        command.CommandText = "INSERT OR REPLACE INTO user_databases (session_id, database_path) VALUES ($sessionId, $databaseName)";
        command.Parameters.Add(new DuckDBParameter("sessionId", sessionId));
        command.Parameters.Add(new DuckDBParameter("databaseName", databaseName));
        command.ExecuteNonQuery();
    }
    
    public List<string> GetUserDatabaseNames(string sessionId)
    {
        using var connection = new DuckDBConnection(connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        
        command.CommandText = "SELECT database_path FROM user_databases WHERE session_id = $sessionId";
        command.Parameters.Add(new DuckDBParameter("sessionId", sessionId));
        using var reader = command.ExecuteReader();
        
        var result = new List<string>();
        while (reader.Read())
        {
            result.Add(reader.GetString(0));
        }
        return result;
    }
}
