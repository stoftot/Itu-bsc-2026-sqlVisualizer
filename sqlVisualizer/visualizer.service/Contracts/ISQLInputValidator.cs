using visualizer.service.Exceptions;

namespace visualizer.service.Contracts;

/// <summary>
/// Validates that a user query is parseable and executable before visualization.
/// </summary>
public interface ISQLInputValidator
{
    /// <summary>
    /// Validates a SQL query.
    /// </summary>
    /// <exception cref="SQLParseException">Thrown when the query cannot be parsed or validated.</exception>
    public void Validate(string sql);
}
