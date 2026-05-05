using commonDataModels.Models;
using tableGeneration.Contracts;

namespace inputParsing.Models;

public class SQLDecomposedComponent(SQLKeyword keyword, string clause) : ISQLDecompositionComponent
{
    public SQLKeyword Keyword() => keyword;
    public string Clause() => clause;
}