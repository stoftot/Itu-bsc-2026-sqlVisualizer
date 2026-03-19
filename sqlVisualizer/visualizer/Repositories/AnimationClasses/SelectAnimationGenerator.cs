using visualizer.Exstensions;
using visualizer.Models;
using System.Text.RegularExpressions;
using visualizer.Utility;

namespace visualizer.Repositories.AnimationClasses;

public static class SelectAnimationGenerator
{
    private static TableVisualModifier tvm = new();

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

        var columns = action.Clause.Split(',').Select(c => c.Trim()).ToList();

        var toColumnIndex = 0;
        foreach (var column in columns)
        {
            if (column.Contains('('))
            {
                HandleAggregateColumn(fromTables, toTable, column, toColumnIndex, steps);
            }
            else
            {
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

            toColumnIndex++;
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
                HandleCountAggregate(fromTables, toTable, parts[1].Trim()[..^1], toColumnIndex, steps);
                break;
            case SQLAggregateFunctionsKeyword.SUM:
            case SQLAggregateFunctionsKeyword.AVG:
                HandleSumAndAvgAggregate(fromTables, toTable, parts[1].Trim()[..^1], toColumnIndex, steps);
                break;
            case SQLAggregateFunctionsKeyword.MIN:
            case SQLAggregateFunctionsKeyword.MAX:
                HandleMinAndMaxAggregate(fromTables, toTable, parts[1].Trim()[..^1], toColumnIndex, steps);
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
        var fromColumnIndexes = new List<int>();
        try
        {
            foreach (var column in columnNames)
            {
                fromColumnIndexes.Add(fromTables[0].IndexOfColumn(column));
            }
        }
        catch (ArgumentException e)
        {
            throw new NotSupportedException("The column wasn't found, this method only handles a specific columns", e);
        }

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
    
    private static void HandleNormalSelect(List<Table> fromTables, Table toTable,
        string column, int columnIndex, List<Action> steps,
        Func<int, Action> generateFromAnimation)
    {
        var fromIndex = fromTables[0].IndexOfColumn(column);
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
    }
}
