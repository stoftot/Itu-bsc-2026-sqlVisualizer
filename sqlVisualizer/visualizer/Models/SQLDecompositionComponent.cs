namespace visualizer.Models;

public class SQLDecompositionComponent(SQLKeyword keyword, string clause)
{
    public SQLKeyword Keyword { get; } = keyword;
    public string Clause { get; } = clause;

    public override string ToString()
    {
        return $"{Keyword.ToSQLString()} {Clause}";
    }
}