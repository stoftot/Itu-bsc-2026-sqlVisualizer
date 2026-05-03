namespace visualizer.Repositories.Contracts;

public interface ITablesPerExecutionStepGenerator
{
    public IReadOnlyList<ISqlExecutionStep> Generate(string sql);
}

public interface ISqlExecutionStep
{
    public IReadOnlyList<ITable> FromTables();
    public IReadOnlyList<ITable> ToTables();
    public ISQLComponent SQLComponent();
}