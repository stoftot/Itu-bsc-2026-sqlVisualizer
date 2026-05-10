using commonDataModels.Models;

namespace animationGeneration.Contracts;

/// <summary>
/// Represents the SQL clause associated with an executed visualization step.
/// </summary>
public interface ISQLComponent
{
    /// <summary>
    /// Gets the clause keyword.
    /// </summary>
    public SQLKeyword Keyword();

    /// <summary>
    /// Gets the raw clause text without the leading keyword.
    /// </summary>
    public string Clause();
}
