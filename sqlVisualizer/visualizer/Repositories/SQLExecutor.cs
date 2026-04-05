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
        
        var containsJoinAndNotOrderBy = components.Any(c =>
                                            c.Keyword.IsJoin()) &&
                                        components.All(c =>
                                            c.Keyword != SQLKeyword.ORDER_BY);
            
        var queryBuilder = new StringBuilder();

        // WITH must come before SELECT *, so output it first.
        var withComponent = components.FirstOrDefault(c => c.Keyword == SQLKeyword.WITH);
        if (withComponent != null)
        {
            queryBuilder.Append(withComponent.ToExecutableClause());
            queryBuilder.Append(' ');
        }

        if (!containsSelect)
        {
            queryBuilder.Append("SELECT * ");
        }

        if (containsWindowFunctionAndNotOrderBy)
        {
            var selectComponent = components.First(c => c.Keyword == SQLKeyword.SELECT);
            var columnsToOrderBy = GetWindowFunctionsColumnsToGroupBy(selectComponent.Clause);
            if (!string.IsNullOrWhiteSpace(columnsToOrderBy))
                components.Add(new SQLDecompositionComponent(SQLKeyword.ORDER_BY, columnsToOrderBy));
        }
        else if (containsGroupByAndNotOrderBy)
        {
            var groupBy = components.First(c => c.Keyword == SQLKeyword.GROUP_BY);
            components.Add(new SQLDecompositionComponent(SQLKeyword.ORDER_BY, groupBy.Clause));
        }
        else if (containsJoinAndNotOrderBy)
        {
            var lastJoin = components.Last(c => c.Keyword.IsJoin());
            var orderBy = "";
            
            switch (lastJoin.Keyword)
            {
                case SQLKeyword.JOIN:
                case SQLKeyword.INNER_JOIN:
                case SQLKeyword.LEFT_JOIN:
                case SQLKeyword.LEFT_OUTER_JOIN:
                {
                    //extract from table
                    var fromComponent = components.First(c => c.Keyword == SQLKeyword.FROM);
                    orderBy = fromComponent.Clause.Trim().Split(' ').Last() + ".rowid";
                }
                    break;
                case SQLKeyword.RIGHT_JOIN:
                case SQLKeyword.RIGHT_OUTER_JOIN:
                {
                    //extract joining table
                    orderBy = UtilRegex.Match(lastJoin.Clause, ".*(?=ON)")
                        .Value.Trim().Split(' ').Last() + ".rowid";
                }
                    break;
                case SQLKeyword.FULL_JOIN:
                case SQLKeyword.FULL_OUTER_JOIN:
                {
                    var fromComponent = components.First(c => c.Keyword == SQLKeyword.FROM);
                    orderBy = fromComponent.Clause.Trim().Split(' ').Last() + ".rowid";

                    orderBy += ","+UtilRegex.Match(lastJoin.Clause, ".*(?=ON)")
                        .Value.Trim().Split(' ').Last() + ".rowid";
                }
                    break;
            }

            components.Add(new SQLDecompositionComponent(SQLKeyword.ORDER_BY, orderBy));
        }

        foreach (var component in components.Where(c => c.Keyword != SQLKeyword.WITH).OrderBy(c => c.Keyword.SyntaxPrecedence()))
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
            table.Name = component.Clause.Split(' ')[^1];
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

        var columns = new List<string>();

        if (columnsPartitionByMatch.Success)
        {
            foreach (var column in columnsPartitionByMatch.Groups[1].Value.Split(','))
            {
                var c = column.Trim();
                if (columns.Contains(c)) continue;
                columns.Add(c);
            }
        }

        if (columnsOrderByMatch.Success)
        {
            foreach (var column in columnsOrderByMatch.Groups[0].Value.Split(','))
            {
                var c = column.Trim();
                if (columns.Contains(c)) continue;
                // columns.Remove(c);
                columns.Add(c);
            }
        }

        return string.Join(",", columns);
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
