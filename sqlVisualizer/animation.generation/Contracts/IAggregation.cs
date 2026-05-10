namespace animationGeneration.Contracts;

/// <summary>
/// Represents an aggregate value attached to an execution-stage table.
/// </summary>
public interface IAggregation
{
    /// <summary>
    /// Gets the aggregate name or label.
    /// </summary>
    public string Name();

    /// <summary>
    /// Gets the aggregate value as display text.
    /// </summary>
    public string Value();
}
