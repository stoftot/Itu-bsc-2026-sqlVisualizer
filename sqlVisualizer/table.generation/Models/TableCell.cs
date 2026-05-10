using animationGeneration.Contracts;

namespace tableGeneration.Models;

internal class TableCell : ITableCell
{
    public required string Value  { get; set; }
    public object? RawValue { get; set; }

    public TableCell DeepClone()
    {
        return (TableCell)this.MemberwiseClone();
    }

    public override bool Equals(object? obj)
    {
        var compare = obj as TableCell;
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

    string ITableCell.Value()  => Value;
    object? ITableCell.RawValue() => RawValue;
}
