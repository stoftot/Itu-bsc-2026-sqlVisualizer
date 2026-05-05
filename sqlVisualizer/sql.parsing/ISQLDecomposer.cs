using tableGeneration.Models;

namespace inputParsing;

public interface ISQLDecomposer
{
    List<SQLDecompositionComponent>? Decompose(string sql);
}
