using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using visualizer.Models;

namespace visualizer.Repositories;

public class TableGenerator(SQLExecutor sqlExecutor, TableOriginColumnsGenerator tocg)
{
    public void GenerateTablesIntialStepWithOriginColumns(List<Table> fromTables, SQLDecompositionComponent intialStep)
    {
        fromTables.Add(sqlExecutor.Execute(intialStep).Result);
        tocg.GenerateTableOriginOnColumnsFromTableName(fromTables);
    }

    public void GenerateFromTablesWithOriginColumns(SQLDecompositionComponent currStep,
        List<Table> fromTables, List<Table> prevToTables, List<SQLDecompositionComponent> currSteps)
    {
        fromTables.AddRange(prevToTables.Select(t => t.DeepClone()).ToList());

        switch (currStep.Keyword)
        {
            case SQLKeyword.JOIN:
            case SQLKeyword.INNER_JOIN:
            case SQLKeyword.LEFT_JOIN:
            case SQLKeyword.LEFT_OUTER_JOIN:
            case SQLKeyword.RIGHT_JOIN:
            case SQLKeyword.RIGHT_OUTER_JOIN:
            case SQLKeyword.FULL_JOIN:
            case SQLKeyword.FULL_OUTER_JOIN:
                GenerateFromTablesJoin(fromTables, currStep);
                break;
            case SQLKeyword.HAVING:
                GenerateFromTablesHaving(fromTables, currStep, currSteps);
                break;
        }
    }

    private void GenerateFromTablesJoin(List<Table> fromTables, SQLDecompositionComponent currentStep)
    {
        var joiningTable = sqlExecutor.Execute(currentStep.GenerateFromClauseFromJoin()).Result;
        joiningTable.Name = ExtractSourceName(currentStep.Clause);
        tocg.GenerateTableOriginOnColumnsFromTableName(joiningTable);
        fromTables.Add(joiningTable);
    }

    private static string ExtractSourceName(string clause)
    {
        var onIndex = clause.IndexOf(" ON ", StringComparison.OrdinalIgnoreCase);
        var beforeOn = onIndex >= 0 ? clause[..onIndex].Trim() : clause.Trim();
        var parts = beforeOn.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length >= 2)
            return parts[^1];

        return parts[0];
    }

    private void GenerateFromTablesHaving(List<Table> fromTables, SQLDecompositionComponent currStep,
        List<SQLDecompositionComponent> currSteps)
    {
        //normalize so count(*) is treated as count()
        var clause = Regex.Replace(currStep.Clause, @"\b[^ ]+?\((?:|\*)\)", "COUNT()");

        const string extractAggregationPattern = @"\b[^ ]+?\(.*?\)";
        var matches = Regex.Matches(clause, extractAggregationPattern,
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        var aggregations = new List<string>();

        foreach (Match match in matches)
        {
            if (!aggregations.Contains(match.Value)) aggregations.Add(match.Value);
        }

        var selectStatement = new SQLDecompositionComponent(SQLKeyword.SELECT, string.Join(",", aggregations));
        var temp = currSteps.ToList();
        temp.Remove(temp.Last());
        temp.Add(selectStatement);
        var aggregationResults = sqlExecutor.Execute(temp).Result;

        if (fromTables.Count != aggregationResults.Entries.Count)
            throw new ArgumentException("The number of aggregation results must match the number of grouped tables(\n" +
                                        $"aggregations: {aggregationResults.Entries.Count}\n" +
                                        $"grouped by tables: {fromTables.Count}\n)");

        //Add aggregationResults to the respective group by tables
        for (int i = 0; i < fromTables.Count; i++)
        {
            var fromTable = fromTables[i];
            var aggregationResult = aggregationResults.Entries[i];

            foreach (var (name, tableValue) in aggregationResults.ColumnNames.Zip(aggregationResult.Values))
            {
                fromTable.Aggregations.Add(new Aggregation()
                {
                    Name = name,
                    Value = tableValue.Value
                });
            }
        }
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
            case SQLKeyword.HAVING:
                GenerateToTablesHaving(fromTables, toTables, currStep, currSteps);
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
            .OrderBy(g => g.Key, CompositeKeyComparer.Instance)
            .Select(g => new Table
            {
                ColumnNames = tabel.ColumnNames.ToList(),
                Entries = g.ToList()
            })
            .ToList();

        toTables.AddRange(groupedTables);
    }

    private void GenerateToTablesHaving(List<Table> fromTables, List<Table> toTables,
        SQLDecompositionComponent currStep, List<SQLDecompositionComponent> currSteps)
    {
        //normalize so count(*) is treated as count()
        var clause = Regex.Replace(currStep.Clause, @"\b[^ ]+?\((?:|\*)\)", "COUNT()");

        const string extractAggregationPattern = @"\b[^ ]+?\(.*?\)";
        var matches = Regex.Matches(clause, extractAggregationPattern,
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        var aggregations = new List<string>();

        foreach (Match match in matches)
        {
            if (!aggregations.Contains(match.Value)) aggregations.Add(match.Value);
        }

        var selectStatement = new SQLDecompositionComponent(SQLKeyword.SELECT, string.Join(",", aggregations));
        var temp = currSteps.ToList();
        temp.Add(selectStatement);
        var aggregationResults = sqlExecutor.Execute(temp).Result;

        //filter out tables
        var aggregationIndex = 0;
        var startOfAggregation = fromTables[0].ColumnsOriginalTableNames.IndexOf("()");
        foreach (var fromTable in fromTables)
        {
            var aggregationValues = aggregationResults.Entries[aggregationIndex].Values
                .Select(v => v.Value);
            // var fromValues = fromTable.Entries[0].Values.GetRange(startOfAggregation, fromTable.Entries[0].Values.Count - startOfAggregation);
            if (fromTable.Aggregations.Count == 0) throw new ArgumentException("Aggregations cannot be empty");
            var fromValues = fromTable.Aggregations
                .Select(a => a.Value);

            if (aggregationValues.SequenceEqual(fromValues))
            {
                toTables.Add(fromTable.DeepClone());
                aggregationIndex++;
                if (aggregationIndex >= aggregationResults.Entries.Count) break;
            }
        }
    }

    private sealed class CompositeKey : IEquatable<CompositeKey>
    {
        private readonly object?[] _values;

        public CompositeKey(IEnumerable<object?> values) => _values = values.ToArray();
        public object?[] GetValues() => _values;

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

    private sealed class CompositeKeyComparer : IComparer<CompositeKey>
    {
        public static readonly CompositeKeyComparer Instance = new();

        public int Compare(CompositeKey? x, CompositeKey? y)
        {
            if (ReferenceEquals(x, y)) return 0;
            if (x is null) return -1;
            if (y is null) return 1;

            var xValues = x.GetValues();
            var yValues = y.GetValues();

            var lengthCompare = xValues.Length.CompareTo(yValues.Length);
            if (lengthCompare != 0) return lengthCompare;

            for (var i = 0; i < xValues.Length; i++)
            {
                var comparison = CompareValue(xValues[i], yValues[i]);
                if (comparison != 0) return comparison;
            }

            return 0;
        }

        private static int CompareValue(object? left, object? right)
        {
            if (ReferenceEquals(left, right)) return 0;
            if (left is null) return -1;
            if (right is null) return 1;

            if (left is IComparable comparable && left.GetType() == right.GetType())
                return comparable.CompareTo(right);

            return string.Compare(left.ToString(), right.ToString(), StringComparison.Ordinal);
        }
    }
}