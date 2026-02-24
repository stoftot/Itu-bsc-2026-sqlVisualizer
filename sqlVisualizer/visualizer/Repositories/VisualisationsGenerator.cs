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
            if (i > 0)
            {
                var prevVis = visualisations[i - 1];
                if (prevVis.ToTables.Count == 1)
                {
                    tocg.DuplicateOriginOnColumnsToSingle(prevVis.ToTables[0], fromTables[0]);
                }
                else
                {
                    tocg.DuplicateOriginOnColumnsToMulti(prevVis.ToTables, fromTables);
                }
            }
            else
            {
                tocg.GenerateTableOriginOnColumnsFromTableName(fromTables);
            }
            
            //Generate to tables
            var currVis = tg.GenerateToTable(steps[i], currSteps, fromTables, toTables);

            prevToTables = toTables.ToList();
            fromTables.Clear();
            toTables.Clear();
            
            //Generate origin on to tables
            tocg.GenerateTableOriginOnToTablesColumns(currVis);
            
            visualisations.Add(currVis);
        }
    }

    private void GenerateAnimations(List<Visualisation> visualisations)
    {
        foreach (var vis in visualisations)
        {
            // vis.Animation = AnimationGenerator.Generate(vis.FromTables, vis.ToTables, vis.Component);
        }
    }
}