namespace visualizer.Repositories;
using DuckDB.NET.Data;

public class UserRepository(string connectionString)
{
    // Create a repository for a duckdb database, that uses session id as the user id, and stores the last query for each user in a table called "user_queries"
    
    public void SaveUserQuery(string sessionId, string query)
    {
        using var connection = new DuckDBConnection(connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        
        command.CommandText = "INSERT OR REPLACE INTO user_queries (session_id, query) VALUES ($sessionId, $query)";
        command.Parameters.Add(new DuckDBParameter("sessionId", sessionId));
        command.Parameters.Add(new DuckDBParameter("query", query));
        command.ExecuteNonQuery();
    }
    
    public string? GetUserQuery(string sessionId)
    {
        using var connection = new DuckDBConnection(connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        
        command.CommandText = "SELECT query FROM user_queries WHERE session_id = $sessionId";
        command.Parameters.Add(new DuckDBParameter("sessionId", sessionId));
        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            return reader.GetString(0);
        }
        return null;
    }
}