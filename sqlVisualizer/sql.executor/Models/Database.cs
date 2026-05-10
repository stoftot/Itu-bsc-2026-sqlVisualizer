using commonDataModels;

namespace sql.executor.Models;

internal class Database(string name, IReadOnlyList<IDatabaseTable> tables) : IDatabase
{
    public string Name() => name;
    public IReadOnlyList<IDatabaseTable> Tables() => tables;
}
