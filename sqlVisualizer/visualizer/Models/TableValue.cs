using System.Text;

namespace visualizer.Models;

public class TableValue : TableObjectBase
{
    public required string Value  { get; set; }

    public string GetStyle()
    {
        var styleBuilder = new StringBuilder();

        if (IsHighlighted)
        {
            styleBuilder.Append(GetHighlightStyle());
            styleBuilder.Append(';');
        }

        if (!IsVisible)
        {
            styleBuilder.Append("visibility:hidden");
            styleBuilder.Append(';');
        }
        
        return styleBuilder.ToString();
    }

    public TableValue DeepClone()
    {
        return new TableValue{ Value = Value };
    }
}