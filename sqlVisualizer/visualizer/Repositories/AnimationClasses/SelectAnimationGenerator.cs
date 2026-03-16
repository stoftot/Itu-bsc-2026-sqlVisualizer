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
            case "avg":
            case "max":
            case "min":
            case "sum":
            case "count":
                HandleAggregateWindowFunction(fromTable, toTable, windowFunction, columnIndex, steps);
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
        
        Console.WriteLine("Partitions entries:" +  string.Join(" | ", sourcePartitions.Select(g => string.Join(", ", g))));
        Console.WriteLine("Partitions entries:" +  string.Join(" | ", resultPartitions.Select(g => string.Join(", ", g))));

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
                tvm.ChangeHighlightColourCell(fromTable, sourceRowIndex, fromTable.IndexOfColumn(windowFunction.Argument), UtilColor.SecondaryHighlightColor);
                steps.Add(tvm.CombineActions(
                [
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
}