using visualizer.service.Exceptions;

namespace visualizer.service.Contracts;

public interface ISQLInputValidator
{
    /// <exception cref="SQLParseException">thrown when a parse error happens</exception>
    public void Validate(string sql);
}