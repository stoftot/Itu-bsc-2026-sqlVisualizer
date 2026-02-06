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
            var entries = new List<List<string>>();
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
                var row = new List<string>();
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    row.Add(reader.GetValue(i).ToString() ?? "NULL");
                }

                entries.Add(row);
            }

            return new Table { ColumnNames = columnNames, Entries = entries };
        }
        finally
        {
            await connection.CloseAsync();
        }
    }

    public async Task<Table> Execute(IEnumerable<SQLDecompositionComponent> components)
    {
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
        => await Execute([component]);
}