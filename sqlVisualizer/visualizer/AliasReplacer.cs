using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using visualizer.Models;

namespace Visualizer;

public class AliasReplacer
{
    private Dictionary<string, string> _aliasToTableMap;
    private Dictionary<string, string> _selectAliasMap;

    public AliasReplacer()
    {
        _aliasToTableMap = new Dictionary<string, string>();
        _selectAliasMap = new Dictionary<string, string>();
    }
    
    public string ReplaceAliases(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
            return sql;

        // Extract alias mappings from FROM and JOIN clauses
        ExtractAliases(sql);

        string result = sql;

        // Remove alias definitions
        foreach (var alias in _aliasToTableMap)
        {
            result = Regex.Replace(result,$@"\b{alias.Key}\s+", string.Empty, RegexOptions.IgnoreCase);
        }
        foreach (var alias in _selectAliasMap)
        {
            result = Regex.Replace(result,$@"\b{alias.Value}\s*", string.Empty, RegexOptions.IgnoreCase);
        }
        
        // Remove 'AS' keyword before alias definitions
        result = Regex.Replace(result, @"\bAS\s+", string.Empty, RegexOptions.IgnoreCase);
        
        // Replace alias references with table names
        foreach (var kvp in _aliasToTableMap)
        {
            string tableName = kvp.Value;
            string alias = kvp.Key;

            // Match alias as a whole word followed by a dot (e.g., "u.id" -> "users.id")
            result = Regex.Replace(result, $@"\b{Regex.Escape(alias)}\.(\w+)", $"{tableName}.$1",
                RegexOptions.IgnoreCase);
        }

        return result;
    }

    public void InsertAliases(List<Visualisation> visualisations)
    {
        var selectVis = visualisations.First(v => v.Component.Keyword == SQLKeyword.SELECT);
        var table = selectVis.ToTables[0];
        foreach (var a in _selectAliasMap)
        {
            var parts = a.Key.Trim().Split('.');
            var columnName = _aliasToTableMap[parts[0]] + "." + parts[1];
            var i = table.IndexOfColumn(columnName);
            table.ColumnNames[i] = a.Value;
        }
    }
    
    private void ExtractAliases(string sql)
    {
        _aliasToTableMap.Clear();
        _selectAliasMap.Clear();

        // Extract table aliases from FROM and JOIN clauses
        ExtractTableAliases(sql);

        // Extract column/expression aliases from SELECT clause
        ExtractSelectAliases(sql);
    }
    
    private void ExtractTableAliases(string sql)
    {
        //match from
        string pattern = @"(?:FROM)\s+([^\s]+?)\s(?:as\s+|\s*)([^\s]+?)\s+(?:JOIN|INNER JOIN|LEFT JOIN|RIGHT JOIN|FULL JOIN|CROSS JOIN|NATURAL JOIN|WHERE|GROUP BY|HAVING|ORDER BY|LIMIT|OFFSET|WINDOW|UNION|INTERSECT|EXCEPT)\s";
        var match =
            Regex.Match(sql, pattern, RegexOptions.IgnoreCase);
        
        if (match.Success)
        {
            var tableName = match.Groups[1].Value;
            var alias = match.Groups[2].Value;

            _aliasToTableMap[alias] = tableName;
        }
        
        //match joins
        pattern = @"(?:JOIN)\s+([^\s]+?)\s(?:as\s+|\s*)([^\s]+?)\s+(?:ON)\s";
        var matches =
            Regex.Matches(sql, pattern, RegexOptions.IgnoreCase);

        foreach (Match m in matches)
        {
            var tableName = m.Groups[1].Value;
            var alias = m.Groups[2].Value;

            _aliasToTableMap[alias] = tableName;
        }
    }
    
    private void ExtractSelectAliases(string sql)
    {
        // Extract the SELECT clause (from SELECT to FROM)
        Match selectMatch =
            Regex.Match(sql, @"SELECT\s+(.*?)\s+FROM", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (!selectMatch.Success)
            return;

        string selectClause = selectMatch.Groups[1].Value;

        string pattern = @"([^,]+?)(?:\s)(?:as\s+|\s*)(.+?)(?:,|$)";
        MatchCollection matches =
            Regex.Matches(selectClause, pattern, RegexOptions.IgnoreCase);

        foreach (Match match in matches)
        {
            string tableName = match.Groups[1].Value;
            string alias = match.Groups[2].Value;

            _selectAliasMap[tableName] = alias;
        }
    }
    
    private bool IsReservedKeyword(string word)
    {
        var keywords = new[] { "AND", "OR", "NOT", "IN", "ON", "AS", "BY", "DESC", "ASC", "NULL", "TRUE", "FALSE" };
        return keywords.Contains(word, StringComparer.OrdinalIgnoreCase);
    }
}