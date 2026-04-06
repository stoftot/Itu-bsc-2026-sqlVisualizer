using visualizer.Exstensions;
using visualizer.Models;
using System.Text.RegularExpressions;
using visualizer.Utility;

namespace visualizer.Repositories.AnimationClasses;

public static class SelectAnimationGenerator
{
    private static TableVisualModifier tvm = new();

    /// <summary>
    /// Splits a SQL clause into individual columns by splitting on top-level commas
    /// (commas not inside parentheses).
    /// </summary>
    /// <param name="clause">The SQL clause to split</param>
    /// <returns>A list of column expressions, trimmed</returns>
    private static List<string> SplitClauseIntoColumns(string clause)
    {
        var columns = new List<string>();
        var currentColumn = new System.Text.StringBuilder();
        int parenDepth = 0;

        foreach (char c in clause)
        {
            if (c == '(')
            {
                parenDepth++;
                currentColumn.Append(c);
            }
            else if (c == ')')
            {
                parenDepth--;
                currentColumn.Append(c);
            }
            else if (c == ',' && parenDepth == 0)
            {
                columns.Add(currentColumn.ToString().Trim());
                currentColumn.Clear();
            }
            else
            {
                currentColumn.Append(c);
            }
        }

        // Add the last column
        if (currentColumn.Length > 0)
        {
            columns.Add(currentColumn.ToString().Trim());
        }

        return columns;
    }

