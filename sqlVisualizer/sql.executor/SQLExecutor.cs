using System.Data.Common;
using System.Text;
using System.Text.RegularExpressions;
using DuckDB.NET.Data;
using visualizer.Models;
using visualizer.Utility;

namespace visualizer.Repositories;

public class SQLExecutor
{
    private readonly ICurrentDatabaseContext _databaseContext;

    public SQLExecutor(ICurrentDatabaseContext databaseContext)
    {
        _databaseContext = databaseContext;
    }

    // Backward-compatible constructor used by existing tests.
    public SQLExecutor(DuckDBConnection connection)
    {
        _databaseContext = new CurrentDatabaseContext
        {
            ActiveConnectionString = connection.ConnectionString
        };
    }

    public async Task<Table> Execute(string sql, string? connectionString = null)
    {
        var resolvedConnectionString = connectionString ?? _databaseContext.ActiveConnectionString;
        await using var temporaryConnection = new DuckDBConnection(resolvedConnectionString);
        var entries = new List<TableEntry>();
        await temporaryConnection.OpenAsync();
        await using var command = new DuckDBCommand(sql, temporaryConnection);
        await using var reader = await command.ExecuteReaderAsync();

        var schema = reader.GetColumnSchema();
        var columnNames = new List<string>();

        foreach (var col in schema)
        {
            var name = col.ColumnName;
            if (name.Equals("count_star()", StringComparison.InvariantCultureIgnoreCase))
            {
                name = "count()";
            }

            columnNames.Add(name);
        }

        while (await reader.ReadAsync())
        {
            var row = new List<TableValue>();
            for (var i = 0; i < reader.FieldCount; i++)
            {
                var rawValue = reader.IsDBNull(i) ? null : reader.GetValue(i);
                row.Add(new TableValue
                {
                    RawValue = rawValue,
                    Value = rawValue?.ToString() ?? "NULL"
                });
            }

            entries.Add(new TableEntry { Values = row });
        }

        return new Table {ColumnNames = columnNames, Entries = entries };
    }
    
    public async Task<Database> GetDatabase(string? connectionString = null)
    {
        var database = new Database()
            { Name = "Standard", Tables = [] };
        var tables = await Execute("SHOW TABLES", connectionString);

        foreach (var tableName in tables.Entries.Select(table => table.Values[0].Value))
        {
            var table = await Execute("SELECT * FROM " + '"' + tableName + '"', connectionString);
            table.Name = tableName;
            
            var columnTypes = await Execute($"""
                                            SELECT data_type
                                            FROM information_schema.columns
                                            WHERE table_name = '{tableName}'
                                            ORDER BY ordinal_position
                                            """, connectionString);
            table.ColumnTypes = columnTypes.Entries.Select(e => e.Values[0].Value).ToList();
            database.Tables.Add(table);
        }

        return database;
    }
}
