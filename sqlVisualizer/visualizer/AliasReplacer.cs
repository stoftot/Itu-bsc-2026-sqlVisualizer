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
            result = Regex.Replace(result,$@"\b{alias.Key}\s+", string.Empty, RegexOptions.IgnoreCase);
        }
        foreach (var alias in _selectAliasMap)
        {
            result = Regex.Replace(result,$@"\b{alias.Key}\s*", string.Empty, RegexOptions.IgnoreCase);
        }
        
        // Remove 'AS' keyword before alias definitions
        result = Regex.Replace(result, @"\bAS\s+", string.Empty, RegexOptions.IgnoreCase);
        
        // Replace alias references with table names
        foreach (var kvp in _aliasToTableMap)
        {
            string alias = kvp.Key;
            string tableName = kvp.Value;

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

        // Split by comma to get individual column expressions
        string[] columns = selectClause.Split(',');

        foreach (string column in columns)
        {
            string trimmed = column.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
                continue;

            // Pattern 1: "expression AS alias"
            Match asMatch = Regex.Match(trimmed, @"\s+AS\s+(\w+)\s*$", RegexOptions.IgnoreCase);
            if (asMatch.Success)
            {
                string alias = asMatch.Groups[1].Value;
                string expression = Regex.Replace(trimmed, @"\s+AS\s+\w+\s*$", string.Empty, RegexOptions.IgnoreCase)
                    .Trim();
                _selectAliasMap[alias] = expression;
                continue;
            }

            // Pattern 2: "expression alias" (just a space, last word is the alias)
            string[] parts = Regex.Split(trimmed, @"\s+");
            if (parts.Length >= 2)
            {
                string lastPart = parts[parts.Length - 1];
                // Only treat it as an alias if it's a simple identifier and not a reserved keyword
                if (Regex.IsMatch(lastPart, @"^\w+$") && !IsReservedKeyword(lastPart))
                {
                    string expression = string.Join(" ", parts, 0, parts.Length - 1);
                    _selectAliasMap[lastPart] = expression;
                }
            }
        }
    }
    
    private bool IsReservedKeyword(string word)
    {
        var keywords = new[] { "AND", "OR", "NOT", "IN", "ON", "AS", "BY", "DESC", "ASC", "NULL", "TRUE", "FALSE" };
        return keywords.Contains(word, StringComparer.OrdinalIgnoreCase);
    }
}