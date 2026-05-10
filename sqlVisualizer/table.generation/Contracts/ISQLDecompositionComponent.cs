using commonDataModels.Models;

namespace tableGeneration.Contracts;

/// <summary>
/// Represents one decomposed SQL clause.
/// </summary>
public interface ISQLDecompositionComponent
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
