namespace visualizer.service.Contracts;

public interface IDisplayTableRow
{
    public bool IsHighlighted();
    public string HighlightedStyle();
    public IReadOnlyList<IDisplayTableCell> Cells();
}