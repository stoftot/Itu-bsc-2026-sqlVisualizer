using visualizer.Models;

namespace visualizer.Repositories.Contracts;

public interface ISQLDecompositionComponent
{
    public SQLKeyword Keyword();
    public string Clause();
}