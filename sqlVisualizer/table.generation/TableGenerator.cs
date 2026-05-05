using System.Text.RegularExpressions;
using commonDataModels;
using commonDataModels.Models;
using tableGeneration.Models;

namespace tableGeneration;

public class TableGenerator(SQLExecutorWrapper sqlExecutor, TableOriginColumnsGenerator tocg)
{
    public void GenerateTablesIntialStepWithOriginColumns(List<Table> fromTables, SQLDecompositionComponent intialStep,
        List<SQLDecompositionComponent> currSteps)
    {
        var table = sqlExecutor.Execute(currSteps).Result;
        table.Name = intialStep.Clause.Split(' ')[^1];
        fromTables.Add(table);
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
                GenerateFromTablesJoin(fromTables, currStep, currSteps);
                break;
            case SQLKeyword.HAVING:
                GenerateFromTablesHaving(fromTables, currStep, currSteps);
                break;
        }
    }

    private void GenerateFromTablesJoin(List<Table> fromTables, SQLDecompositionComponent currentStep,
        List<SQLDecompositionComponent> currSteps)
    {
        var fromClause = currentStep.GenerateFromClauseFromJoin();
        var withComponent = currSteps.FirstOrDefault(c => c.Keyword == SQLKeyword.WITH);
        var components = withComponent != null
            ? (IEnumerable<SQLDecompositionComponent>)[withComponent, fromClause]
            : [fromClause];
        var joiningTable = sqlExecutor.Execute(components).Result;
        joiningTable.Name = UtilRegex.ExtractTableNameFromJoin(currentStep.Clause);
        tocg.GenerateTableOriginOnColumnsFromTableName(joiningTable);
        fromTables.Add(joiningTable);
    }

    private void GenerateFromTablesHaving(List<Table> fromTables, SQLDecompositionComponent currStep,
        List<SQLDecompositionComponent> currSteps)
    {
        if(fromTables[0].Entries.Count == 0) return;
        
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
        var temp = currSteps.ToList()[..^1];
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

    public ExecutedStep GenerateToTable(SQLDecompositionComponent currStep,
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
            case SQLKeyword.JOIN:
            case SQLKeyword.INNER_JOIN:
            {
                var result = sqlExecutor.Execute(currSteps).Result;
                GenerateToTablesInnerJoin(fromTables, toTables, result);
                break;
            }
            case SQLKeyword.LEFT_JOIN:
            case SQLKeyword.LEFT_OUTER_JOIN:
            {
                var result = sqlExecutor.Execute(currSteps).Result;
                GenerateToTablesLeftJoin(fromTables, toTables, result);
                break;
            }
            case SQLKeyword.RIGHT_JOIN:
            case SQLKeyword.RIGHT_OUTER_JOIN:
            {
                var result = sqlExecutor.Execute(currSteps).Result;
                GenerateToTablesRightJoin(fromTables, toTables, result);
                break;
            }
            case SQLKeyword.FULL_JOIN:
            case SQLKeyword.FULL_OUTER_JOIN:
            {
                var  result = sqlExecutor.Execute(currSteps).Result;
                GenerateToTablesFullJoin(fromTables, toTables,  result);
                break;
            }
            default:
                toTables.Add(
                    sqlExecutor.Execute(currSteps).Result);
                break;
        }

        if (toTables.Count == 0)
        {
            if (currStep.Keyword.IsJoin())
            {
                var table = new Table
                {
                    ColumnNames = [],
                    Entries = []
                };
                table.ColumnNames.AddRange(fromTables[0].ColumnNames);
                table.ColumnNames.AddRange(fromTables[1].ColumnNames);
                table.ColumnsOriginalTableNames.AddRange(fromTables[0].ColumnsOriginalTableNames);
                table.ColumnsOriginalTableNames.AddRange(fromTables[1].ColumnsOriginalTableNames);
                toTables.Add(table);
            }
            else
            {
                var table = new Table
                {
                    ColumnNames = fromTables[0].ColumnNames.ToList(),
                    Entries = []
                };
                // table.ColumnsOriginalTableNames.AddRange(fromTables[0].ColumnsOriginalTableNames);
                toTables.Add(table);
            }
        }

        return new ExecutedStep
        {
            Step= currStep,
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
        var columnNamesToGroupBy = currentStep.Clause.Split(',', StringSplitOptions.TrimEntries);

        var groupByIndexes = tabel.IndexOfColumns(columnNamesToGroupBy).ToList();

        var groupedTables = tabel.Entries
            .GroupBy(e => new CompositeKey(groupByIndexes.Select(i => e.Values[i].RawValue)))
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
        if (aggregationResults.Entries.Count == 0)
        {
            var table = new Table
            {
                ColumnNames = fromTables[0].ColumnNames.ToList(), 
                Entries = []
            };
            table.ColumnsOriginalTableNames.AddRange(fromTables[0].ColumnsOriginalTableNames);
            toTables.Add(table);
            return;
        }

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

    private void GenerateToTablesInnerJoin(List<Table> fromTables, List<Table> toTables, Table resultTable)
    {
        var sourceTable = fromTables[0];
        var joiningTable = fromTables[1];
        var newResultTable = CreateEmptyResultTable(resultTable);
        
        foreach (var source in sourceTable.Entries)
        {
            AddMatchesForSource(source, joiningTable.Entries, resultTable, newResultTable);
        }

        toTables.Add(newResultTable);
    }
    
    private void GenerateToTablesLeftJoin(List<Table> fromTables, List<Table> toTables, Table resultTable)
    {
        var sourceTable = fromTables[0];
        var joiningTable = fromTables[1];
        var newResultTable = CreateEmptyResultTable(resultTable);

        var nullJoiningValues = CreateNullValues(joiningTable.Entries[0].Values.Count);
        
        var numberOfEntriesToGenerate = resultTable.Entries.Count;
        foreach (var source in sourceTable.Entries)
        {
            var foundMatch = AddMatchesForSource(source, joiningTable.Entries, resultTable, newResultTable);
            if (foundMatch) continue;

            var sourceWithNullForJoiningColumns = AppendValues(source, nullJoiningValues);
            
            RemoveExpectedEntry(resultTable, sourceWithNullForJoiningColumns);
            
            newResultTable.Entries.Add(sourceWithNullForJoiningColumns);
        }
        
        if (newResultTable.Entries.Count != numberOfEntriesToGenerate)
            throw new ArgumentException($"Didn't generate all the required entries, missing :" +
                                        $"{numberOfEntriesToGenerate-newResultTable.Entries.Count}");
        
        toTables.Add(newResultTable);
    }
    
    private void GenerateToTablesRightJoin(List<Table> fromTables, List<Table> toTables, Table resultTable)
    {
        var sourceTable = fromTables[0];
        var joiningTable = fromTables[1];
        var newResultTable = CreateEmptyResultTable(resultTable);

        var nullSourceValues = CreateNullValues(sourceTable.Entries.First().Values.Count);
        
        var numberOfEntriesToGenerate = resultTable.Entries.Count;
        foreach (var joining in joiningTable.Entries)
        {
            var foundMatch = AddMatchesForJoining(joining, sourceTable.Entries, resultTable, newResultTable);
            if (foundMatch) continue;

            var joiningWithNullForSourceColumns = PrependValues(joining, nullSourceValues);
            
            RemoveExpectedEntry(resultTable, joiningWithNullForSourceColumns);
            
            newResultTable.Entries.Add(joiningWithNullForSourceColumns);
        }
        if (newResultTable.Entries.Count != numberOfEntriesToGenerate)
            throw new ArgumentException($"Didn't generate all the required entries, missing :" +
                                        $"{numberOfEntriesToGenerate-newResultTable.Entries.Count}");
        
        toTables.Add(newResultTable);
    }
    
    private void GenerateToTablesFullJoin(List<Table> fromTables, List<Table> toTables, Table resultTable)
    {
        var sourceTable = fromTables[0];
        var joiningTable = fromTables[1];
        var newResultTable = CreateEmptyResultTable(resultTable);
        
        var nullJoiningValues = CreateNullValues(joiningTable.Entries.First().Values.Count);
        var nullSourceValues = CreateNullValues(sourceTable.Entries.First().Values.Count);

        var matchedJoiningEntries = new HashSet<TableEntry>();
        var numberOfEntriesToGenerate = resultTable.Entries.Count;
        
        foreach (var source in sourceTable.Entries)
        {
            var foundMatch = AddMatchesForSource(
                source, joiningTable.Entries, resultTable, newResultTable, matchedJoiningEntries);
            if (foundMatch) continue;

            var sourceWithNullForJoiningColumns = AppendValues(source, nullJoiningValues);
            
            RemoveExpectedEntry(resultTable, sourceWithNullForJoiningColumns);
            
            newResultTable.Entries.Add(sourceWithNullForJoiningColumns);
        }

        foreach (var joining in joiningTable.Entries.Where(j => !matchedJoiningEntries.Contains(j)))
        {
            var foundMatch = AddMatchesForJoining(joining, sourceTable.Entries, resultTable, newResultTable);
            if (foundMatch) continue;

            var joiningWithNullForSourceColumns = PrependValues(joining, nullSourceValues);
            
            RemoveExpectedEntry(resultTable, joiningWithNullForSourceColumns);
            
            newResultTable.Entries.Add(joiningWithNullForSourceColumns);
        }
        
        if (resultTable.Entries.Count != 0)
            throw new ArgumentException($"Didn't generate all the required entries, missing :" +
                                        $"{numberOfEntriesToGenerate-newResultTable.Entries.Count}");
        
        toTables.Add(newResultTable);
    }

    private static Table CreateEmptyResultTable(Table resultTable)
    {
        var newResultTable = resultTable.DeepClone();
        newResultTable.Entries.Clear();
        return newResultTable;
    }

    private static List<TableValue> CreateNullValues(int count)
    {
        return Enumerable.Range(0, count)
            .Select(_ => new TableValue
            {
                Value = "NULL",
                RawValue = null
            })
            .ToList();
    }

    private static TableEntry AppendValues(TableEntry entry, IEnumerable<TableValue> valuesToAppend)
    {
        var copy = entry.DeepClone();
        copy.Values.AddRange(valuesToAppend.Select(value => value.DeepClone()));
        return copy;
    }

    private static TableEntry PrependValues(TableEntry entry, IEnumerable<TableValue> valuesToPrepend)
    {
        var copy = entry.DeepClone();
        copy.Values.InsertRange(0, valuesToPrepend.Select(value => value.DeepClone()));
        return copy;
    }

    private static void RemoveExpectedEntry(Table resultTable, TableEntry expectedEntry)
    {
        var result = resultTable.Entries.FirstOrDefault(entry => entry.Equals(expectedEntry));
        if (result == null)
            throw new ArgumentException("Supposed to find null entry, but didn't");

        resultTable.Entries.Remove(result);
    }

    private static bool AddMatchesForSource(
        TableEntry source,
        IEnumerable<TableEntry> joiningEntries,
        Table remainingResults,
        Table outputTable,
        HashSet<TableEntry>? matchedJoiningEntries = null)
    {
        var foundMatch = false;

        foreach (var joining in joiningEntries)
        {
            var result = remainingResults.Entries.FirstOrDefault(r =>
                source.AreJoinEquivalentToResult(joining, r));

            if (result == null) continue;

            foundMatch = true;
            outputTable.Entries.Add(result);
            remainingResults.Entries.Remove(result);
            matchedJoiningEntries?.Add(joining);
        }

        return foundMatch;
    }

    private static bool AddMatchesForJoining(
        TableEntry joining,
        IEnumerable<TableEntry> sourceEntries,
        Table remainingResults,
        Table outputTable)
    {
        var foundMatch = false;

        foreach (var source in sourceEntries)
        {
            var result = remainingResults.Entries.FirstOrDefault(r =>
                source.AreJoinEquivalentToResult(joining, r));

            if (result == null) continue;

            foundMatch = true;
            outputTable.Entries.Add(result);
            remainingResults.Entries.Remove(result);
        }

        return foundMatch;
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
            return TableValue.CompareRawValues(left, right);
        }
    }
}
