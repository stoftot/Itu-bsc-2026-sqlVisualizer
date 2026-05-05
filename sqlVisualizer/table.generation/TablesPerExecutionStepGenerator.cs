using animationGeneration.Contracts;
using commonDataModels.Models;
using tableGeneration.Contracts;
using tableGeneration.Models;

namespace tableGeneration;

public class TablesPerExecutionStepGenerator(SQLExecutorWrapper executorWrapper, ISQLParser parser)
    : ITablesPerExecutionStepGenerator
{
    private static TableOriginColumnsGenerator tocg = new();
    private TableGenerator tg = new(executorWrapper, tocg);
    
    public IEnumerable<ISqlExecutedStep> Generate(string sql)
    {
        var executedSteps = new List<ISqlExecutedStep>();
        var steps = parser.Parse(sql)
            .Select(c => new SQLDecompositionComponent(c.Keyword(), c.Clause())).ToList();

        // WITH is execution context (CTEs), not a visualisation step — extract it up front.
        var withComponent = steps.FirstOrDefault(s => s.Keyword == SQLKeyword.WITH);
        if (withComponent != null) steps.Remove(withComponent);

        var intialStep = steps.First();
        steps.Remove(intialStep);

        var fromTables = new List<Table>();
        var toTables = new List<Table>();
        var prevToTables = new List<Table>();
        List<SQLDecompositionComponent> currSteps = withComponent != null
            ? [withComponent, intialStep]
            : [intialStep];

        tg.GenerateTablesIntialStepWithOriginColumns(fromTables, intialStep, currSteps);

        foreach (var currStep in steps)
        {
            currSteps.Add(currStep);

            //Generate from tables
            tg.GenerateFromTablesWithOriginColumns(currStep, fromTables, prevToTables, currSteps);

            ValidateOriginColumnsCount(fromTables, currStep);
            ValidateThatNumberColumnsInEntriesMatchWithNumberOfColumnsInTables(fromTables, currStep);

            //Generate to tables
            var currExecution = tg.GenerateToTable(currStep, currSteps, fromTables, toTables);

            prevToTables = toTables.ToList();
            fromTables.Clear();
            toTables.Clear();

            //Generate origin on to tables
            tocg.GenerateTableOriginOnToTablesColumns(currExecution);
            ValidateOriginColumnsCount(currExecution.ToTables, currExecution.Step);
            ValidateThatNumberColumnsInEntriesMatchWithNumberOfColumnsInTables(currExecution.ToTables, currExecution.Step);

            executedSteps.Add(currExecution);
        }
        
        return executedSteps;
    }

    private void ValidateOriginColumnsCount(List<Table> tables, SQLDecompositionComponent component)
    {
        foreach (var table in tables)
        {
            if (table.ColumnsOriginalTableNames.Count != table.ColumnNames.Count)
                throw new Exception("count of original table names are supposed to match with the count of columns" +
                                    $"\n{table.ColumnsOriginalTableNames.Count} :  {table.ColumnNames.Count}" +
                                    $"\nStatment: \"{component}\"");
        }
    }

    private void ValidateThatNumberColumnsInEntriesMatchWithNumberOfColumnsInTables
        (List<Table> tables, SQLDecompositionComponent component)
    {
        foreach (var table in tables)
        {
            var numberOfColumnsInTables = table.ColumnNames.Count;
            var correct = table.Entries.TrueForAll(e => e.Values.Count == numberOfColumnsInTables);
            if (!correct)
                throw new Exception("table contains incorrect number of entries" +
                                    $"\nStatment: \"{component}\"");
        }
    }
}