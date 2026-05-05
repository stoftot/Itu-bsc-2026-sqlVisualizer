namespace commonDataModels;

public interface ISQLExecutor
{
    public Task<ISimpleTable> Execute(string sqlQuery);
    public Task<IDatabase> GetDatabase(string? connectionString = null);
}