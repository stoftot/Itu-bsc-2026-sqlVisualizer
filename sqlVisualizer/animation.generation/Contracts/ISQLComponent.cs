using visualizer.Models;

namespace visualizer.Repositories.Contracts;

public interface ISQLComponent
{
    public SQLKeyword Keyword();
    public string Clause();
}