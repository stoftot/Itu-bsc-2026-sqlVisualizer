using System.ComponentModel;
using visualizer.Models;

namespace visualizer.Repositories;

public class VisualisationsGenerator(SQLDecomposer decomposer, TableGenerator tg, TableOriginColumnsGenerator tocg)
{
    

    public List<Visualisation> Generate(string query)
    {
        var visualisations = new List<Visualisation>();
        var steps = decomposer.Decompose(query);

        GenerateTablesWithOriginOnColumns(steps, visualisations);
        GenerateAnimations(visualisations);

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

        tg.GenerateTablesIntialStep(fromTables, intialStep);

        for (int i = 0; i < steps.Count; i++)
        {
            var currStep = steps[i];
            currSteps.Add(currStep);
            
            //Generate from tables
            tg.GenerateFromTables(currStep, fromTables, prevToTables);
            
            //Generate origin on from tables
            if (i == 0)
                tocg.GenerateTableOriginOnColumnsFromTableName(fromTables);
            
            ValidateOriginColumnsCount(fromTables, currStep);
            
            //Generate to tables
            var currVis = tg.GenerateToTable(steps[i], currSteps, fromTables, toTables);

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