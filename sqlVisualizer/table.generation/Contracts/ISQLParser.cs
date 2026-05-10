namespace tableGeneration.Contracts;

/// <summary>
/// Parses SQL text into clause-level decomposition components used by the visualization pipeline.
/// </summary>
public interface ISQLParser
{
    /// <summary>
    /// Parses a SQL query into top-level components such as FROM, WHERE, GROUP BY, and SELECT.
    /// </summary>
    public IEnumerable<ISQLDecompositionComponent> Parse(string sql);
}
