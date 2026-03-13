using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using visualizer.Models;
using visualizer.Utility;

namespace Visualizer;

public class AliasReplacer
{
    private Dictionary<string, string> AliasToTableMap { get; } = new Dictionary<string, string>(); 
    private Dictionary<string, string> SelectAliasMap { get; } = new Dictionary<string, string>(); 
    public string ReplaceAliases(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
            return sql;
        
        var selectMatch =
            Regex.Match(sql, UtilRegex.SelectToFromInclusivePattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
        var selectPart = selectMatch.Groups[0].Value;
        
        var queryBodyMatch =
            Regex.Match(sql, @"\bFROM\b(.*)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        var queryBody = queryBodyMatch.Groups[1].Value;
        
        // Extract table aliases from FROM and JOIN clauses
        ExtractTableAliases("FROM" + queryBody);

        // Extract column/expression aliases from SELECT clause
        ExtractSelectAliases(selectPart);
        
        // Remove alias definitions
        foreach (var (alias, _) in AliasToTableMap)
        {
            queryBody = Regex.Replace(queryBody,$@"(\s+AS\s+|\s+){alias}(?=\s)", string.Empty, RegexOptions.IgnoreCase);
        }
        foreach (var (_, alias) in SelectAliasMap)
        {
            selectPart = Regex.Replace(selectPart,$@"(\s+AS\s+|\s+){alias}((?=\s*FROM)|[ ]*(?=,))", string.Empty, RegexOptions.IgnoreCase);
        }
        
        var result = selectPart + queryBody;
        
        // Replace alias references with table names
        foreach (var (alias, tableName) in AliasToTableMap)
        {
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
        foreach (var a in SelectAliasMap)
        {
            var parts = a.Key.Trim().Split('.');
            var columnName = AliasToTableMap[parts[0]] + "." + parts[1];
            var i = table.IndexOfColumn(columnName);
            table.ColumnNames[i] = a.Value;
        }
    }
    
    private void ExtractTableAliases(string sql)
    {
        //match from
        var match =
            Regex.Match(sql, UtilRegex.AliasForFromTablePattern, RegexOptions.IgnoreCase);
        
        if (match.Success)
        {
            var tableName = match.Groups[1].Value.Trim();
            var alias = match.Groups[2].Value.Trim();

            AliasToTableMap[alias] = tableName;
        }
        
        //match joins
        var matches =
            Regex.Matches(sql, UtilRegex.AliasForJoinsPattern, RegexOptions.IgnoreCase);

        foreach (Match m in matches)
        {
            var tableName = m.Groups[1].Value.Trim();
            var alias = m.Groups[2].Value.Trim();

            AliasToTableMap[alias] = tableName;
        }
    }
    
    private void ExtractSelectAliases(string sql)
    {
        // Extract the SELECT clause (from SELECT to FROM)
        var selectMatch =
            Regex.Match(sql, UtilRegex.SelectClausePattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (!selectMatch.Success)
            return;

        var selectClause = selectMatch.Groups[1].Value;
        
        var matches =
            Regex.Matches(selectClause, UtilRegex.ExtractAliasForSelectClausePattern, RegexOptions.IgnoreCase);

        foreach (Match match in matches)
        {
            var tableName = match.Groups[1].Value;
            var alias = match.Groups[2].Value;

            SelectAliasMap[tableName] = alias;
        }
    }
}