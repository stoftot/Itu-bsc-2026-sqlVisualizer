using System.Collections.Concurrent;
using visualizer.Models;
using DuckDB.NET.Data;

namespace visualizer.Repositories;
public class MetricsHandler
{
    private readonly string _connectionString;
    public MetricsHandler(string connectionString)
    {
        _connectionString = connectionString;
    }

    public void IncrementAction(string sessionId, ActionType actionType)
    {
        using var connection = new DuckDBConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();

        command.CommandText = @"
            INSERT INTO button_action_counts (session_id, action_name, action_count)
            VALUES ($sessionId, $actionName, 1)
            ON CONFLICT (session_id, action_name)
            DO UPDATE SET action_count = button_action_counts.action_count + 1;
        ";

        command.Parameters.Add(new DuckDBParameter("sessionId", sessionId));
        command.Parameters.Add(new DuckDBParameter("actionName", actionType.ToString()));

        command.ExecuteNonQuery();
    }
    
    public void RecordQuery(string sessionId, string query)
    {
        using var connection = new DuckDBConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();

        command.CommandText = @"
        INSERT INTO queries_written (session_id, query_string, event_ts)
        VALUES ($sessionId, $queryString, $eventTs);
    ";

        command.Parameters.Add(new DuckDBParameter("sessionId", sessionId));
        command.Parameters.Add(new DuckDBParameter("queryString", query));
        command.Parameters.Add(new DuckDBParameter("eventTs", DateTime.Now));

        command.ExecuteNonQuery();
    }

    public void PrintQueries(string sessionId)
    {
        using var connection = new DuckDBConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();

        command.CommandText = @"
        SELECT id, query_string, event_ts
        FROM queries_written
        WHERE session_id = $sessionId
        ORDER BY event_ts;
         ";

        command.Parameters.Add(new DuckDBParameter("sessionId", sessionId));

        using var reader = command.ExecuteReader();

        Console.WriteLine($"Queries for session: {sessionId}");

        while (reader.Read())
        {
            var id = reader.GetInt64(0);
            var query = reader.GetString(1);
            var timestamp = reader.GetDateTime(2);

            Console.WriteLine($"[{id}] {timestamp}: {query}");
        }
    }

    public void PrintActions(string sessionId)
    {
        using var connection = new DuckDBConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
        SELECT action_name, action_count
        FROM button_action_counts
        WHERE session_id = $sessionId;
        ";

        command.Parameters.Add(new DuckDBParameter("sessionId", sessionId));

        using var reader = command.ExecuteReader();

        Console.WriteLine($"Session: {sessionId}");

        while (reader.Read())
        {
            var actionName = reader.GetString(0);
            var count = reader.GetInt64(1);

            Console.WriteLine($"{actionName}: {count}");
        }
    }
}