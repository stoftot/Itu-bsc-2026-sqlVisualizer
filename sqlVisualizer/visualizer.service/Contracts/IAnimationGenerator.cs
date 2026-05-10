namespace visualizer.service.Contracts;

/// <summary>
/// Generates UI-ready animations from a SQL query.
/// </summary>
public interface IAnimationGenerator
{
    /// <summary>
    /// Generates one animation per SQL execution step.
    /// </summary>
    public IReadOnlyList<IAnimation> Generate(string sql);
}
