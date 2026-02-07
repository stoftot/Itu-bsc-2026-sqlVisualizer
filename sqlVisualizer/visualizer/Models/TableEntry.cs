namespace visualizer.Models;

public class TableEntry
{
    public bool IsHighlighted { get; set; } = false;
    public required List<string> Values { get; set; }
    private string HighlightStyle { get; set; } = "";

    public string GetHighlightStyle()
    {
        if (string.IsNullOrWhiteSpace(HighlightStyle))
        {
            SetHighlightStyleDefault();
        }
        return HighlightStyle;
    }
    
    public void ToggleHighlight() => IsHighlighted = !IsHighlighted;

    public void SetHighlightStyleDefault() => HighlightStyle = "background-color: #FFF3CD;";

    public void SetHighlightHexColor(string hexColor)
    {
        HighlightStyle = $"background-color: #{hexColor};";
        Console.WriteLine(string.Join(",", Values));
    }
}