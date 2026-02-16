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
}