    public static Animation Generate(List<Table> fromTables, Table toTable,
        SQLDecompositionComponent action)
    {
        var steps = new List<Action>();
        
        if (action.Clause.Trim().Equals("*"))
        {
            var step = tvm.CombineActions([
                tvm.GenerateToggleHighlightTables(fromTables),
                tvm.GenerateToggleHighlightTable(toTable)
            ]);
            
            return new Animation([step, step]);
        }

        steps.Add(tvm.HideTableCellBased(toTable));
        
        var columns = SplitClauseIntoColumns(action.Clause);

        var toColumnIndex = 0;
        foreach (var column in columns)
        {
            // Handel window functions
            if (column.ToLower().Contains(" over "))
            {
                HandleWindowFunction(fromTables[0], toTable, column, toColumnIndex, steps);
                toColumnIndex++;
                continue;
            }
            
            if (column.Contains('('))
            {
                HandleAggregateColumn(fromTables, toTable, column, toColumnIndex, steps);
                toColumnIndex++;
                continue;
            }
            
            if (fromTables.Count == 1)
            {
                Action FromAnimationGenerator(int i) => tvm.GenerateToggleHighlightColumn(fromTables[0], i);

                toColumnIndex += HandleNormalSelect(fromTables, toTable, column, toColumnIndex, steps, FromAnimationGenerator);
            }
            else
            {
                Action FromAnimationGenerator(int i) =>
                    fromTables.Select(table => tvm.GenerateToggleHighlightCell(table, 0, i))
                        .ToList()
                        .ToOneAction();

                toColumnIndex += HandleNormalSelect(fromTables, toTable, column, toColumnIndex, steps, FromAnimationGenerator);
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

        var columnParameters = parts[1].Split(")")[0].Trim();
        switch (keyword)
        {
            case SQLAggregateFunctionsKeyword.COUNT:
                HandleCountAggregate(fromTables, toTable, columnParameters, toColumnIndex, steps);
                break;
            case SQLAggregateFunctionsKeyword.SUM:
            case SQLAggregateFunctionsKeyword.AVG:
                HandleSumAndAvgAggregate(fromTables, toTable, columnParameters, toColumnIndex, steps);
                break;
            case SQLAggregateFunctionsKeyword.MIN:
            case SQLAggregateFunctionsKeyword.MAX:
                HandleMinAndMaxAggregate(fromTables, toTable, columnParameters, toColumnIndex, steps);
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
            //TODO: right now only supports count on specific columns
            HandleAggregateSpecificColumns(fromTables, toTable, [parameter], toColumnIndex, steps);
        }
    }

    private static void HandleSumAndAvgAggregate(List<Table> fromTables, Table toTable,
        string parameter, int toColumnIndex, List<Action> steps)
    {
        HandleAggregateSpecificColumns(fromTables, toTable, UtilRegex.ExtractReferencedColumns(parameter), toColumnIndex, steps);
    }
    
    private static void HandleMinAndMaxAggregate(List<Table> fromTables, Table toTable,
        string parameter, int toColumnIndex, List<Action> steps)
    {
        HandleAggregateSpecificColumns(fromTables, toTable, UtilRegex.ExtractReferencedColumns(parameter), toColumnIndex, steps);
    }

    private static void HandleAggregateSpecificColumns(List<Table> fromTables, Table toTable,
        IEnumerable<string> columnNames, int toColumnIndex, List<Action> steps)
    {
        var fromColumnIndexes = fromTables[0].IndexOfColumns(columnNames).ToList();

        int i = 0;
        foreach (var table in fromTables)
        {
            steps.Add(tvm.CombineActions(
            [
                tvm.GenerateToggleHighlightColumns(table, fromColumnIndexes),
                tvm.GenerateToggleVisibleCell(toTable, i, toColumnIndex),
                tvm.GenerateToggleHighlightCell(toTable, i, toColumnIndex)
            ]));

            steps.Add(tvm.CombineActions(
            [
                tvm.GenerateToggleHighlightColumns(table, fromColumnIndexes),
                tvm.GenerateToggleHighlightCell(toTable, i++, toColumnIndex)
            ]));
        }
    }
    
    private static int HandleNormalSelect(List<Table> fromTables, Table toTable,
        string column, int columnIndex, List<Action> steps,
        Func<int, Action> generateFromAnimation)
    {
        var fromIndex = fromTables[0].IndexOfColumn(column);
        if (fromIndex == -1)
        {
            var fromIndexes = fromTables[0].IndexOfOriginTableColumns(column);
            var fromAnimations = fromIndexes.Select(generateFromAnimation).ToList();
            var numberOfColumns = fromIndexes.Count;
            var visibleStep = new List<Action>();
            var HighLightStep = new List<Action>();
            
            for (int i = 0; i < numberOfColumns; i++)
            {
                visibleStep.Add(tvm.GenerateToggleVisibleColumn(toTable, columnIndex+i));
                HighLightStep.Add(tvm.GenerateToggleHighlightColumn(toTable, columnIndex+i));
            }
            
            steps.Add(tvm.CombineActions(
            [
                fromAnimations,
                visibleStep,
                HighLightStep
            ]));
            
            steps.Add(tvm.CombineActions(
            [
                fromAnimations,
                HighLightStep
            ]));
            return numberOfColumns;
        }

        var fromAnimation = generateFromAnimation(fromIndex);
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

        return 1;
    }

    private static void HandleWindowFunction(Table fromTable, Table toTable,
        string column, int columnIndex, List<Action> steps)
    {
        WindowFunction windowFunction = WindowFunction.FromString(column);
        
        switch (windowFunction.Function.ToLower())
        {
            case "avg":
            case "max":
            case "min":
            case "sum":
            case "count":
                HandleAggregateWindowFunction(fromTable, toTable, windowFunction, columnIndex, steps);
                break;
            case "rank":
            case "row_number":
            case "dense_rank":
            case "ntile":
                HandleRankingWindowFunction(fromTable, toTable, windowFunction, columnIndex, steps);
                break;
            case "lag":
            case "lead":
            case "first_value":
            case "last_value":
            case "nth_value":
                HandleValueWindowFunction(fromTable, toTable, windowFunction, columnIndex, steps);
                break;
            default:
                throw new NotImplementedException($"the window function \"{windowFunction.Function}\" is not supported");
        }
    }
    
    private static void HandleAggregateWindowFunction(Table fromTable, Table toTable, WindowFunction windowFunction, 
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

        List<List<int>> sourcePartitions = [];
        int rowIndexColumnIndex = fromTableWithRowIndex.IndexOfColumn(Table.RowIndexColumnName);

        if (windowFunction.PartitionNames.Count > 0)
        {
            List<int> partitionIndices = windowFunction.PartitionNames
                .Select(p => fromTableWithRowIndex.IndexOfColumn(p)).ToList();
            sourcePartitions = fromTableWithRowIndex.Entries
                .GroupBy(e => string.Join(", ", partitionIndices.Select(i => e.Values[i].Value)))
                .OrderBy(g => g.Key)
                .Select(g => g.Select(e => int.Parse(e.Values[rowIndexColumnIndex].Value)).ToList())
                .ToList();
        }
        else
        {
            sourcePartitions = [fromTableWithRowIndex.Entries.Select(e => int.Parse(e.Values[rowIndexColumnIndex].Value)).ToList()];
        }

        int resultTableRowIndex = 0;
        List<List<int>> resultPartitions = sourcePartitions.Select(partition => partition.Select(_ => resultTableRowIndex++).ToList()).ToList();
        

        // Generating Animation
        for (int i = 0; i < sourcePartitions.Count; i++)
        {
            var sourcePartition = sourcePartitions[i];
            var resultPartition = resultPartitions[i];

            // First, highlight all rows in this partition in the source table
            var partitionRowActions = sourcePartition
                .Select(rowIdx => tvm.GenerateToggleHighlightRow(fromTable.Entries[rowIdx]))
                .ToList();
            steps.Add(tvm.CombineActions(partitionRowActions));

            for (int j = 0; j < sourcePartition.Count; j++)
            {
                int sourceRowIndex = sourcePartition[j];
                int resultRowIndex = resultPartition[j];
                steps.Add(tvm.CombineActions(
                [
                    tvm.ChangeHighlightColourCell(fromTable, sourceRowIndex, fromTable.IndexOfColumn(windowFunction.Argument), UtilColor.SecondaryHighlightColor),
                    tvm.GenerateToggleHighlightCell(fromTable, sourceRowIndex, fromTable.IndexOfColumn(windowFunction.Argument)),
                    tvm.GenerateToggleVisibleCell(toTable, resultRowIndex, columnIndex),
                    tvm.GenerateToggleHighlightCell(toTable, resultRowIndex, columnIndex)
                ]));
            }
            
            // Unhighlight
            var unhighlightSourceCells = sourcePartition
                .Select(rowIdx => tvm.GenerateToggleHighlightCell(fromTable, rowIdx, fromTable.IndexOfColumn(windowFunction.Argument)))
                .ToList();
            
            var unhighlightResultCells = resultPartition
                .Select(rowIdx => tvm.GenerateToggleHighlightCell(toTable, rowIdx, columnIndex))
                .ToList();

            var unhighlightActions = partitionRowActions.Concat(unhighlightSourceCells).Concat(unhighlightResultCells).ToList();
            
            steps.Add(tvm.CombineActions(unhighlightActions));
        }
    }
    
    private static void HandleRankingWindowFunction(Table fromTable, Table toTable, WindowFunction windowFunction,
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

        List<List<int>> sourcePartitions = [];
        int rowIndexColumnIndex = fromTableWithRowIndex.IndexOfColumn(Table.RowIndexColumnName);

        if (windowFunction.PartitionNames.Count > 0)
        {
            List<int> partitionIndices = windowFunction.PartitionNames
                .Select(p => fromTableWithRowIndex.IndexOfColumn(p)).ToList();
            sourcePartitions = fromTableWithRowIndex.Entries
                .GroupBy(e => string.Join(", ", partitionIndices.Select(i => e.Values[i].Value)))
                .OrderBy(g => g.Key)
                .Select(g => g.Select(e => int.Parse(e.Values[rowIndexColumnIndex].Value)).ToList())
                .ToList();
        }
        else
        {
            sourcePartitions = [fromTableWithRowIndex.Entries.Select(e => int.Parse(e.Values[rowIndexColumnIndex].Value)).ToList()];
        }

        int resultTableRowIndex = 0;
        List<List<int>> resultPartitions = sourcePartitions.Select(partition => partition.Select(_ => resultTableRowIndex++).ToList()).ToList();

        List<int> orderColumnIndices = windowFunction.Orders
            .Select(o => fromTable.IndexOfColumn(o.ColumnName))
            .ToList();

        // Generating Animation
        for (int i = 0; i < sourcePartitions.Count; i++)
        {
            var sourcePartition = sourcePartitions[i];
            var resultPartition = resultPartitions[i];

            // First, highlight all rows in this partition in the source table
            var partitionRowActions = sourcePartition
                .Select(rowIdx => tvm.GenerateToggleHighlightRow(fromTable.Entries[rowIdx]))
                .ToList();
            steps.Add(tvm.CombineActions(partitionRowActions));

            for (int j = 0; j < sourcePartition.Count; j++)
            {
                int sourceRowIndex = sourcePartition[j];
                int resultRowIndex = resultPartition[j];

                tvm.ChangeHighlightColourCells(fromTable, sourceRowIndex, orderColumnIndices, UtilColor.SecondaryHighlightColor);
                var sourceHighlightActions = orderColumnIndices
                    .Select(colIdx => tvm.GenerateToggleHighlightCell(fromTable, sourceRowIndex, colIdx))
                    .ToList();

                steps.Add(tvm.CombineActions(
                [
                    sourceHighlightActions,
                    [
                        tvm.GenerateToggleVisibleCell(toTable, resultRowIndex, columnIndex),
                        tvm.GenerateToggleHighlightCell(toTable, resultRowIndex, columnIndex)
                    ]
                ]));
            }

            var unhighlightSourceCells = sourcePartition
                .SelectMany(rowIdx => orderColumnIndices
                    .Select(colIdx => tvm.GenerateToggleHighlightCell(fromTable, rowIdx, colIdx)))
                .ToList();

            var unhighlightResultCells = resultPartition
                .Select(rowIdx => tvm.GenerateToggleHighlightCell(toTable, rowIdx, columnIndex))
                .ToList();

            steps.Add(tvm.CombineActions(
                partitionRowActions.Concat(unhighlightSourceCells).Concat(unhighlightResultCells)));
        }
    }

    private static void HandleValueWindowFunction(Table fromTable, Table toTable, WindowFunction windowFunction,
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

        List<List<int>> sourcePartitions = [];
        int rowIndexColumnIndex = fromTableWithRowIndex.IndexOfColumn(Table.RowIndexColumnName);

        if (windowFunction.PartitionNames.Count > 0)
        {
            List<int> partitionIndices = windowFunction.PartitionNames
                .Select(p => fromTableWithRowIndex.IndexOfColumn(p)).ToList();
            sourcePartitions = fromTableWithRowIndex.Entries
                .GroupBy(e => string.Join(", ", partitionIndices.Select(i => e.Values[i].Value)))
                .OrderBy(g => g.Key)
                .Select(g => g.Select(e => int.Parse(e.Values[rowIndexColumnIndex].Value)).ToList())
                .ToList();
        }
        else
        {
            sourcePartitions = [fromTableWithRowIndex.Entries.Select(e => int.Parse(e.Values[rowIndexColumnIndex].Value)).ToList()];
        }

        int resultTableRowIndex = 0;
        List<List<int>> resultPartitions = sourcePartitions.Select(partition => partition.Select(_ => resultTableRowIndex++).ToList()).ToList();

        int argColumnIndex = fromTable.IndexOfColumn(windowFunction.Argument);
        int extraArg = ParseExtraArg(windowFunction.Extra);

        // Generating Animation
        for (int i = 0; i < sourcePartitions.Count; i++)
        {
            var sourcePartition = sourcePartitions[i];
            var resultPartition = resultPartitions[i];

            var partitionRowActions = sourcePartition
                .Select(rowIdx => tvm.GenerateToggleHighlightRow(fromTable.Entries[rowIdx]))
                .ToList();
            steps.Add(tvm.CombineActions(partitionRowActions));

            // Track which source cells have been highlighted (to avoid double-toggling for
            // functions where every result row maps to the same source, e.g. FIRST_VALUE)
            var highlightedSourceRows = new HashSet<int>();

            for (int j = 0; j < sourcePartition.Count; j++)
            {
                int resultRowIndex = resultPartition[j];
                int? srcRowIndex = GetValueFunctionSourceRow(windowFunction.Function.ToLower(), sourcePartition, j, extraArg);

                var revealStep = new List<Action>();

                if (srcRowIndex.HasValue && highlightedSourceRows.Add(srcRowIndex.Value))
                {
                    revealStep.Add(tvm.ChangeHighlightColourCell(fromTable, srcRowIndex.Value, argColumnIndex, UtilColor.SecondaryHighlightColor));
                    revealStep.Add(tvm.GenerateToggleHighlightCell(fromTable, srcRowIndex.Value, argColumnIndex));
                }

                revealStep.Add(tvm.GenerateToggleVisibleCell(toTable, resultRowIndex, columnIndex));
                revealStep.Add(tvm.GenerateToggleHighlightCell(toTable, resultRowIndex, columnIndex));
                steps.Add(tvm.CombineActions(revealStep));
            }

            var unhighlightSourceCells = highlightedSourceRows
                .Select(rowIdx => tvm.GenerateToggleHighlightCell(fromTable, rowIdx, argColumnIndex))
                .ToList();

            var unhighlightResultCells = resultPartition
                .Select(rowIdx => tvm.GenerateToggleHighlightCell(toTable, rowIdx, columnIndex))
                .ToList();

            steps.Add(tvm.CombineActions(
                partitionRowActions.Concat(unhighlightSourceCells).Concat(unhighlightResultCells)));
        }
    }

    private static int? GetValueFunctionSourceRow(string function, List<int> sourcePartition, int j, int extraArg)
    {
        return function switch
        {
            "first_value" => sourcePartition[0],
            "last_value" => sourcePartition[^1],
            "lag" => j - extraArg >= 0 ? sourcePartition[j - extraArg] : null,
            "lead" => j + extraArg < sourcePartition.Count ? sourcePartition[j + extraArg] : null,
            "nth_value" => extraArg - 1 < sourcePartition.Count ? sourcePartition[extraArg - 1] : null,
            _ => throw new NotImplementedException($"Value function {function} not supported")
        };
    }

    private static int ParseExtraArg(string extra, int defaultValue = 1)
    {
        if (string.IsNullOrEmpty(extra)) return defaultValue;
        var firstPart = extra.Split(',')[0].Trim();
        return int.TryParse(firstPart, out int result) ? result : defaultValue;
    }
}
