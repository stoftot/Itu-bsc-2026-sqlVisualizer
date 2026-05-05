using commonDataModels.Models;

namespace animationGeneration.Contracts;

public interface ISQLComponent
{
    public SQLKeyword Keyword();
    public string Clause();
}