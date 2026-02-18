namespace visualizer.Models;

public class Visualisation
{
    public Animation Animation { get; set; }
    public List<Table> FromTables  { get; set; }
    public List<Table> ToTables { get; set; }
    public SQLDecompositionComponent Component { get; set; }
}