using System.Drawing;
using System.Text;
using DuckDB.NET.Data;
using visualizer.Models;

namespace visualizer.Repositories;

public class SQLExecutor(DuckDBConnection connection)
{
    private async Task<Table> Execute(string sql)
    {
        try
        {
            var entries = new List<TableEntry>();
            await connection.OpenAsync();
            await using var command = new DuckDBCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync();

            var columnNames = new List<string>();
            for (var i = 0; i < reader.FieldCount; i++)
            {
                columnNames.Add(reader.GetName(i));
            }

            while (await reader.ReadAsync())
            {
                var row = new List<TableValue>();
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    row.Add(new TableValue{Value = reader.GetValue(i).ToString() ?? "NULL"});
                }

                entries.Add(new TableEntry { Values = row });
            }

            return new Table{ColumnNames = columnNames.AsReadOnly(), Entries = entries.AsReadOnly()};
        }
        finally
        {
            await connection.CloseAsync();
        }
    }

    public async Task<Table> Execute(IEnumerable<SQLDecompositionComponent> sqlDecompositionComponents)
    {
        var components = sqlDecompositionComponents.ToList();
        var containsSelect = components.Any(c => c.Keyword == SQLKeyword.SELECT);
        var queryBuilder = new StringBuilder();
        if (!containsSelect)
        {
            queryBuilder.Append("SELECT * ");
        }

        foreach (var component in components.OrderBy(c => c.Keyword.SyntaxPrecedence()))
        {
            queryBuilder.Append(component.ToExecutableClause());
            queryBuilder.Append(' ');
        }

        return await Execute(queryBuilder.ToString());
    }

    public async Task<Table> Execute(SQLDecompositionComponent component)
    {
        var table = await Execute([component]);

        if (component.Keyword == SQLKeyword.FROM)
        {
            table.Name = component.Clause.Split(' ')[0];
        }

        return table;
    }

    public async Task<Database> GetDatabase()
    {
        var database = new Database()
            { Name = "Standard", Tables = [] };
        var tables = await Execute("SHOW TABLES");

        foreach (var tableName in tables.Entries.Select(table => table.Values[0].Value))
        {
            var table = await Execute("SHOW TABLE " + tableName);
            table.Name = tableName;
            database.Tables.Add(table);
        }

        return database;
    }
}