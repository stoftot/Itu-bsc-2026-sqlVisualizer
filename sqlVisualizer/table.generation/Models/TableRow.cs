namespace tableGeneration.Models;

internal class TableRow
{
    public required List<TableCell> Cells { get; set; }

    public TableCell this[int column] => Cells[column];

    public TableRow DeepClone()
    {
        return new TableRow
        {
            Cells = Cells
                .Select(cell => cell.DeepClone())
                .ToList()
        };
    }

    public override bool Equals(object? obj)
    {
        return obj is TableRow compare && Cells.SequenceEqual(compare.Cells);
    }
    
    public bool AreJoinEquivalentToResult(TableRow joining, TableRow result)
    {
        var sourceCells = Cells.ToList();
        var joiningCells = joining.Cells.ToList();
        var resultCells = result.Cells.ToList();

        return sourceCells.Concat(joiningCells)
            .SequenceEqual(resultCells);
    }
}
