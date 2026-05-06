using System.Text.Json;
using commonDataModels;
using DuckDB.NET.Data;
using visualizer.service.Contracts;
using visualizer.service.Exceptions;

namespace inputParsing;

internal class SQLInputValidator(ISQLExecutor sqlExecutor) : ISQLInputValidator
{
    // json_serialize_sql() is a pure parse function — no tables needed.
    private const string InMemoryConnectionString = "DataSource=:memory:";
    public void Validate(string sql)
    {
        ValidateWithDuckDb(sql);

        //validate by running against the database.
        try
        { 
            sqlExecutor.Execute(sql).Wait();
        }
        catch (Exception e)
        {
            throw new SQLParseException($"SQL parse error: {e.Message}");
        }
    }
    
    /// <summary>
    /// This checks if there is non-select query present, or if there are syntax errors
    /// </summary>
    /// <param name="sql"></param>
    /// <exception cref="SQLParseException"></exception>
    private static void ValidateWithDuckDb(string sql)
    {
        // Escape single quotes so the SQL can be embedded as a literal argument.
        var escaped = sql.Replace("'", "''");

        using var connection = new DuckDBConnection(InMemoryConnectionString);
        connection.Open();
        using var command = new DuckDBCommand($"SELECT json_serialize_sql('{escaped}')", connection);
        var raw = command.ExecuteScalar()?.ToString()
                  ?? throw new SQLParseException("DuckDB returned null from json_serialize_sql.");

        using var doc = JsonDocument.Parse(raw);

        if (doc.RootElement.TryGetProperty("error", out var errorFlag) && errorFlag.GetBoolean())
        {
            var message = doc.RootElement.TryGetProperty("error_message", out var msg)
                ? msg.GetString()
                : "Unknown SQL parse error.";
            throw new SQLParseException($"SQL parse error: {message}");
        }
    }
}
