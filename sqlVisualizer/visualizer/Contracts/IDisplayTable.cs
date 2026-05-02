using visualizer.Utility;

namespace visualizer.Models;

public interface IDisplayTable
{
    public IList<IDisplayAggregation> Aggregations();
    public IList<string> ColumnNames();
    public IList<IDisplayTableRow> VisibleRows();
}

public interface IDisplayTableGenerator
{
    public IDisplayTable Generate(ISimpleTable table);
}