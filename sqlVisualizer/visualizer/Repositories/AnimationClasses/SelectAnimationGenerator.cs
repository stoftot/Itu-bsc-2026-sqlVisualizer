using visualizer.Exstensions;
using visualizer.Models;

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

        var columns = action.Clause.Split(',').Select(c => c.Trim()).ToList();

        var columnIndex = 0;
        foreach (var column in columns)
        {
            // Handel window functions
            if (column.Contains(" over "))
            {
                HandleWindowFunction(fromTables, toTable, column, columnIndex, steps);
                continue;
            }
            
            if (column.Contains('('))
            {
                if (fromTables.Count == 1)
                    throw new NotImplementedException();
                else
                    HandleAggregateColumnMultiTables(fromTables, toTable, column, columnIndex, steps);
            }
            else
            {
                if (fromTables.Count == 1)
                {
                    Action FromAnimationGenerator(int i) => tvm.GenerateToggleHighlightColumn(fromTables[0], i);

                    HandleNormalSelect(fromTables, toTable, column, columnIndex, steps, FromAnimationGenerator);
                }
                else
                {
                    Action FromAnimationGenerator(int i) =>
                        fromTables.Select(table => tvm.GenerateToggleHighlightCell(table, 0, i))
                            .ToList()
                            .ToOneAction();

                    HandleNormalSelect(fromTables, toTable, column, columnIndex, steps, FromAnimationGenerator);
                }
            }

            columnIndex++;
        }

        return new Animation(steps);
    }

    private static void HandleAggregateColumnMultiTables(List<Table> fromTables, Table toTable,
        string column, int columnIndex, List<Action> steps)
    {
        var parts = column.Split('(', 2);

        if (!Enum.TryParse(parts[0].Trim().ToUpperInvariant(), out SQLAggregateFunctionsKeyword keyword))
            throw new ArgumentException($"the aggregate function \"{parts[0].Trim()}\" is not supported");

        switch (keyword)
        {
            case SQLAggregateFunctionsKeyword.COUNT:
                HandleCountAggregateMultipleTables(fromTables, toTable, parts[1].Replace(')', ' ').Trim(), columnIndex,
                    steps);
                break;
        }
    }

    private static void HandleCountAggregateMultipleTables(List<Table> fromTables, Table toTable,
        string parameter, int columnIndex, List<Action> steps)
    {
        if (string.IsNullOrEmpty(parameter))
        {
            int i = 0;
            foreach (var table in fromTables)
            {
                steps.Add(tvm.CombineActions(
                [
                    tvm.GenerateToggleHighlightRows(table.Entries),
                    tvm.GenerateToggleVisibleCell(toTable, i, columnIndex),
                    tvm.GenerateToggleHighlightCell(toTable, i, columnIndex)
                ]));

                steps.Add(tvm.CombineActions(
                [
                    tvm.GenerateToggleHighlightRows(table.Entries),
                    tvm.GenerateToggleHighlightCell(toTable, i++, columnIndex)
                ]));
            }
        }
        else
        {
            throw new NotImplementedException();
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

    private static void HandleWindowFunction(List<Table> fromTables, Table toTable,
        string column, int columnIndex, List<Action> steps)
    {
        string function = column.Split(" over ")[0];
        string window = column.Split(" over ")[1];
        
        Console.WriteLine("function: " + function);
        Console.WriteLine("window: " + window);

        if (function.Contains("sum(", StringComparison.InvariantCultureIgnoreCase))
        {
            HandleWindowFunctionSum(fromTables, toTable, column, columnIndex, steps);
            return;
        }
            
        throw new NotImplementedException($"the window function \"{function}\" is not supported");
    }
    
    private static void HandleWindowFunctionSum(List<Table> fromTables, Table toTable,
        string column, int columnIndex, List<Action> steps)
    {
        string window = column.Split(" over ")[1];
        
        // Check if there's a PARTITION BY clause
        if (window.Contains("partition by", StringComparison.InvariantCultureIgnoreCase))
        {
            Console.WriteLine("Handling window function sum with partition");
            HandleWindowFunctionSumWithPartition(fromTables, toTable, columnIndex, steps, window);
        }
        else
        {
            Console.WriteLine("Handling window function sum without partition");
            HandleWindowFunctionSumNoPartition(fromTables, toTable, columnIndex, steps);
        }
        Console.WriteLine("End of window function sum");
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
    
    private static void HandleWindowFunctionSumWithPartition(List<Table> fromTables, Table toTable, int columnIndex, List<Action> steps, string window)
    {
        // Extract partition columns from "partition by col1, col2, ..." 
        var partitionPart = window.Substring(window.IndexOf("partition by", StringComparison.InvariantCultureIgnoreCase) + 12).Trim();
        //var orderPart = partitionPart.Contains("order by", StringComparison.InvariantCultureIgnoreCase)
        //    ? partitionPart.Substring(partitionPart.IndexOf("order by", StringComparison.InvariantCultureIgnoreCase))
        //    : "";
        string orderPart = "productname";

        
        Table orderedFromTable = fromTables[0].DeepClone().AppendRowIndex().OrderBy(orderPart, true);
        
        Console.WriteLine("partitionPart: " + partitionPart);
        Console.WriteLine("orderPart: " + orderPart);
        
        if (!string.IsNullOrEmpty(orderPart) && partitionPart.Contains("order by", StringComparison.InvariantCultureIgnoreCase))
            partitionPart = partitionPart.Substring(0, partitionPart.IndexOf("order by", StringComparison.InvariantCultureIgnoreCase)).Trim();
        
        var partitionColumns = partitionPart.Split(',').Select(c => c.Trim()).ToList();
        
        Console.WriteLine("partitionColumns: " + string.Join(", ", partitionColumns));
        
        // Find the column indices for partitioning in the source table
        var partitionColumnIndices = new List<int>();
        foreach (var partCol in partitionColumns)
        {
            partitionColumnIndices.Add(fromTables[0].IndexOfColumn(partCol));
            Console.WriteLine("partitionColumnIndices: " + string.Join(", ", partitionColumnIndices));
        }
        
        int sumColumnIndex = fromTables[0].IndexOfColumn("price");
        
        List<Partition> partitionsFromSource = [];
        
        // Group rows by partition values in source table
        var partitions = new Dictionary<string, int>();
        for (int i = 0; i < orderedFromTable.Entries.Count; i++)
        {
            //Console.WriteLine(orderedFromTable.Entries[i].Values[4].Value);
            string partitionKey = GetPartitionKey(orderedFromTable, i, partitionColumnIndices);
            if (!partitions.ContainsKey(partitionKey))
            {
                partitions[partitionKey] = partitionsFromSource.Count;
                partitionsFromSource.Add(new Partition([], []));
            }
            partitionsFromSource[partitions[partitionKey]].RowIndices.Add(int.Parse(orderedFromTable.Entries[i].Values[4].Value));
            partitionsFromSource[partitions[partitionKey]].Values.Add(int.Parse(orderedFromTable.Entries[i].Values[sumColumnIndex].Value));
        }
        
        foreach (var partition in partitionsFromSource)
        {
            int sum = 0;
            for (int i = 0; i < partition.Values.Count; i++)
            {
                sum += partition.Values[i];
                partition.Values[i] = sum;
            }
        }
        
        Console.WriteLine("Detected partitions from source: " + partitions.Count);
        foreach (var partition in partitionsFromSource)
        {
            Console.WriteLine("Partition rows: " + string.Join(", ", partition.RowIndices) + 
                              " Values: " + string.Join(", ", partition.Values));
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
        
        // Print detected partitions for debugging
        Console.WriteLine("Detected partitions from result: " + partitions.Count);
        foreach (var partition in partitions)
        {
            Console.WriteLine("Partition rows: " + string.Join(", ", partition.RowIndices) + 
                              " Values: " + string.Join(", ", partition.Values));
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