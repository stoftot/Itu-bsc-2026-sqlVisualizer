using System.Collections.Immutable;

namespace visualizer.Models;

public class DisplayTableRow : DisplayableBase, IDisplayTableRow
{
    public required List<DisplayTableTableCell> Cells { get; init; }

    public DisplayTableTableCell this[int column] => Cells[column];

    bool IDisplayTableRow.IsHighlighted() => IsHighlighted;
    public string HighlightedStyle() => GetHighlightStyle();
    IReadOnlyList<IDisplayTableCell> IDisplayTableRow.Cells() => Cells;
    
    public DisplayTableRow DeepClone()
    {
        return new DisplayTableRow
        {
            Cells = Cells
                .Select(v => v.DeepClone())
                .ToList()
        };
    }
    
    public override bool Equals(object? obj)
    {
        return Cells.SequenceEqual(((DisplayTableRow)obj).Cells);
    }

    public ImmutableArray<DisplayTableTableCell> ValuesAsImmutableArray(ICollection<int> columnIndexes)
    {
        return columnIndexes
            .Select(columnIndex => Cells[columnIndex])
            .ToImmutableArray();
    }

    public DisplayTableRow AppendRowIndex(string rowIndex)
    {
        List<DisplayTableTableCell> cells = Cells;
        cells.Add(new DisplayTableTableCell {Value =  rowIndex, RawValue = rowIndex});
        return new DisplayTableRow
        {
            Cells = cells
        };
    }
    
    public bool AreJoinEquivalentToResult(DisplayTableRow joining, DisplayTableRow result)
    {
        var p = Cells.ToList();
        var j = joining.Cells.ToList();
        var r = result.Cells.ToList();

        return p.Concat(j)
            .SequenceEqual(r);
    }
}