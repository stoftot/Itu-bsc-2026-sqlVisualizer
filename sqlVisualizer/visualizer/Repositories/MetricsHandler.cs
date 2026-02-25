using System.Collections.Concurrent;
using visualizer.Models;
using DuckDB.NET.Data;

namespace visualizer.Repositories;

public interface IMetricsHandler
{
    void IncrementAction(string sessionId, ActionType actionType);
    void RecordQuery(string sessionId, string query);
    void PrintQueries(string sessionId);
    void PrintActions(string sessionId);
    void EnterStep(string sessionId, SQLKeyword step);
    void StartAnimation(string sessionId);
    void StopAnimation(string sessionId);
    void PrintSessionTimings(string sessionId);
}

public class MetricsHandler : IMetricsHandler
{
    private readonly string _connectionString;
    private readonly ConcurrentDictionary<string, SessionTimingState> _sessions = new();
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
    
    public void EnterStep(string sessionId, SQLKeyword step)
    {
        var state = _sessions.GetOrAdd(sessionId, _ => new SessionTimingState());

        lock (state.LockObj)
        {
            var now = DateTime.UtcNow;
            
            if (state.CurrentStep is not null && !string.Equals(state.CurrentStep, step.ToString(), StringComparison.Ordinal))
            {
                SaveTimings(sessionId, state, now);
            }

            if (state.CurrentStep is null)
            {
                state.CurrentStep = step.ToString();
                state.StepStartUtc = now;
                state.AnimationAccumulatedMs = 0;
                state.AnimationRunning = false;
                state.AnimationStartUtc = null;
            }
        }
    }
    
    public void StartAnimation(string sessionId)
    {
        if (!_sessions.TryGetValue(sessionId, out var state))
            return;

        lock (state.LockObj)
        {
            if (state.CurrentStep is null) return;
            if (state.AnimationRunning) return;

            state.AnimationRunning = true;
            state.AnimationStartUtc = DateTime.UtcNow;
        }
    }
    
    public void StopAnimation(string sessionId)
    {
        if (!_sessions.TryGetValue(sessionId, out var state))
            return;

        lock (state.LockObj)
        {
            if (!state.AnimationRunning) return;

            var now = DateTime.UtcNow;
            if (state.AnimationStartUtc.HasValue)
            {
                state.AnimationAccumulatedMs += (long)(now - state.AnimationStartUtc.Value).TotalMilliseconds;
            }

            state.AnimationRunning = false;
            state.AnimationStartUtc = null;
        }
    }

    // Writes one row for the current step segment and advances to "no active step"
    private void SaveTimings(string sessionId, SessionTimingState state, DateTime nowUtc)
    {
        if (state.AnimationRunning && state.AnimationStartUtc.HasValue)
        {
            state.AnimationAccumulatedMs += (long)(nowUtc - state.AnimationStartUtc.Value).TotalMilliseconds;
            state.AnimationRunning = false;
            state.AnimationStartUtc = null;
        }

        if (state.CurrentStep is null || state.StepStartUtc is null)
            return;

        var timeSpentMs = (long)(nowUtc - state.StepStartUtc.Value).TotalMilliseconds;
        var animationMs = state.AnimationAccumulatedMs;

        InsertTimeSpentRow(sessionId, state.CurrentStep, timeSpentMs, animationMs, nowUtc);

        state.CurrentStep = null;
        state.StepStartUtc = null;
        state.AnimationAccumulatedMs = 0;
    }

    private void InsertTimeSpentRow(string sessionId, string step, long timeSpentMs, long animationMs, DateTime nowUtc)
    {
        using var connection = new DuckDBConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO time_spent (session_id, step, time_spent_ms, animation_ms, event_ts)
            VALUES ($sessionId, $step, $timeSpentMs, $animationMs, $eventTs);
        ";

        command.Parameters.Add(new DuckDBParameter("sessionId", sessionId));
        command.Parameters.Add(new DuckDBParameter("step", step));
        command.Parameters.Add(new DuckDBParameter("timeSpentMs", timeSpentMs));
        command.Parameters.Add(new DuckDBParameter("animationMs", animationMs));
        command.Parameters.Add(new DuckDBParameter("eventTs", nowUtc));

        command.ExecuteNonQuery();
    }
    
    public void PrintSessionTimings(string sessionId)
    {
        using var connection = new DuckDBConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
        SELECT
            step,
            SUM(time_spent_ms) AS total_time,
            SUM(animation_ms)  AS total_animation
        FROM time_spent
        WHERE session_id = $sessionId
        GROUP BY step
        ORDER BY step;
    ";

        command.Parameters.Add(new DuckDBParameter("sessionId", sessionId));

        using var reader = command.ExecuteReader();

        Console.WriteLine($"=== Timings for Session: {sessionId} ===");

        while (reader.Read())
        {
            var step = reader.GetString(0);
            var totalTime = reader.GetInt64(1);
            var animationTime = reader.GetInt64(2);

            Console.WriteLine($"Step {step}");
            Console.WriteLine($"  Viewing Time  : {totalTime} ms");
            Console.WriteLine($"  Animation Time: {animationTime} ms");
        }
    }

    private sealed class SessionTimingState
    {
        public object LockObj { get; } = new();

        public string? CurrentStep { get; set; }
        public DateTime? StepStartUtc { get; set; }

        public bool AnimationRunning { get; set; }
        public DateTime? AnimationStartUtc { get; set; }
        public long AnimationAccumulatedMs { get; set; }
    }
    
    
}