namespace commonDataModels;

public interface ISimpleTable
{
    string Name();
    IList<string> ColumnNames();
    IList<IList<object>> Rows();
    IList<string> ColumnTypes();
}