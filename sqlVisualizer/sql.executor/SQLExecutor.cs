using System.Data.Common;
using commonDataModels;
using commonDataModels.Models;
using DuckDB.NET.Data;
using sql.executor.Models;

namespace sql.executor;

public class SQLExecutor(ICurrentDatabaseContext databaseContext) : ISQLExecutor
{
    public Task<ISimpleTable> Execute(string sqlQuery)
    {
        return ExecutePrivate(sqlQuery);
    }

    public async Task<IDatabase> GetDatabase(string? connectionString = null)
    {
        var tables = await ExecutePrivate("SHOW TABLES", connectionString);
        var databaseTables = new List<Table>();
        
        foreach (var tableName in tables.Rows().Select(r => r[0]?.ToString()))
        {
            if (tableName == null)
                throw new ArgumentException("unable to fetch table name when trying to read tables in database");
            
            var st = await ExecutePrivate("SELECT * FROM " + '"' + tableName + '"', connectionString);
            
            var columnTypes = await ExecutePrivate($"""
                                            SELECT data_type
                                            FROM information_schema.columns
                                            WHERE table_name = '{tableName}'
                                            ORDER BY ordinal_position
                                            """, connectionString);
            databaseTables.Add(new Table(st.ColumnNames(), st.Rows(), 
                tableName,
                columnTypes.Rows().Select(r => r[0]!.ToString()).ToList()!
                ));
        }

        return new Database("standard", databaseTables);
    }
    
    private async Task<ISimpleTable> ExecutePrivate(string sql, string? connectionString = null)
    {
        var resolvedConnectionString = connectionString ?? databaseContext.ActiveConnectionString;
        await using var temporaryConnection = new DuckDBConnection(resolvedConnectionString);
        var rows = new List<IList<object?>>();
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
            var row = new List<object?>();
            for (var i = 0; i < reader.FieldCount; i++)
            {
                var rawValue = reader.IsDBNull(i) ? null : reader.GetValue(i);
                row.Add(rawValue);
            }

            rows.Add(row);
        }

        return new Table(columnNames, rows);
    }
}
