namespace visualizer.Repositories.Contracts;

public interface ISQLParser
{
    public IEnumerable<ISQLDecompositionComponent> Parse(string sql);
}