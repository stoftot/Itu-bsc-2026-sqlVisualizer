using System.ComponentModel;
using System.Text.RegularExpressions;
using Visualizer;
using visualizer.Models;

namespace visualizer.Repositories;

public class VisualisationsGenerator(ISQLDecomposer decomposer, TableGenerator tg, TableOriginColumnsGenerator tocg, AliasReplacer ar)
{
    public List<Visualisation> Generate(string query)
    {
        var visualisations = new List<Visualisation>();
        query = Regex.Replace(query, "[ ]{2,}", " ");
        query = ar.ReplaceAliases(query);
        var steps = decomposer.Decompose(query);

        GenerateTablesWithOriginOnColumns(steps, visualisations);
        GenerateAnimations(visualisations);

        return visualisations;
    }

    private void GenerateTablesWithOriginOnColumns(List<SQLDecompositionComponent> steps, List<Visualisation> visualisations)
    {
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
            var currVis = tg.GenerateToTable(currStep, currSteps, fromTables, toTables);

            prevToTables = toTables.ToList();
            fromTables.Clear();
            toTables.Clear();
            
            //Generate origin on to tables
            tocg.GenerateTableOriginOnToTablesColumns(currVis);
            ValidateOriginColumnsCount(currVis.ToTables, currVis.Component);
            ValidateThatNumberColumnsInEntriesMatchWithNumberOfColumnsInTables(currVis.ToTables, currVis.Component);
            
            visualisations.Add(currVis);
        }
    }

    private void GenerateAnimations(List<Visualisation> visualisations)
    {
        foreach (var vis in visualisations)
        {
            if (vis.ToTables.TrueForAll(t => t.Entries.Count == 0))
                vis.Animation = new Animation(new List<Action>());
            else
                vis.Animation = AnimationGenerator.Generate(vis.FromTables, vis.ToTables, vis.Component);
        }
    }

    private void ValidateOriginColumnsCount(List<Table> tables, SQLDecompositionComponent component)
    {
        foreach (var table in tables)
        {
            if (table.ColumnsOriginalTableNames.Count != table.ColumnNames.Count)
                throw new Exception("count of original table names are supposed to match with the count of columns" +
                                    $"\n{table.ColumnsOriginalTableNames.Count} :  {table.ColumnNames.Count}"+
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
