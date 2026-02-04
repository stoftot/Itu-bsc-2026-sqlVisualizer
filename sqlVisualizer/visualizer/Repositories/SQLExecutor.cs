using Microsoft.Data.Sqlite;
using visualizer.Models;

namespace visualizer.Repositories;

public class SQLExecutor(SqliteConnection connection)
{
    public async Task<Table> Execute(string sql)
    {
        var entries = new List<List<string>>();
        await connection.OpenAsync();
        await using var command = new SqliteCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();
        
        var header = new List<string>();
        for (var i = 0; i < reader.FieldCount; i++)
        {
            header.Add(reader.GetName(i));
        }
        
        while (await reader.ReadAsync())
        {
            var row = new List<string>();
            for (var i = 0; i < reader.FieldCount; i++)
            {
                row.Add(reader.GetValue(i).ToString() ?? "NULL");
            }
            entries.Add(row);
        }
        return new Table { Entries = entries.Prepend(header).ToList() };
    }
}