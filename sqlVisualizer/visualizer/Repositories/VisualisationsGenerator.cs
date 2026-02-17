using System.ComponentModel;
using visualizer.Models;

namespace visualizer.Repositories;

public class VisualisationsGenerator(SQLDecomposer decomposer, SQLExecutor sqlExecutor)
{
    public List<Visualisation> Generate(string query)
    {
        var visualisations = new List<Visualisation>();

        GenerateTables(query, visualisations);
        GenerateTableOriginOnColumns(visualisations);
        GenerateAnimations(visualisations);
        
        return visualisations;
    }

    private void GenerateTables(string query, List<Visualisation> visualisations)
    {
        var steps = decomposer.Decompose(query);
        var intialStep = steps.First();
        steps.Remove(intialStep);

        var fromTables = new List<Table>();
        Table toTable;

        fromTables.Add(sqlExecutor.Execute(intialStep).Result);
        fromTables[0].Name = intialStep.Clause.Split(',')[0].Trim();
        
        for (int i = 0; i < steps.Count; i++)
        {
            if (i != 0)
                fromTables.Add(
                    sqlExecutor.Execute(
                        steps[..i].Prepend(intialStep)
                    ).Result);

            var currentStep = steps[i];

            if (currentStep.Keyword.IsJoin())
            {
                var joiningTable = sqlExecutor.Execute(currentStep.GenerateFromClauseFromJoin()).Result;
                joiningTable.Name = currentStep.Clause.Split(' ')[0].Trim();
                fromTables.Add(joiningTable);
            }

            toTable = sqlExecutor.Execute(steps[..(i + 1)].Prepend(intialStep)).Result;

            visualisations.Add(new Visualisation()
            {
                Component = currentStep,
                FromTables = fromTables.ToList(),
                ToTable = toTable
            });

            fromTables.Clear();
        }
    }

    private void GenerateTableOriginOnColumns(List<Visualisation> visualisations)
    {
        var vis = visualisations[0];

        foreach (var table in vis.FromTables)
        {
            table.OrginalTableNames.AddRange(
                Enumerable.Repeat(table.Name, table.ColumnNames.Count)
            );
        }

        for (int i = 0; i < visualisations.Count; i++)
        {
            vis = visualisations[i];

            //this is to just to copy the previous result tables origin columns over into the next steps from table
            if (i > 0)
            {
                DuplicateOriginOnColumns([visualisations[i - 1].ToTable], vis.FromTables[0]);
            }
            
            switch (vis.Component.Keyword)
            {
                case SQLKeyword.JOIN:
                case SQLKeyword.INNER_JOIN:
                    DuplicateOriginOnColumns(vis.FromTables, vis.ToTable);
                    break;
                case SQLKeyword.SELECT:
                    GenerateTableOriginOnColumnsForSelect(vis);
                    break;
                case SQLKeyword.LEFT_JOIN:
                case SQLKeyword.RIGHT_JOIN:
                case SQLKeyword.FULL_JOIN:
                case SQLKeyword.WHERE:
                    DuplicateOriginOnColumns(vis.FromTables, vis.ToTable);
                    break;
                case SQLKeyword.GROUP_BY:
                case SQLKeyword.HAVING:
                case SQLKeyword.ORDER_BY:
                case SQLKeyword.LIMIT:
                case SQLKeyword.OFFSET:
                    throw new NotImplementedException();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    private void DuplicateOriginOnColumns(List<Table> fromTables, Table toTable)
    {
        foreach (var table in fromTables)
        {
            toTable.OrginalTableNames.AddRange(table.OrginalTableNames);
        }
    }

    private void GenerateTableOriginOnColumnsForSelect(Visualisation vis)
    {
        if (vis.FromTables.Count > 1)
            throw new ArgumentException("select is only allowed from one tabel to another table");
        
        var fromTable = vis.FromTables[0];
        
        var columnsSelected = vis.Component.Clause.Split(',').Select(c => c.Trim()).ToList();
        
        foreach (var column in columnsSelected)
        {
            var parts = column.Split('.', 2);
            var tableName = parts.Length == 2 ? parts[0] : null;
            var columnName = parts.Length == 2 ? parts[1] : parts[0];

            for (int i = 0; i < fromTable.ColumnNames.Count; i++)
            {
                if (fromTable.ColumnNames[i].Equals(columnName, StringComparison.InvariantCultureIgnoreCase) &&
                    (tableName == null ||
                     fromTable.OrginalTableNames[i].Equals(tableName, StringComparison.InvariantCultureIgnoreCase)))
                {
                    vis.ToTable.OrginalTableNames.Add(fromTable.OrginalTableNames[i]);
                }
            }
        }
        
        if(vis.ToTable.OrginalTableNames.Count != vis.ToTable.ColumnNames.Count) 
            throw new Exception("count of original table names are supposed to match with the count of columns" +
                                $"\n{vis.ToTable.OrginalTableNames.Count} :  {vis.ToTable.ColumnNames.Count}");
    }

    private void GenerateAnimations(List<Visualisation> visualisations)
    {
        foreach (var vis in visualisations)
        {
            vis.Animation = AnimationGenerator.Generate(vis.FromTables, vis.ToTable, vis.Component);
        }
    }
}