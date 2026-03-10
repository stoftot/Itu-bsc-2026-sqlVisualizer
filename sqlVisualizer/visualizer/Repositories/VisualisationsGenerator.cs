using System.ComponentModel;
using Visualizer;
using visualizer.Models;

namespace visualizer.Repositories;

public class VisualisationsGenerator(SQLDecomposer decomposer, TableGenerator tg, TableOriginColumnsGenerator tocg, AliasReplacer ar)
{
    

    public List<Visualisation> Generate(string query)
    {
        var visualisations = new List<Visualisation>();
        // query = ar.ReplaceAliases(query);
        var steps = decomposer.Decompose(query);

        GenerateTablesWithOriginOnColumns(steps, visualisations);
        GenerateAnimations(visualisations);
        
        // ar.InsertAliases(visualisations);

        return visualisations;
    }

    private void GenerateTablesWithOriginOnColumns(List<SQLDecompositionComponent> steps, List<Visualisation> visualisations)
    {
        var intialStep = steps.First();
        steps.Remove(intialStep);

        var fromTables = new List<Table>();
        var toTables = new List<Table>();
        var prevToTables = new List<Table>();
        List<SQLDecompositionComponent> currSteps = [intialStep];

        tg.GenerateTablesIntialStepWithOriginColumns(fromTables, intialStep);

        foreach (var currStep in steps)
        {
            currSteps.Add(currStep);
            
            //Generate from tables
            tg.GenerateFromTablesWithOriginColumns(currStep, fromTables, prevToTables);
            
            ValidateOriginColumnsCount(fromTables, currStep);
            
            //Generate to tables
            var currVis = tg.GenerateToTable(currStep, currSteps, fromTables, toTables);

            prevToTables = toTables.ToList();
            fromTables.Clear();
            toTables.Clear();
            
            //Generate origin on to tables
            tocg.GenerateTableOriginOnToTablesColumns(currVis);
            ValidateOriginColumnsCount(currVis.ToTables, currVis.Component);
            
            visualisations.Add(currVis);
        }
    }

    private void GenerateAnimations(List<Visualisation> visualisations)
    {
        foreach (var vis in visualisations)
        {
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
}