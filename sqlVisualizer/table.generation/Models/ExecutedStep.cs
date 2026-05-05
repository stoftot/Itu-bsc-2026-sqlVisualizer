using visualizer.Repositories.Contracts;

namespace visualizer.Models;

public class ExecutedStep : ISqlExecutedStep
{
    public required List<Table> FromTables { get; set; }
    public required List<Table> ToTables { get; set; }
    public required SQLDecompositionComponent Step { get; set; }
    
    IReadOnlyList<ITable> ISqlExecutedStep.FromTables() => FromTables;
    IReadOnlyList<ITable> ISqlExecutedStep.ToTables() => ToTables;
    public ISQLComponent SQLComponent() => Step;
}