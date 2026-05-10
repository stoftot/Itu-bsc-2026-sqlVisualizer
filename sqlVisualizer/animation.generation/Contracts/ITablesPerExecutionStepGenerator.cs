namespace animationGeneration.Contracts;

/// <summary>
/// Builds the intermediate execution steps that explain how a query transforms data over time.
/// </summary>
public interface ITablesPerExecutionStepGenerator
{
    /// <summary>
    /// Generates executed steps for a SQL query in execution order.
    /// </summary>
    public IEnumerable<ISqlExecutedStep> Generate(string sql);
}

/// <summary>
/// Represents one executed transformation step in the SQL visualization pipeline.
/// </summary>
public interface ISqlExecutedStep
{
    /// <summary>
    /// Gets the input tables shown before the step is applied.
    /// </summary>
    public IReadOnlyList<ITable> FromTables();

    /// <summary>
    /// Gets the output tables shown after the step is applied.
    /// </summary>
    public IReadOnlyList<ITable> ToTables();

    /// <summary>
    /// Gets the SQL component responsible for the transformation.
    /// </summary>
    public ISQLComponent SQLComponent();
}
