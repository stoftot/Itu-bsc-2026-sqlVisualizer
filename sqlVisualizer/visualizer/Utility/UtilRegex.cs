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
    // public const string Pattern = "";
    // public const string Pattern = "";
    // public const string Pattern = "";
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