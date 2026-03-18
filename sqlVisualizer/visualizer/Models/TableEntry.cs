using System.Collections.Immutable;

namespace visualizer.Models;

public class TableEntry : TableObjectBase
{
    public required List<TableValue> Values { get; set; }

    public TableEntry DeepClone()
    {
        return new TableEntry
        {
            Values = Values
                .Select(v => v.DeepClone())
                .ToList()
        };
    }

    public override bool Equals(object? obj)
    {
        return Values.SequenceEqual(((TableEntry)obj).Values);
    }

    public ImmutableArray<TableValue> ValuesAsImmutableArray(ICollection<int> columnIndexes)
    {
        return columnIndexes
            .Select(columnIndex => Values[columnIndex])
            .ToImmutableArray();
    }

    public TableEntry AppendRowIndex(string rowIndex)
    {
        List<TableValue> values = Values;
        values.Add(new TableValue {Value =  rowIndex});
        return new TableEntry
        {
            Values = values
        };
    }
}