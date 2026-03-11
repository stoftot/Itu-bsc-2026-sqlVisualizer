namespace visualizer.Utility;

public static class UtilRegex
{
    //match from
    public static readonly string FromPattern = @"(?:FROM)\s+([^\s]+?)\s(?:as\s+|\s*)([^\s]+?)\s+(?:JOIN|INNER JOIN|LEFT JOIN|RIGHT JOIN|FULL JOIN|CROSS JOIN|NATURAL JOIN|WHERE|GROUP BY|HAVING|ORDER BY|LIMIT|OFFSET|WINDOW|UNION|INTERSECT|EXCEPT)\s";
}