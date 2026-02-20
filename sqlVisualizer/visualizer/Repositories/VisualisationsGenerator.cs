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
        var toTables = new List<Table>();
        var prevToTables = new List<Table>();

        GenerateTablesIntialStep(fromTables, intialStep);

        for (int i = 0; i < steps.Count; i++)
        {
            //copy previous to tables to new from tables
            if (i != 0)
                fromTables.AddRange(prevToTables.Select(t => t.DeepClone()).ToList());

            var currentStep = steps[i];

            if (currentStep.Keyword.IsJoin())
            {
                GenerateTablesJoin(fromTables, currentStep);
            }

            if (currentStep.Keyword == SQLKeyword.GROUP_BY)
            {
                GenerateTablesGroupBy(fromTables, toTables, currentStep);
            }
            else
            {
                toTables.Add(
                    sqlExecutor.Execute(steps[..(i + 1)].Prepend(intialStep)).Result);
            }

            visualisations.Add(new Visualisation()
            {
                Component = currentStep,
                FromTables = fromTables.ToList(),
                ToTables = toTables.ToList()
            });

            prevToTables = toTables.ToList();
            fromTables.Clear();
            toTables.Clear();
        }
    }

    private void GenerateTablesIntialStep(List<Table> fromTables, SQLDecompositionComponent intialStep)
    {
        fromTables.Add(sqlExecutor.Execute(intialStep).Result);
        fromTables[0].Name = intialStep.Clause.Split(',')[0].Trim();
    }

    private void GenerateTablesJoin(List<Table> fromTables, SQLDecompositionComponent currentStep)
    {
        var joiningTable = sqlExecutor.Execute(currentStep.GenerateFromClauseFromJoin()).Result;
        joiningTable.Name = currentStep.Clause.Split(' ')[0].Trim();
        fromTables.Add(joiningTable);
    }

    private void GenerateTablesGroupBy(List<Table> fromTables, List<Table> toTables, SQLDecompositionComponent currentStep)
    {
        //TODO: Add support for tableName.Coulmname goup by
        if (fromTables.Count > 1)
            throw new ArgumentException("Group by can only be generated when there is only one from table");
        var tabel = fromTables[0].DeepClone();
        var columnNamesToGroupBy = currentStep.Clause.Split(',');

        var groupByIndexes = new List<int>();
        
        foreach (var columName in columnNamesToGroupBy)
        {
            groupByIndexes.Add(tabel.IndexOfColumn(columName.Trim()));
        }

        var groupedTables = tabel.Entries
            .GroupBy(e => new CompositeKey(groupByIndexes.Select(i => e.Values[i].Value)))
            .Select(g => new Table
            {
                ColumnNames = tabel.ColumnNames.ToList(),
                Entries = g.ToList()
            })
            .Reverse()
            .ToList();

        toTables.AddRange(groupedTables);
    }
    
    public sealed class CompositeKey : IEquatable<CompositeKey>
    {
        private readonly object?[] _values;

        public CompositeKey(IEnumerable<object?> values) => _values = values.ToArray();

        public bool Equals(CompositeKey? other)
            => other is not null && _values.SequenceEqual(other._values);

        public override bool Equals(object? obj) => obj is CompositeKey other && Equals(other);

        public override int GetHashCode()
        {
            var hc = new HashCode();
            foreach (var v in _values) hc.Add(v);
            return hc.ToHashCode();
        }
    }

    private void GenerateTableOriginOnColumns(List<Visualisation> visualisations)
    {
        var vis = visualisations[0];

        foreach (var table in vis.FromTables)
        {
            table.ColumnsOriginalTableNames.AddRange(
                Enumerable.Repeat(table.Name, table.ColumnNames.Count)
            );
        }

        for (int i = 0; i < visualisations.Count; i++)
        {
            vis = visualisations[i];

            //this is just to copy the previous result tables origin columns over into the next steps from table
            if (i > 0)
            {
                var prevToTables = visualisations[i - 1].ToTables;
                if (prevToTables.Count == 1)
                {
                    DuplicateOriginOnColumnsToSingle(prevToTables[0], vis.FromTables[0]);
                }
                else
                {
                    DuplicateOriginOnColumnsToMulti(prevToTables, vis.FromTables);
                }
            }

            switch (vis.Component.Keyword)
            {
                case SQLKeyword.JOIN:
                case SQLKeyword.INNER_JOIN:
                    DuplicateOriginOnColumnsToSingle(vis.FromTables, vis.ToTables[0]);
                    break;
                case SQLKeyword.SELECT:
                    GenerateTableOriginOnColumnsForSelect(vis);
                    break;
                case SQLKeyword.GROUP_BY:
                    DuplicateOriginOnColumnsToMulti(vis.FromTables[0], vis.ToTables);
                    break;
                case SQLKeyword.LEFT_JOIN:
                case SQLKeyword.RIGHT_JOIN:
                case SQLKeyword.FULL_JOIN:
                case SQLKeyword.WHERE:
                    DuplicateOriginOnColumnsToSingle(vis.FromTables, vis.ToTables[0]);
                    break;
                case SQLKeyword.HAVING:
                case SQLKeyword.ORDER_BY:
                case SQLKeyword.LIMIT:
                case SQLKeyword.OFFSET:
                    throw new NotImplementedException();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    private void DuplicateOriginOnColumnsToSingle(Table fromTable, Table toTable)
    {
        DuplicateOriginOnColumnsToSingle([fromTable], toTable);
    }

    private void DuplicateOriginOnColumnsToSingle(List<Table> fromTables, Table toTable)
    {
        foreach (var table in fromTables)
        {
            toTable.ColumnsOriginalTableNames.AddRange(table.ColumnsOriginalTableNames);
        }
    }

    private void DuplicateOriginOnColumnsToMulti(Table fromTable, List<Table> toTables)
    {
        foreach (var table in toTables)
        {
            DuplicateOriginOnColumnsToSingle(fromTable, table);
        }
    }

    private void DuplicateOriginOnColumnsToMulti(List<Table> fromTables, List<Table> toTables)
    {
        if (fromTables.Count != toTables.Count)
            throw new ArgumentException("count of from tables and to tables are supposed to match");
        for (int i = 0; i < fromTables.Count; i++)
        {
            DuplicateOriginOnColumnsToSingle(fromTables[i], toTables[i]);
        }
    }

    private void GenerateTableOriginOnColumnsForSelect(Visualisation vis)
    {
        var allTablesHaveSameColumns = vis.FromTables
            .All(t => t.ColumnNames.SequenceEqual(vis.FromTables[0].ColumnNames));
        if (!allTablesHaveSameColumns)
            throw new ArgumentException("select is only allowed from when selecting from tabels" +
                                        ", that all contain the same colunmns");

        var toTable = vis.ToTables[0];
        var fromTable = vis.FromTables[0];
        
        if(vis.Component.Clause.Trim().Equals("*"))
            DuplicateOriginOnColumnsToSingle(fromTable, toTable);
        
        
        var columnsSelected = vis.Component.Clause.Split(',').Select(c => c.Trim()).ToList();
        
        foreach (var column in columnsSelected)
        {
            //check if agregate founction
            if (column.Contains('('))
            {
                toTable.ColumnsOriginalTableNames.Add("()");
                continue;
            }

            var parts = column.Split('.', 2);
            var tableName = parts.Length == 2 ? parts[0] : null;
            var columnName = parts.Length == 2 ? parts[1] : parts[0];

            for (int i = 0; i < fromTable.ColumnNames.Count; i++)
            {
                if (fromTable.ColumnNames[i].Equals(columnName, StringComparison.InvariantCultureIgnoreCase) &&
                    (tableName == null ||
                     fromTable.ColumnsOriginalTableNames[i].Equals(tableName, StringComparison.InvariantCultureIgnoreCase)))
                {
                    toTable.ColumnsOriginalTableNames.Add(fromTable.ColumnsOriginalTableNames[i]);
                }
            }
        }

        if (toTable.ColumnsOriginalTableNames.Count != toTable.ColumnNames.Count)
            throw new Exception("count of original table names are supposed to match with the count of columns" +
                                $"\n{toTable.ColumnsOriginalTableNames.Count} :  {toTable.ColumnNames.Count}");
    }

    private void GenerateAnimations(List<Visualisation> visualisations)
    {
        foreach (var vis in visualisations)
        {
            // vis.Animation = AnimationGenerator.Generate(vis.FromTables, vis.ToTables, vis.Component);
        }
    }
}