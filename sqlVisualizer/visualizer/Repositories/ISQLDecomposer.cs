using visualizer.Models;

namespace visualizer.Repositories;

public interface ISQLDecomposer
{
    List<SQLDecompositionComponent>? Decompose(string sql);
}
