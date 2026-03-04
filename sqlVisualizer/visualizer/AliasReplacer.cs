using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

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
            result = Regex.Replace(result,$@"\b{alias.Value}\s+", string.Empty, RegexOptions.IgnoreCase);
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
            string alias = kvp.Value;
            string tableName = kvp.Key;

            // Match alias as a whole word followed by a dot (e.g., "u.id" -> "users.id")
            result = Regex.Replace(result, $@"\b{Regex.Escape(alias)}\.(\w+)", $"{tableName}.$1",
                RegexOptions.IgnoreCase);
        }

        return result;
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
        // Pattern to match: (FROM|JOIN) table_name (AS)? alias
        string pattern = @"(?:FROM|JOIN)\s+(.+?)(?:\s)(?:as\s+|\s*)(.+?)(?:\s+?)";
        MatchCollection matches =
            Regex.Matches(sql, pattern, RegexOptions.IgnoreCase);

        foreach (Match match in matches)
        {
            string tableName = match.Groups[1].Value;
            string alias = match.Groups[2].Value;

            _aliasToTableMap[tableName] = alias;
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