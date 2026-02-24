using visualizer.Models;

namespace visualizer.Repositories;

public class TableGenerator(SQLExecutor sqlExecutor)
{
    public void GenerateFromTables(SQLDecompositionComponent currStep, 
        List<Table> fromTables, List<Table> prevToTables)
    {
        fromTables.AddRange(prevToTables.Select(t => t.DeepClone()).ToList());

        switch (currStep.Keyword)
        {
            case SQLKeyword.JOIN:
            case SQLKeyword.INNER_JOIN:
            case SQLKeyword.LEFT_JOIN:
            case SQLKeyword.RIGHT_JOIN:
            case SQLKeyword.FULL_JOIN:
                GenerateFromTablesJoin(fromTables, currStep);
                break;
        }
    }
    
    private void GenerateFromTablesJoin(List<Table> fromTables, SQLDecompositionComponent currentStep)
    {
        var joiningTable = sqlExecutor.Execute(currentStep.GenerateFromClauseFromJoin()).Result;
        joiningTable.Name = currentStep.Clause.Split(' ')[0].Trim();
        fromTables.Add(joiningTable);
    }

    public Visualisation GenerateToTable(SQLDecompositionComponent currStep,
        List<SQLDecompositionComponent> currSteps,
        List<Table> fromTables, List<Table> toTables)
    {
        switch (currStep.Keyword)
        {
            case SQLKeyword.GROUP_BY:
                GenerateToTablesGroupBy(fromTables, toTables, currStep);
                break;
            default:
                toTables.Add(
                    sqlExecutor.Execute(currSteps).Result);
                break;
        }

        return new Visualisation
        {
            Component = currStep,
            FromTables = fromTables.ToList(),
            ToTables = toTables.ToList()
        };
    }

    public void GenerateTablesIntialStep(List<Table> fromTables, SQLDecompositionComponent intialStep)
    {
        fromTables.Add(sqlExecutor.Execute(intialStep).Result);
        fromTables[0].Name = intialStep.Clause.Split(',')[0].Trim();
    }

    private void GenerateToTablesGroupBy(List<Table> fromTables, List<Table> toTables,
        SQLDecompositionComponent currentStep)
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

    private sealed class CompositeKey : IEquatable<CompositeKey>
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
}