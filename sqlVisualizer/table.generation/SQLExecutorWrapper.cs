using System.Text;
using System.Text.RegularExpressions;
using commonDataModels;
using commonDataModels.Models;
using tableGeneration.Models;

namespace tableGeneration;

public class SQLExecutorWrapper(ISQLExecutor executor)
{
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

        foreach (var component in components.Where(c => c.Keyword != SQLKeyword.WITH)
                     .OrderBy(c => c.Keyword.SyntaxPrecedence()))
        {
            queryBuilder.Append(component.ToExecutableClause());
            queryBuilder.Append(' ');
        }


        var simpleTable = await executor.Execute(queryBuilder.ToString());

        return new Table
        {
            ColumnNames = simpleTable.ColumnNames().ToList(),
            Entries = simpleTable.Rows().Select(row =>
                new TableEntry
                {
                    Values = row.Select(rawValue =>
                        new TableValue
                        {
                            Value = rawValue?.ToString() ?? "NULL",
                            RawValue = rawValue
                        }).ToList()
                }).ToList()
        };
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
}