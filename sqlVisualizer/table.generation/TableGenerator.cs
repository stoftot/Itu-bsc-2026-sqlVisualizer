using System.Text.RegularExpressions;
using commonDataModels;
using commonDataModels.Models;
using tableGeneration.Models;

namespace tableGeneration;

internal class TableGenerator(SQLExecutorWrapper sqlExecutor, TableOriginColumnsGenerator tocg)
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
        if (fromTables[0].Rows.Count == 0) return;
        
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

        if (fromTables.Count != aggregationResults.Rows.Count)
            throw new ArgumentException("The number of aggregation results must match the number of grouped tables(\n" +
                                        $"aggregations: {aggregationResults.Rows.Count}\n" +
                                        $"grouped by tables: {fromTables.Count}\n)");

        //Add aggregationResults to the respective group by tables
        for (int i = 0; i < fromTables.Count; i++)
        {
            var fromTable = fromTables[i];
            var aggregationResult = aggregationResults[i];

            foreach (var (name, tableCell) in aggregationResults.ColumnNames.Zip(aggregationResult.Cells))
            {
                fromTable.Aggregations.Add(new Aggregation()
                {
                    Name = name,
                    Value = tableCell.Value
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
                    Rows = []
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
                    Rows = []
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

        var groupedTables = tabel.Rows
            .GroupBy(row => new CompositeKey(groupByIndexes.Select(i => row[i].RawValue)))
            .OrderBy(g => g.Key, CompositeKeyComparer.Instance)
            .Select(g => new Table
            {
                ColumnNames = tabel.ColumnNames.ToList(),
                Rows = g.ToList()
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
        if (aggregationResults.Rows.Count == 0)
        {
            var table = new Table
            {
                ColumnNames = fromTables[0].ColumnNames.ToList(), 
                Rows = []
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
            var aggregationValues = aggregationResults[aggregationIndex].Cells
                .Select(cell => cell.Value);
            // var fromValues = fromTable.Rows[0].Cells.GetRange(startOfAggregation, fromTable.Rows[0].Cells.Count - startOfAggregation);
            if (fromTable.Aggregations.Count == 0) throw new ArgumentException("Aggregations cannot be empty");
            var fromValues = fromTable.Aggregations
                .Select(a => a.Value);

            if (aggregationValues.SequenceEqual(fromValues))
            {
                toTables.Add(fromTable.DeepClone());
                aggregationIndex++;
                if (aggregationIndex >= aggregationResults.Rows.Count) break;
            }
        }
    }

    private void GenerateToTablesInnerJoin(List<Table> fromTables, List<Table> toTables, Table resultTable)
    {
        var sourceTable = fromTables[0];
        var joiningTable = fromTables[1];
        var newResultTable = CreateEmptyResultTable(resultTable);
        
        foreach (var source in sourceTable.Rows)
        {
            AddMatchesForSource(source, joiningTable.Rows, resultTable, newResultTable);
        }

        toTables.Add(newResultTable);
    }
    
    private void GenerateToTablesLeftJoin(List<Table> fromTables, List<Table> toTables, Table resultTable)
    {
        var sourceTable = fromTables[0];
        var joiningTable = fromTables[1];
        var newResultTable = CreateEmptyResultTable(resultTable);

        var nullJoiningCells = CreateNullCells(joiningTable[0].Cells.Count);
        
        var numberOfEntriesToGenerate = resultTable.Rows.Count;
        foreach (var source in sourceTable.Rows)
        {
            var foundMatch = AddMatchesForSource(source, joiningTable.Rows, resultTable, newResultTable);
            if (foundMatch) continue;

            var sourceWithNullForJoiningColumns = AppendCells(source, nullJoiningCells);
            
            RemoveExpectedEntry(resultTable, sourceWithNullForJoiningColumns);
            
            newResultTable.Rows.Add(sourceWithNullForJoiningColumns);
        }
        
        if (newResultTable.Rows.Count != numberOfEntriesToGenerate)
            throw new ArgumentException($"Didn't generate all the required entries, missing :" +
                                        $"{numberOfEntriesToGenerate-newResultTable.Rows.Count}");
        
        toTables.Add(newResultTable);
    }
    
    private void GenerateToTablesRightJoin(List<Table> fromTables, List<Table> toTables, Table resultTable)
    {
        var sourceTable = fromTables[0];
        var joiningTable = fromTables[1];
        var newResultTable = CreateEmptyResultTable(resultTable);

        var nullSourceCells = CreateNullCells(sourceTable[0].Cells.Count);
        
        var numberOfEntriesToGenerate = resultTable.Rows.Count;
        foreach (var joining in joiningTable.Rows)
        {
            var foundMatch = AddMatchesForJoining(joining, sourceTable.Rows, resultTable, newResultTable);
            if (foundMatch) continue;

            var joiningWithNullForSourceColumns = PrependCells(joining, nullSourceCells);
            
            RemoveExpectedEntry(resultTable, joiningWithNullForSourceColumns);
            
            newResultTable.Rows.Add(joiningWithNullForSourceColumns);
        }
        if (newResultTable.Rows.Count != numberOfEntriesToGenerate)
            throw new ArgumentException($"Didn't generate all the required entries, missing :" +
                                        $"{numberOfEntriesToGenerate-newResultTable.Rows.Count}");
        
        toTables.Add(newResultTable);
    }
    
    private void GenerateToTablesFullJoin(List<Table> fromTables, List<Table> toTables, Table resultTable)
    {
        var sourceTable = fromTables[0];
        var joiningTable = fromTables[1];
        var newResultTable = CreateEmptyResultTable(resultTable);
        
        var nullJoiningCells = CreateNullCells(joiningTable[0].Cells.Count);
        var nullSourceCells = CreateNullCells(sourceTable[0].Cells.Count);

        var matchedJoiningRows = new HashSet<TableRow>();
        var numberOfEntriesToGenerate = resultTable.Rows.Count;
        
        foreach (var source in sourceTable.Rows)
        {
            var foundMatch = AddMatchesForSource(
                source, joiningTable.Rows, resultTable, newResultTable, matchedJoiningRows);
            if (foundMatch) continue;

            var sourceWithNullForJoiningColumns = AppendCells(source, nullJoiningCells);
            
            RemoveExpectedEntry(resultTable, sourceWithNullForJoiningColumns);
            
            newResultTable.Rows.Add(sourceWithNullForJoiningColumns);
        }

        foreach (var joining in joiningTable.Rows.Where(row => !matchedJoiningRows.Contains(row)))
        {
            var foundMatch = AddMatchesForJoining(joining, sourceTable.Rows, resultTable, newResultTable);
            if (foundMatch) continue;

            var joiningWithNullForSourceColumns = PrependCells(joining, nullSourceCells);
            
            RemoveExpectedEntry(resultTable, joiningWithNullForSourceColumns);
            
            newResultTable.Rows.Add(joiningWithNullForSourceColumns);
        }
        
        if (resultTable.Rows.Count != 0)
            throw new ArgumentException($"Didn't generate all the required entries, missing :" +
                                        $"{numberOfEntriesToGenerate-newResultTable.Rows.Count}");
        
        toTables.Add(newResultTable);
    }

    private static Table CreateEmptyResultTable(Table resultTable)
    {
        var newResultTable = resultTable.DeepClone();
        newResultTable.Rows.Clear();
        return newResultTable;
    }

    private static List<TableCell> CreateNullCells(int count)
    {
        return Enumerable.Range(0, count)
            .Select(_ => new TableCell
            {
                Value = "NULL",
                RawValue = null
            })
            .ToList();
    }

    private static TableRow AppendCells(TableRow row, IEnumerable<TableCell> cellsToAppend)
    {
        var copy = row.DeepClone();
        copy.Cells.AddRange(cellsToAppend.Select(cell => cell.DeepClone()));
        return copy;
    }

    private static TableRow PrependCells(TableRow row, IEnumerable<TableCell> cellsToPrepend)
    {
        var copy = row.DeepClone();
        copy.Cells.InsertRange(0, cellsToPrepend.Select(cell => cell.DeepClone()));
        return copy;
    }

    private static void RemoveExpectedEntry(Table resultTable, TableRow expectedRow)
    {
        var result = resultTable.Rows.FirstOrDefault(row => row.Equals(expectedRow));
        if (result == null)
            throw new ArgumentException("Supposed to find null entry, but didn't");

        resultTable.Rows.Remove(result);
    }

    private static bool AddMatchesForSource(
        TableRow source,
        IEnumerable<TableRow> joiningRows,
        Table remainingResults,
        Table outputTable,
        HashSet<TableRow>? matchedJoiningRows = null)
    {
        var foundMatch = false;

        foreach (var joining in joiningRows)
        {
            var result = remainingResults.Rows.FirstOrDefault(r =>
                source.AreJoinEquivalentToResult(joining, r));

            if (result == null) continue;

            foundMatch = true;
            outputTable.Rows.Add(result);
            remainingResults.Rows.Remove(result);
            matchedJoiningRows?.Add(joining);
        }

        return foundMatch;
    }

    private static bool AddMatchesForJoining(
        TableRow joining,
        IEnumerable<TableRow> sourceRows,
        Table remainingResults,
        Table outputTable)
    {
        var foundMatch = false;

        foreach (var source in sourceRows)
        {
            var result = remainingResults.Rows.FirstOrDefault(r =>
                source.AreJoinEquivalentToResult(joining, r));

            if (result == null) continue;

            foundMatch = true;
            outputTable.Rows.Add(result);
            remainingResults.Rows.Remove(result);
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
            return TableCell.CompareRawValues(left, right);
        }
    }
}
