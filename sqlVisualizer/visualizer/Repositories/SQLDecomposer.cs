using System.Text.RegularExpressions;
using visualizer.Models;

namespace visualizer.Repositories;

public class SQLDecomposer
{
    private IReadOnlyList<SQLKeyword> SupportedKeywords { get; }

    private string ComplimentKeywordsRegex(IReadOnlyCollection<SQLKeyword> without) => string.Join("|",
        SupportedKeywords
            .Where(k => !without.Contains(k))
            .Select(k => "\\s" + (k.ToSQLString().Replace(" ", "\\s")) + "\\s"));

    private string SupportedJoinsRegex { get; }

    public SQLDecomposer(IList<SQLKeyword>? supportedKeywords = null)
    {
        SupportedKeywords = (supportedKeywords ?? Enum.GetValues<SQLKeyword>())
            .AsReadOnly();

        var supportedJoins = SupportedKeywords
            .Where(k => k.IsJoin())
            .ToList();
        SupportedJoinsRegex = string.Join("|",
            supportedJoins.Select(k => "\\s" + (k.ToSQLString().Replace(" ", "\\s")) + "\\s"));
    }

    public List<SQLDecompositionComponent>? Decompose(string sql)
    {
        List<SQLDecompositionComponent> result = [];
        
        sql = sql.ToLower().Replace("\nfrom ", " from ");
        string selectSQL = sql.Split(" from ")[0].Replace("select ", "");
        sql = "from " + sql.Split(" from ")[1];
        
        SQLDecompositionComponent selectClause = new SQLDecompositionComponent(SQLKeyword.SELECT, selectSQL);
        result.Add(selectClause);
        
        var keyWordsPresent = SupportedKeywords
            .Where(k => Regex.IsMatch(
                sql,
                k.ToSQLString(),
                RegexOptions.IgnoreCase | RegexOptions.Singleline))
            .Where(k => !k.IsJoin())
            .ToList();

        if (sql.Contains("join", StringComparison.OrdinalIgnoreCase))
        {
            var joins = DecomposeJoin(sql);
            if (joins == null) throw new Exception("Joins expected but none found.");
            result.AddRange(joins);
        }

        foreach (var keyword in keyWordsPresent)
        {
            var clause = DecomposeKeyword(sql, keyword);
            if (clause == null) throw new Exception($"{keyword} expected but none found.");
            result.Add(clause);
        }

        result = result
            .OrderBy(c => c.Keyword.ExecutionPrecedence())
            .ToList();
        
        return result.Count == 0 ? null : result;
    }

    private List<SQLDecompositionComponent>? DecomposeJoin(string sql)
    {
        var match = Regex.Match(
            sql,
            JoinRegexPattern,
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        List<SQLDecompositionComponent> result = [];

        while (match.Success)
        {
            if (Enum.TryParse<SQLKeyword>(match.Groups[1].Value.Trim().Replace(" ", "_"), true, out var kw))
            {
                result.Add(new SQLDecompositionComponent(kw, match.Groups[2].Value.Trim()));
            }
            else
            {
                throw new Exception("Failed to parse join keyword: " + match.Groups[1].Value);
            }

            match = match.NextMatch();
        }

        return result.Count == 0 ? null : result;
    }

    private SQLDecompositionComponent? DecomposeKeyword(string sql, SQLKeyword keyword)
    {
        var match = Regex.Match(
            sql,
            RegexPatternForKeyword(keyword),
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        if (!match.Success) return null;

        if (Enum.TryParse<SQLKeyword>(match.Groups[1].Value.Trim().Replace(' ', '_'), true, out var kw))
        {
            return new SQLDecompositionComponent(kw, match.Groups[2].Value.Trim());
        }

        throw new Exception($"Failed to parse \"{keyword}\" keyword: " + match.Groups[1].Value);
    }

    /*
     * ?:         - Non-capturing group
     * .. ?       - Lazy quantifier, matches as few characters as needed
     * (?=...)    - Positive lookahead,
     *              asserts that what is written before is followed by what is inside the lookahead
     * (?<=...)   - Positive lookbehind,
     *              asserts that what is written after is preceded by what is inside the lookbehind
     * $          - End of string
     */
    private string RegexPatternForKeyword(SQLKeyword keywords) =>
        $"({keywords.ToSQLString()})(.*?)(?=(?:{ComplimentKeywordsRegex([keywords])})|$)";

    private string JoinRegexPattern =>
        $"({SupportedJoinsRegex})(.*?)(?=(?:{ComplimentKeywordsRegex([])})|$)";
}