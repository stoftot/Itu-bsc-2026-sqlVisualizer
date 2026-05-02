namespace visualizer.Models;

public interface IDisplayTableRow
{
    public bool IsHighlighted();
    public string HighlightedStyle();
    public IList<IDisplayCell> Cells();
}