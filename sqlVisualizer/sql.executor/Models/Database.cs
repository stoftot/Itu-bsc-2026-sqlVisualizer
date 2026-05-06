using commonDataModels;

namespace sql.executor.Models;

public class Database(string name, IReadOnlyList<IDatabaseTable> tables) : IDatabase
{
    public string Name() => name;
    public IReadOnlyList<IDatabaseTable> Tables() => tables;
}