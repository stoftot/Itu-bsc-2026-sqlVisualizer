using visualizer.Exceptions;

namespace visualizer.Contracts;

public interface ISQLInputValidator
{
    /// <exception cref="SQLParseException">thrown when a parse error happens</exception>
    public void Validate(string sql);
}