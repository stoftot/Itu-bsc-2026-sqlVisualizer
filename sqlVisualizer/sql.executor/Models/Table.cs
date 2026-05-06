using commonDataModels;

namespace sql.executor.Models;

internal class Table(IList<string> columnNames, IList<IList<object?>> rows, string name = "()", IReadOnlyList<string>? columnTypes = null) : IDatabaseTable
{
    public IList<string> ColumnNames() => columnNames;
    public IList<IList<object?>> Rows() => rows;
    
    public string Name() => name;
    public IReadOnlyList<string> ColumnTypes() => columnTypes ?? new List<string>();
}
