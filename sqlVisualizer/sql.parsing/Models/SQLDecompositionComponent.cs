namespace visualizer.Models;

public class SQLDecompositionComponent(SQLKeyword keyword, string clause)
{
    public SQLKeyword Keyword { get; } = keyword;
    public string Clause { get; } = clause;

    public override string ToString()
    {
        return $"{Keyword.ToSQLString()} {Clause}";
    }
    
    public string ToExecutableClause()
    {
        return ToString();
    }
    
    public SQLDecompositionComponent GenerateFromClauseFromJoin()
    {
        if (!Keyword.IsJoin())
            throw new InvalidOperationException("Only join components can generate a FROM clause.");

        return new SQLDecompositionComponent
            (SQLKeyword.FROM, Clause[..Clause.IndexOf(' ')] );
    }
}