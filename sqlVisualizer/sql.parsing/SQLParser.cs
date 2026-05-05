using System.Text.RegularExpressions;
using tableGeneration.Contracts;

namespace inputParsing;

public class SQLParser : ISQLParser
{
    private AliasReplacer ar = new();
    private SQLDecomposer dec = new();
    public IEnumerable<ISQLDecompositionComponent> Parse(string sql)
    {
        sql = Regex.Replace(sql, "[ ]{2,}", " ");
        sql = ar.ReplaceAliases(sql);
        return dec.Decompose(sql) ?? throw new InvalidDataException($"Could not decompose the given sql: \"{sql}\"");
    }
}