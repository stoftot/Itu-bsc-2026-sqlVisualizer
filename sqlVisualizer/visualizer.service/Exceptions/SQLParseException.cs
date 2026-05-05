namespace visualizer.service.Exceptions;

public class SQLParseException : Exception
{
    public SQLParseException(){}
    public SQLParseException(string message) : base(message) { }
    public SQLParseException(string message, Exception inner) : base(message, inner) { }
}