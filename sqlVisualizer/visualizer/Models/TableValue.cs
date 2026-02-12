namespace visualizer.Models;

public class TableValue : TableObjectBase
{
    public string OriginalTableName { get; set; } = string.Empty;
    public required string Value  { get; set; }
}