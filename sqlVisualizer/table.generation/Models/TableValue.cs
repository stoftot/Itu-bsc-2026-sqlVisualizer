using System.Text;

namespace visualizer.Models;

public class TableValue : TableObjectBase
{
    public required string Value  { get; set; }
    public object? RawValue { get; set; }

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
        return (TableValue)this.MemberwiseClone();
    }

    public override bool Equals(object? obj)
    {
        var compare = obj as TableValue;
        return compare is not null && string.Equals(Value, compare.Value);
    }

    public static int CompareRawValues(object? left, object? right)
    {
        if (ReferenceEquals(left, right)) return 0;
        if (left is null) return -1;
        if (right is null) return 1;

        if (left is IComparable comparable && left.GetType() == right.GetType())
            return comparable.CompareTo(right);

        return string.Compare(left.ToString(), right.ToString(), StringComparison.Ordinal);
    }
}
