using visualizer.Exstensions;
using visualizer.Models;

namespace visualizer.Repositories.AnimationClasses;

public static class SelectAnimationGenerator
{
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
            HandleWindowFunctionSumWithPartition(fromTables, toTable, column, columnIndex, steps, window);
        }
        else
        {
            Console.WriteLine("Handling window function sum without partition");
            HandleWindowFunctionSumNoPartition(fromTables, toTable, column, columnIndex, steps);
        }
        Console.WriteLine("End of window function sum");
    }
    
    private static void HandleWindowFunctionSumNoPartition(List<Table> fromTables, Table toTable,
        string column, int columnIndex, List<Action> steps)
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
    
    private static void HandleWindowFunctionSumWithPartition(List<Table> fromTables, Table toTable,
        string column, int columnIndex, List<Action> steps, string window)
    {
        // Extract partition columns from "partition by col1, col2, ..." 
        var partitionPart = window.Substring(window.IndexOf("partition by", StringComparison.InvariantCultureIgnoreCase) + 12).Trim();
        var orderPart = partitionPart.Contains("order by", StringComparison.InvariantCultureIgnoreCase)
            ? partitionPart.Substring(partitionPart.IndexOf("order by", StringComparison.InvariantCultureIgnoreCase))
            : "";
        
        Console.WriteLine("partitionPart: " + partitionPart);
        Console.WriteLine("orderPart: " + orderPart);
        
        if (!string.IsNullOrEmpty(orderPart))
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
        
        // Group rows by partition values in source table
        var partitions = new Dictionary<string, List<int>>();
        for (int i = 0; i < fromTables[0].Entries.Count; i++)
        {
            var partitionKey = GetPartitionKey(fromTables[0], i, partitionColumnIndices);
            Console.WriteLine("row: " + i + " partitionKey: " + partitionKey);
            if (!partitions.ContainsKey(partitionKey))
                partitions[partitionKey] = [];
            partitions[partitionKey].Add(i);
        }
        
        Console.WriteLine("partitions Keys: " + string.Join(", ", partitions.Keys));
        Console.WriteLine("partitions Values: " + string.Join(" | ", partitions.Values.Select(v => string.Join(", ", v))));
        
        // Detect the actual order of partitions by mapping result rows to source partitions
        var partitionOrder = DetectPartitionOrderFromSourceTable(fromTables[0], toTable, partitionColumnIndices, sumColumnIndex);
        
        Console.WriteLine("partitionOrder: " + string.Join(", ", partitionOrder));
        
        // Animate each partition in the order they appear in the result
        int resultRowIndex = 0;
        foreach (var partitionKey in partitionOrder)
        {
            Console.WriteLine("partitionKey: " + partitionKey);
            if (!partitions.ContainsKey(partitionKey))
                continue;
                
            var partition = partitions[partitionKey];
            
            // First, highlight all rows in this partition
            var partitionRowActions = new List<Action>();
            foreach (var rowIdx in partition)
            {
                partitionRowActions.Add(tvm.GenerateToggleHighlightRow(fromTables[0].Entries[rowIdx]));
            }
            steps.Add(tvm.CombineActions(partitionRowActions));
            
            // Then animate the sum calculation for this partition
            foreach (var rowIdx in partition)
            {
                steps.Add(tvm.CombineActions(
                [
                    tvm.GenerateToggleHighlightCell(fromTables[0], rowIdx, sumColumnIndex),
                    tvm.GenerateToggleVisibleCell(toTable, resultRowIndex, columnIndex),
                    tvm.GenerateToggleHighlightCell(toTable, resultRowIndex, columnIndex)
                ]));
                resultRowIndex++;
            }
            
            // Unhighlight the partition
            var unhighlightPartitionActions = new List<Action>();
            foreach (var rowIdx in partition)
            {
                unhighlightPartitionActions.Add(tvm.GenerateToggleHighlightRow(fromTables[0].Entries[rowIdx]));
            }
            steps.Add(tvm.CombineActions(unhighlightPartitionActions));
        }
    }
    
    private static List<string> DetectPartitionOrderFromSourceTable(Table sourceTable, Table resultTable, 
        List<int> partitionColumnIndices, int sumColumnIndex)
    {
        // Strategy: Build cumulative sums for each partition in source table order,
        // then match these cumulative values to result values to detect partition order
        
        Console.WriteLine("DetectPartitionOrderFromSourceTable");
        Console.WriteLine("partitionColumnIndices: " + string.Join(", ", partitionColumnIndices));
        
        var partitionOrder = new List<string>();
        var seenPartitions = new HashSet<string>();
        
        // First, calculate cumulative sums for each partition
        var partitionCumulativeSums = new Dictionary<string, decimal>();
        var partitionRowCounts = new Dictionary<string, int>();
        
        for (int i = 0; i < sourceTable.Entries.Count; i++)
        {
            var entry = sourceTable.Entries[i];
            var partitionKey = GetPartitionKey(sourceTable, i, partitionColumnIndices);
            if (!partitionCumulativeSums.ContainsKey(partitionKey))
            {
                partitionCumulativeSums[partitionKey] = 0;
                partitionRowCounts[partitionKey] = 0;
            }
            
            if (decimal.TryParse(entry.Values[sumColumnIndex].Value, out decimal value))
            {
                partitionCumulativeSums[partitionKey] += value;
                partitionRowCounts[partitionKey]++;
            }
        }
        
        Console.WriteLine("PartitionCumulativeSums keys: " + string.Join(", ", partitionCumulativeSums.Keys));
        Console.WriteLine("PartitionCumulativeSums values: " + string.Join(", ", partitionCumulativeSums.Values));
        Console.WriteLine("PartitionRowCounts Keys: " + string.Join(", ", partitionRowCounts.Keys));
        Console.WriteLine("PartitionRowCounts Values: " + string.Join(", ", partitionRowCounts.Values));
        
        // Now match result values to partition cumulative sums to detect order
        decimal runningSum = 0;
        foreach (var resultEntry in resultTable.Entries)
        {
            if (decimal.TryParse(resultEntry.Values[0].Value, out decimal resultValue))
            {
                // Find which partition this result row belongs to by checking cumulative sums
                foreach (var partitionKey in partitionCumulativeSums.Keys)
                {
                    if (!seenPartitions.Contains(partitionKey))
                    {
                        // The cumulative sum for this partition should match when we reach its last row
                        decimal partitionEnd = runningSum + partitionCumulativeSums[partitionKey];
                        if (resultValue <= partitionEnd)
                        {
                            seenPartitions.Add(partitionKey);
                            partitionOrder.Add(partitionKey);
                            runningSum += partitionCumulativeSums[partitionKey];
                            break;
                        }
                    }
                }
            }
        }
        
        return partitionOrder;
    }
    
    private static string GetPartitionKey(Table table, int rowIndex, List<int> partitionColumnIndices)
    {
        var keyParts = partitionColumnIndices.Select(colIdx => table.Entries[rowIndex].Values[colIdx].Value).ToList();
        return string.Join("|", keyParts);
    }
}