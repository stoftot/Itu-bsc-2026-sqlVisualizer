using System.Text.RegularExpressions;

namespace visualizer.Utility;

public static class UtilRegex
{
    //match from
    public const string AliasForFromTablePattern = @"(?:FROM)\s+([^\s]+?)\s(?:as\s+|\s*)([^\s]+?)\s+(?:JOIN|INNER JOIN|LEFT JOIN|RIGHT JOIN|FULL JOIN|CROSS JOIN|NATURAL JOIN|WHERE|GROUP BY|HAVING|ORDER BY|LIMIT|OFFSET|WINDOW|UNION|INTERSECT|EXCEPT)\s";
    public const string AliasForJoinsPattern = @"(?:JOIN)\s+([^\s]+?)\s(?:as\s+|\s*)([^\s]+?)\s+(?:ON)\s";
    public const string SelectToFromInclusivePattern = @"SELECT\s+.*?\s+FROM";
    public const string SelectClausePattern = @"SELECT\s+(.*?)\s+FROM";
    public const string ExtractAliasForSelectClausePattern = @"\b(?!DISTINCT)\b([^,]+?)(?:\s)(?:as\s+|\s*)(.+?)(?:,|$)";
    public const string ContainsWindowFunctionPattern = @"(\s|\))\s*over\s*(\s|\()";
    public const string ExtractWindowFunctionFromSelectClausePattern = @"\s*[^,]+?\bover\s*[^)]+\)[^,]+";
    public const string ExtractColumnsFromPartitionByInWindowFunctionPattern = @"PARTITION BY (.+?)\b\s+(?=[^,\s])";
    public const string ExtractColumnsFromOrderByInWindowFunctionPattern = @"(?<=ORDER BY)(?:(?:.+?)\b\s+(?:desc|asc)?)+?(?=[^,\s])";
    public const string NamedWindowFunctionPattern = @"(?<function>\w+)\((?<argument>\w*)(?:,\s*(?<extra>[^)]*))?\).*OVER\s+\(\s*(?:PARTITION BY (?<partitions>.+?)\b\s+(?=[^,\s]))?(?:ORDER BY (?<orders>.+?)\b\s+(?=[^,\s]))?";
    // public const string Pattern = "";
    // public const string Pattern = "";
    // public const string Pattern = "";
    // public const string Pattern = "";
    // public const string Pattern = "";
    // public const string Pattern = "";
    // public const string Pattern = "";
    // public const string Pattern = "";
    // public const string Pattern = "";
    // public const string Pattern = "";
    // public const string Pattern = "";
    // public const string Pattern = "";
    // public const string Pattern = "";
    // public const string Pattern = "";
    // public const string Pattern = "";
    // public const string Pattern = "";
    // public const string Pattern = "";
    // public const string Pattern = "";
    
    /// <summary>
    /// Splits a SELECT clause by top-level commas (ignores commas inside parentheses).
    /// </summary>
    public static List<string> SplitSelectColumns(string clause)
    {
        var columns = new List<string>();
        int depth = 0;
        int start = 0;
        for (int i = 0; i < clause.Length; i++)
        {
            if (clause[i] == '(') depth++;
            else if (clause[i] == ')') depth--;
            else if (clause[i] == ',' && depth == 0)
            {
                columns.Add(clause[start..i].Trim());
                start = i + 1;
            }
        }
        columns.Add(clause[start..].Trim());
        return columns;
    }

    public static Match Match(string input, string pattern)
    {
        return Regex.Match(input, pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
    }
    // public const string Pattern = "";
    
    public static IEnumerable<string> ExtractReferencedColumns(string expression)
    {
        var potenTialColumns = expression.Split(' ');
        var columns = new List<string>();
        const string pattern = @""".+""|\b.+";
        foreach (var pc in potenTialColumns)
        {
            var match = Regex.Match(pc, pattern);
            if (match.Success) columns.Add(match.Groups[0].Value.Trim());
        }
        return columns.Where(value => !int.TryParse(value, out _));
    }
}