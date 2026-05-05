namespace animationGeneration.Contracts;

public interface ITablesPerExecutionStepGenerator
{
    public IEnumerable<ISqlExecutedStep> Generate(string sql);
}

public interface ISqlExecutedStep
{
    public IReadOnlyList<ITable> FromTables();
    public IReadOnlyList<ITable> ToTables();
    public ISQLComponent SQLComponent();
}