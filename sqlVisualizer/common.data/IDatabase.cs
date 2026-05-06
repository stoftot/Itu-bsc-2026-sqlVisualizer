namespace commonDataModels;

public interface IDatabase
{
    public string Name();
    public IReadOnlyList<IDatabaseTable> Tables();
}