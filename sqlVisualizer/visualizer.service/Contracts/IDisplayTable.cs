using commonDataModels;

namespace visualizer.service.Contracts;

public interface IDisplayTable
{
    public IReadOnlyList<IDisplayAggregation> Aggregations();
    public IReadOnlyList<string> ColumnNames();
    public IEnumerable<IDisplayTableRow> VisibleRows();
}

public interface IDisplayTableGenerator
{
    public IDisplayTable Generate(ISimpleTable table);
}