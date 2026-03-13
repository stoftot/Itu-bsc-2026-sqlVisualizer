using System.Text.RegularExpressions;
using visualizer.Exstensions;
using visualizer.Models;
using visualizer.Utility;

namespace visualizer.Repositories.AnimationClasses;

public static class SelectAnimationGenerator
{
    record Partition(List<int> RowIndices, List<int> Values);
    
    private static TableVisualModifier tvm = new();

    public static Animation Generate(List<Table> fromTables, Table toTable,
        SQLDecompositionComponent action)
    {
        var steps = new List<Action>();

        //TODO: Figure out how we want to handle select *
        if (action.Clause.Trim().Equals("*"))
            return new Animation(steps);

        steps.Add(tvm.HideTablesCellBased([toTable]));
        
        //TODO: regex dosen't support window functions with end parentheses inside the over, like "over (..)...)" 
        var windowFunctionMatch = Regex.Match(action.Clause, @"\s*[^,]+?\bover\s*[^)]+\)[^,]+", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        List<string> columns;
        
        if (windowFunctionMatch.Success)
        {
            var windowFunction = windowFunctionMatch.Groups[0].Value;
            var clauseMinusWindow = Regex.Replace(action.Clause, 
                @"\s*[^,]+?\bover\s*[^)]+\)[^,]+", "()");
            
            columns = clauseMinusWindow.Split(',').Select(c => c.Trim()).ToList();
            for (int i = 0; i < columns.Count; i++)
            {
                if (!columns[i].Equals("()")) continue;
                
                columns[i] = windowFunction;
                break;
            }
        }
        else
        {
            columns = action.Clause.Split(',').Select(c => c.Trim()).ToList();
        }

        var toColumnIndex = -1;
        foreach (var column in columns)
        {
            toColumnIndex++;
            // Handel window functions
            if (column.ToLower().Contains(" over "))
            {
                HandleWindowFunction(fromTables[0], toTable, column, toColumnIndex, steps);
                continue;
            }
            
            if (column.Contains('('))
            {
                HandleAggregateColumn(fromTables, toTable, column, toColumnIndex, steps);
                continue;
            }
            
            if (fromTables.Count == 1)
            {
                Action FromAnimationGenerator(int i) => tvm.GenerateToggleHighlightColumn(fromTables[0], i);

                HandleNormalSelect(fromTables, toTable, column, toColumnIndex, steps, FromAnimationGenerator);
            }
            else
            {
                Action FromAnimationGenerator(int i) =>
                    fromTables.Select(table => tvm.GenerateToggleHighlightCell(table, 0, i))
                        .ToList()
                        .ToOneAction();

                HandleNormalSelect(fromTables, toTable, column, toColumnIndex, steps, FromAnimationGenerator);
            }
        }

        return new Animation(steps);
    }

    private static void HandleAggregateColumn(List<Table> fromTables, Table toTable,
        string column, int toColumnIndex, List<Action> steps)
    {
        var parts = column.Split('(', 2);

        if (!Enum.TryParse(parts[0].Trim().ToUpperInvariant(), out SQLAggregateFunctionsKeyword keyword))
            throw new ArgumentException($"the aggregate function \"{parts[0].Trim()}\" is not supported");

        switch (keyword)
        {
            case SQLAggregateFunctionsKeyword.COUNT:
                HandleCountAggregate(fromTables, toTable, parts[1].Replace(')', ' ').Trim(), toColumnIndex, steps);
                break;
            case SQLAggregateFunctionsKeyword.SUM:
            case SQLAggregateFunctionsKeyword.AVG:
                HandleSumAndAvgAggregate(fromTables, toTable, parts[1].Replace(')', ' ').Trim(), toColumnIndex, steps);
                break;
            case SQLAggregateFunctionsKeyword.MIN:
            case SQLAggregateFunctionsKeyword.MAX:
                HandleMinAndMaxAggregate(fromTables, toTable, parts[1].Replace(')', ' ').Trim(), toColumnIndex, steps);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private static void HandleCountAggregate(List<Table> fromTables, Table toTable,
        string parameter, int toColumnIndex, List<Action> steps)
    {
        if (string.IsNullOrEmpty(parameter))
        {
            int i = 0;
            foreach (var table in fromTables)
            {
                steps.Add(tvm.CombineActions(
                [
                    tvm.GenerateToggleHighlightColumn(table, 0),
                    tvm.GenerateToggleVisibleCell(toTable, i, toColumnIndex),
                    tvm.GenerateToggleHighlightCell(toTable, i, toColumnIndex)
                ]));

                steps.Add(tvm.CombineActions(
                [
                    tvm.GenerateToggleHighlightColumn(table, 0),
                    tvm.GenerateToggleHighlightCell(toTable, i++, toColumnIndex)
                ]));
            }
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    private static void HandleSumAndAvgAggregate(List<Table> fromTables, Table toTable,
        string parameter, int toColumnIndex, List<Action> steps)
    {
        //TODO: right now only supports summing and avg on single columns
        var fromColumnIndex = 0;
        try
        {
             fromColumnIndex = fromTables[0].IndexOfColumn(parameter);
        }
        catch (ArgumentException e)
        {
            throw new NotSupportedException("The column wasn't found, and sum and avg only supports summing specific columns", e);
        }

        int i = 0;
        foreach (var table in fromTables)
        {
            steps.Add(tvm.CombineActions(
            [
                tvm.GenerateToggleHighlightColumn(table, fromColumnIndex),
                tvm.GenerateToggleVisibleCell(toTable, i, toColumnIndex),
                tvm.GenerateToggleHighlightCell(toTable, i, toColumnIndex)
            ]));

            steps.Add(tvm.CombineActions(
            [
                tvm.GenerateToggleHighlightColumn(table, fromColumnIndex),
                tvm.GenerateToggleHighlightCell(toTable, i++, toColumnIndex)
            ]));
        }
    }
    
    private static void HandleMinAndMaxAggregate(List<Table> fromTables, Table toTable,
        string parameter, int toColumnIndex, List<Action> steps)
    {
        //TODO: right now only supports min and max on single columns
        var fromColumnIndex = 0;
        try
        {
            fromColumnIndex = fromTables[0].IndexOfColumn(parameter);
        }
        catch (ArgumentException e)
        {
            throw new NotSupportedException("The column wasn't found, and min and max only supports summing specific columns", e);
        }

        int i = 0;
        foreach (var table in fromTables)
        {
            steps.Add(tvm.CombineActions(
            [
                tvm.GenerateToggleHighlightColumn(table, fromColumnIndex),
                tvm.GenerateToggleVisibleCell(toTable, i, toColumnIndex),
                tvm.GenerateToggleHighlightCell(toTable, i, toColumnIndex)
            ]));

            steps.Add(tvm.CombineActions(
            [
                tvm.GenerateToggleHighlightColumn(table, fromColumnIndex),
                tvm.GenerateToggleHighlightCell(toTable, i++, toColumnIndex)
            ]));
        }
    }
    
    private static void HandleNormalSelect(List<Table> fromTables, Table toTable,
        string column, int columnIndex, List<Action> steps,
        Func<int, Action> generateFromAnimation)
    {
        var parts = column.Split('.', 2);
        var tableName = parts.Length == 2 ? parts[0] : null;
        var columnName = parts.Length == 2 ? parts[1] : parts[0];

        for (int i = 0; i < fromTables[0].ColumnNames.Count; i++)
        {
            var fromAnimation = generateFromAnimation(i);

            if (fromTables[0].ColumnNames[i].Equals(columnName, StringComparison.InvariantCultureIgnoreCase) &&
                (tableName == null ||
                 fromTables[0].ColumnsOriginalTableNames[i]
                     .Equals(tableName, StringComparison.InvariantCultureIgnoreCase)))
            {
                steps.Add(tvm.CombineActions(
                [
                    fromAnimation,
                    tvm.GenerateToggleVisibleColumn(toTable, columnIndex),
                    tvm.GenerateToggleHighlightColumn(toTable, columnIndex)
                ]));

                steps.Add(tvm.CombineActions(
                [
                    fromAnimation,
                    tvm.GenerateToggleHighlightColumn(toTable, columnIndex)
                ]));
                return;
            }

            steps.Add(fromAnimation);
            steps.Add(fromAnimation);
        }
    }

    private static void HandleWindowFunction(Table fromTable, Table toTable,
        string column, int columnIndex, List<Action> steps)
    {
        WindowFunction windowFunction = WindowFunction.FromString(column);
        windowFunction.Print();
        
        switch (windowFunction.Function.ToLower())
        {
            case "sum":
                HandleWindowFunctionSum(fromTable, toTable, windowFunction, columnIndex, steps);
                break;
            default:
                throw new NotImplementedException($"the window function \"{windowFunction.Function}\" is not supported");
        }
    }
    
    private static void HandleWindowFunctionSum(Table fromTable, Table toTable, WindowFunction windowFunction, 
        int columnIndex, List<Action> steps)
    {
        Table fromTableWithRowIndex = fromTable.DeepClone().AppendRowIndex();

        if (windowFunction.Orders.Count > 0)
        {
            var orders = windowFunction.Orders;
            orders.Reverse();
            foreach (var order in orders)
                fromTableWithRowIndex = fromTableWithRowIndex.OrderBy(order.ColumnName, order.IsAscending);
        }

        if (windowFunction.PartitionNames.Count > 0)
        {
            List<int> partitionIndices = windowFunction.PartitionNames.Select(p => fromTableWithRowIndex.IndexOfColumn(p)).ToList();
            var t = fromTableWithRowIndex.Entries
                .GroupBy(e => string.Join(", ", partitionIndices.Select(i => e.Values[i].Value)))
                .OrderBy(g => g.Key)
                .ToList();
            Console.WriteLine("Partitions:" +  string.Join(", ", t.Select(g => g.Key)));
            Console.WriteLine("Partitions entries:" +  string.Join(" | ", t.Select(g => string.Join(", ", g.Select(b => b.Values[4].Value)))));
            Console.WriteLine("Partitions entries:" +  string.Join(", ", t.Select(g => string.Join(", ", g.Select(b => b.Values[1].Value)))));
            Console.WriteLine("Partitions entries:" +  string.Join(", ", t.Select(g => string.Join(", ", g.Select(b => b.Values[2].Value)))));
        }
    }
    
    private static void HandleWindowFunctionSumNoPartition(List<Table> fromTables, Table toTable,
        int columnIndex, List<Action> steps)
    {
        int fromTableColumnIndex = fromTables[0].IndexOfColumn("price");
        
        steps.Add(tvm.CombineActions(
        [
            tvm.GenerateToggleHighlightCell(fromTables[0], 0, fromTableColumnIndex),
            tvm.GenerateToggleVisibleCell(toTable, 0, columnIndex),
            tvm.GenerateToggleHighlightCell(toTable, 0, columnIndex)
        ]));
        
        for (int i = 1; i < toTable.Entries.Count; i++)
        {
            steps.Add(tvm.CombineActions(
            [
                tvm.GenerateToggleHighlightCell(toTable, i-1, columnIndex),
                tvm.GenerateToggleHighlightCell(fromTables[0], i, fromTableColumnIndex),
                tvm.GenerateToggleVisibleCell(toTable, i, columnIndex),
                tvm.GenerateToggleHighlightCell(toTable, i, columnIndex)
            ]));
        }
        
        steps.Add(tvm.CombineActions(
        [
            tvm.GenerateToggleHighlightCell(toTable, toTable.Entries.Count-1, columnIndex),
            tvm.GenerateToggleHighlightColumn(fromTables[0], fromTableColumnIndex)
        ]));
    }
    
    private static void HandleWindowFunctionSumWithPartition(List<Table> fromTables, Table toTable, int columnIndex, 
        List<Action> steps, string functionColumn, string? partitionColumn, string? orderColumn)
    {
        Table fromTableWithRowIndex = fromTables[0].DeepClone().AppendRowIndex();
        
        if (orderColumn != null)
            fromTableWithRowIndex = fromTableWithRowIndex.OrderBy(orderColumn, true);
        
        // Find the column indices for partitioning in the source table
        var partitionColumnIndices = new List<int>();
        partitionColumnIndices.Add(fromTables[0].IndexOfColumn(partitionColumn));
        
        int sumColumnIndex = fromTables[0].IndexOfColumn(functionColumn);
        
        List<Partition> partitionsFromSource = [];
        
        // Group rows by partition values in source table
        var partitions = new Dictionary<string, int>();
        for (int i = 0; i < fromTableWithRowIndex.Entries.Count; i++)
        {
            //Console.WriteLine(orderedFromTable.Entries[i].Values[4].Value);
            string partitionKey = GetPartitionKey(fromTableWithRowIndex, i, partitionColumnIndices);
            if (!partitions.ContainsKey(partitionKey))
            {
                partitions[partitionKey] = partitionsFromSource.Count;
                partitionsFromSource.Add(new Partition([], []));
            }
            partitionsFromSource[partitions[partitionKey]].RowIndices.Add(int.Parse(fromTableWithRowIndex.Entries[i].Values[fromTableWithRowIndex.ColumnNames.Count-1].Value));
            partitionsFromSource[partitions[partitionKey]].Values.Add(int.Parse(fromTableWithRowIndex.Entries[i].Values[sumColumnIndex].Value));
        }
        
        // Calculate cumulative sums for each source partition
        foreach (var partition in partitionsFromSource)
        {
            int sum = 0;
            for (int i = 0; i < partition.Values.Count; i++)
            {
                sum += partition.Values[i];
                partition.Values[i] = sum;
            }
        }
        
        // Detect partition boundaries from result table
        var partitionsFromResult = GetPartitionOrderFromResultTable(toTable, columnIndex);

        var (matchedSourcePartitions, matchedResultPartitions) = MatchPartitions(partitionsFromSource, partitionsFromResult);

        // Animate each partition in the order they appear in the result table
        for (int partitionIndex = 0; partitionIndex < matchedResultPartitions.Count; partitionIndex++)
        {
            var sourceRowIndices = matchedSourcePartitions[partitionIndex];
            var resultRowIndices = matchedResultPartitions[partitionIndex];

            if (sourceRowIndices.Count != resultRowIndices.Count)
            {
                throw new Exception(
                    $"Partition size mismatch. Source rows: {sourceRowIndices.Count}, result rows: {resultRowIndices.Count}");
            }

            // First, highlight all rows in this partition in the source table
            var partitionRowActions = sourceRowIndices
                .Select(rowIdx => tvm.GenerateToggleHighlightRow(fromTables[0].Entries[rowIdx]))
                .ToList();
            steps.Add(tvm.CombineActions(partitionRowActions));

            // Then animate the sum calculation for this partition
            for (int i = 0; i < sourceRowIndices.Count; i++)
            {
                var sourceRowIdx = sourceRowIndices[i];
                var resultRowIdx = resultRowIndices[i];

                tvm.ChangeHighlightColourCell(fromTables[0], sourceRowIdx, sumColumnIndex, UtilColor.SecondaryHighlightColor);
                steps.Add(tvm.CombineActions(
                [
                    tvm.GenerateToggleHighlightCell(fromTables[0], sourceRowIdx, sumColumnIndex),
                    tvm.GenerateToggleVisibleCell(toTable, resultRowIdx, columnIndex),
                    tvm.GenerateToggleHighlightCell(toTable, resultRowIdx, columnIndex)
                ]));
            }

            // Unhighlight
            var unhighlightSourceRows = sourceRowIndices
                .Select(rowIdx => tvm.GenerateToggleHighlightRow(fromTables[0].Entries[rowIdx]))
                .ToList();
            
            var unhighlightSourceCells = sourceRowIndices
                .Select(rowIdx => tvm.GenerateToggleHighlightCell(fromTables[0], rowIdx, sumColumnIndex))
                .ToList();
            
            var unhighlightResultCells = resultRowIndices
                .Select(rowIdx => tvm.GenerateToggleHighlightCell(toTable, rowIdx, columnIndex))
                .ToList();
            
            var unhighlightActions = unhighlightSourceRows.Concat(unhighlightSourceCells).Concat(unhighlightResultCells).ToList();
            
            steps.Add(tvm.CombineActions(unhighlightActions));
        }
    }
    
    private static List<Partition> GetPartitionOrderFromResultTable(Table resultTable, int columnIndex)
    {
        // Detect partition boundaries by analyzing where the cumulative sum resets or repeats
        var partitions = new List<Partition>();
        
        if (resultTable.Entries.Count == 0)
            return partitions;
        
        var currentPartition = new List<int>();
        var currentValues = new List<int>();
        decimal? previousValue = null;
        
        for (int i = 0; i < resultTable.Entries.Count; i++)
        {
            if (decimal.TryParse(resultTable.Entries[i].Values[columnIndex].Value, out decimal currentValue))
            {
                // If the current value is less than or equal to the previous value, 
                // it likely indicates a new partition (sum reset)
                if (previousValue.HasValue && currentValue <= previousValue.Value)
                {
                    // Save the current partition
                    if (currentPartition.Count > 0)
                    {
                        partitions.Add(new Partition(new List<int>(currentPartition), new List<int>(currentValues)));
                        currentPartition.Clear();
                        currentValues.Clear();
                    }
                }
                
                currentPartition.Add(i);
                currentValues.Add((int)currentValue);
                previousValue = currentValue;
            }
        }
        
        // Add the last partition
        if (currentPartition.Count > 0)
        {
            partitions.Add(new Partition(currentPartition, currentValues));
        }
        
        return partitions;
    }
    
    private static string GetPartitionKey(Table table, int rowIndex, List<int> partitionColumnIndices)
    {
        var keyParts = partitionColumnIndices.Select(colIdx => table.Entries[rowIndex].Values[colIdx].Value).ToList();
        return string.Join("|", keyParts);
    }
    
    private static string BuildPartitionSignature(List<int> values)
    {
        return string.Join("|", values);
    }

    // Takes two lists of partitions (one from source table, one from result table) and matches them by cumulative values.
    // Returns two aligned lists where index i is the same partition in source/result row-index space.
    private static (List<List<int>>, List<List<int>>) MatchPartitions(List<Partition> sourcePartitions, List<Partition> resultPartitions)
    {
        Console.WriteLine("sourcePartitions: " + string.Join(" || ", sourcePartitions.Select(p => $"[{string.Join(",", p.Values)}]")));
        Console.WriteLine("resultPartitions: " + string.Join(" || ", resultPartitions.Select(p => $"[{string.Join(",", p.Values)}]")));
        
        var matchedSourcePartitions = new List<List<int>>();
        var matchedResultPartitions = new List<List<int>>();

        var sourceBySignature = new Dictionary<string, Queue<Partition>>();
        foreach (var sourcePartition in sourcePartitions)
        {
            var signature = BuildPartitionSignature(sourcePartition.Values);
            if (!sourceBySignature.TryGetValue(signature, out var queue))
            {
                queue = new Queue<Partition>();
                sourceBySignature[signature] = queue;
            }

            queue.Enqueue(sourcePartition);
        }

        foreach (var resultPartition in resultPartitions)
        {
            var signature = BuildPartitionSignature(resultPartition.Values);
            if (!sourceBySignature.TryGetValue(signature, out var queue) || queue.Count == 0)
            {
                throw new Exception(
                    "Could not find matching source partition for result partition with values: " +
                    string.Join(", ", resultPartition.Values));
            }

            var matchingSourcePartition = queue.Dequeue();
            matchedSourcePartitions.Add(matchingSourcePartition.RowIndices);
            matchedResultPartitions.Add(resultPartition.RowIndices);
        }

        return (matchedSourcePartitions, matchedResultPartitions);
    }
}