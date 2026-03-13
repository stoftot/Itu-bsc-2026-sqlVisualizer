using System.Drawing;
using System.Text;
using System.Text.RegularExpressions;
using DuckDB.NET.Data;
using visualizer.Models;
using visualizer.Utility;

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
                    row.Add(new TableValue { Value = reader.GetValue(i).ToString() ?? "NULL" });
                }

                entries.Add(new TableEntry { Values = row });
            }

            return new Table { ColumnNames = columnNames, Entries = entries.AsReadOnly() };
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
        var containsGroupByAndNotOrderBy = components.Any(c =>
                                               c.Keyword == SQLKeyword.GROUP_BY) &&
                                           components.All(c =>
                                               c.Keyword != SQLKeyword.ORDER_BY);
        var containsWindowFunctionAndNotOrderBy =
            Regex.Match(
            components.FirstOrDefault(c => c.Keyword == SQLKeyword.SELECT
                , new SQLDecompositionComponent(SQLKeyword.SELECT, "")).Clause,
            UtilRegex.ContainsWindowFunctionPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline)
                .Success
            && components.All(c => c.Keyword != SQLKeyword.ORDER_BY);
            
        var queryBuilder = new StringBuilder();
        if (!containsSelect)
        {
            queryBuilder.Append("SELECT * ");
        }

        if (containsGroupByAndNotOrderBy)
        {
            var groupBy = components.First(c => c.Keyword == SQLKeyword.GROUP_BY);
            components.Add(new SQLDecompositionComponent(SQLKeyword.ORDER_BY, groupBy.Clause));
        }

        if (containsWindowFunctionAndNotOrderBy)
        {
            var selectComponent = components.First(c => c.Keyword == SQLKeyword.SELECT);
            var columnsToOrderBy = GetWindowFunctionsColumnsToGroupBy(selectComponent.Clause);
            components.Add(new SQLDecompositionComponent(SQLKeyword.ORDER_BY, columnsToOrderBy));
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
            table.Name = component.Clause.Split(' ')[0].Trim();
        }

        return table;
    }

    private string GetWindowFunctionsColumnsToGroupBy(string selectClause)
    {
        var windowFunctionMatch = Regex.Match(selectClause, 
            UtilRegex.ExtractWindowFunctionFromSelectClausePattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
        var windowFunction = windowFunctionMatch.Groups[0].Value;

        var columnsPartitionByMatch = Regex.Match(windowFunction,
            UtilRegex.ExtractColumnsFromPartitionByInWindowFunctionPattern, 
            RegexOptions.IgnoreCase | RegexOptions.Singleline);
        var columnsOrderByMatch = Regex.Match(windowFunction,
            UtilRegex.ExtractColumnsFromOrderByInWindowFunctionPattern, 
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        var columns = new StringBuilder();

        if (columnsPartitionByMatch.Success)
        {
            columns.Append(columnsPartitionByMatch.Groups[1].Value);
        }

        if (columnsOrderByMatch.Success)
        {
            columns.Append(',');
            columns.Append(columnsOrderByMatch.Groups[1].Value);
        }
        
        return columns.ToString();
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