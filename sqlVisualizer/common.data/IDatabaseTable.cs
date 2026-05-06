namespace commonDataModels;

public interface IDatabaseTable : ISimpleTable
{
    public string Name();
    public IReadOnlyList<string> ColumnTypes();
}