using commonDataModels.Models;

namespace tableGeneration.Contracts;

public interface ISQLDecompositionComponent
{
    public SQLKeyword Keyword();
    public string Clause();
}