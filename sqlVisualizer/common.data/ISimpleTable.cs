namespace commonDataModels;

public interface ISimpleTable
{
    IList<string> ColumnNames();
    IList<IList<object?>> Rows();
}