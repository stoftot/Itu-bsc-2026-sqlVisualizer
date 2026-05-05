namespace tableGeneration.Models;

public class TableEntry
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
    
    public bool AreJoinEquivalentToResult(TableEntry joining, TableEntry result)
    {
        var p = Values.ToList();
        var j = joining.Values.ToList();
        var r = result.Values.ToList();

        return p.Concat(j)
            .SequenceEqual(r);
    }
}
