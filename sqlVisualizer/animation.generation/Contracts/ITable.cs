namespace animationGeneration.Contracts;

public interface ITable
{
    public string Name();
    public List<string> ColumnsOriginalTableNames();
    public List<string> ColumnNames();
    public IReadOnlyList<IReadOnlyList<ITableCell>> Data();
    public IReadOnlyList<IAggregation> Aggregations();
}