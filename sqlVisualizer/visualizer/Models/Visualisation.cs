namespace visualizer.Models;

public class Visualisation
{
    public Animation Animation { get; set; }
    public List<Table> FromTables  { get; set; }
    public Table ToTable { get; set; }
    public SQLDecompositionComponent Component { get; set; }
}