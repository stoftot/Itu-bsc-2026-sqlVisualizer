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
    public const string ExtractColumnsFromOrderByInWindowFunctionPattern = @"ORDER BY (.+?)\b\s+(?=[^,\s])";
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
}