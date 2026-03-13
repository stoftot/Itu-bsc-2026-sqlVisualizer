using visualizer.Utility;

namespace visualizer.Models;

public abstract class TableObjectBase
{
    private string HighlightStyle { get; set; } = "";
    public bool IsHighlighted { get; set; } = false;
    public bool IsVisible { get; set; } = true;
    
    public void ToggleHighlight() => IsHighlighted = !IsHighlighted;
    public void ToggleVisible() => IsVisible = !IsVisible;
    
    public string GetHighlightStyle()
    {
        if (string.IsNullOrWhiteSpace(HighlightStyle))
        {
            SetHighlightStyleDefault();
        }

        return HighlightStyle;
    }
    
    public void SetHighlightStyleDefault() => SetHighlightHexColor(UtilColor.PrimaryHighlightColor);
    public void SetHighlightStyleSecondary() => SetHighlightHexColor(UtilColor.SecondaryHiglightColor);
    

    public void SetHighlightHexColor(string hexColor) =>
        HighlightStyle = $"background-color: {hexColor};";
}