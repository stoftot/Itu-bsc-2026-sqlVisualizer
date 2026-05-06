using commonDataModels;

namespace animationGeneration.Models;

internal class DisplayableBase
{
    private string HighlightColor { get; set; }
    private string PreviousHighlightColor { get; set; }
    
    public bool IsHighlighted { get; set; } = false;
    public bool IsVisible { get; set; } = true;
    
    public void ToggleHighlight() => IsHighlighted = !IsHighlighted;
    public void ToggleVisible() => IsVisible = !IsVisible;

    public string GetHighlightStyle() => $"background-color: {HighlightColor};";
    
    public void SetHighlightColorDefault() => SetHighlightHexColor(UtilColor.PrimaryHighlightColor);
    
    public void SetHighlightHexColor(string hexColor)
    {
        PreviousHighlightColor = HighlightColor;
        HighlightColor = hexColor;
    }
    
    public void SetHighlightColorPrevious() => SetHighlightHexColor(PreviousHighlightColor);

    public void SetHighlightStyleDefault()
    {
        SetHighlightColorDefault();
        IsHighlighted = true;
    }
    
    public void ResetStyleAndVisual()
    {
        IsHighlighted = false;
        IsVisible = true;
        SetHighlightColorDefault();
    }
}
