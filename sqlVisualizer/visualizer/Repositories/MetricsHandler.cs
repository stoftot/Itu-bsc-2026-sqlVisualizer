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