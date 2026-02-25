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

        //TODO: Figuure out how we want to handle select *
        if (action.Clause.Trim().Equals("*"))
            return new Animation(steps);

        steps.Add(tvm.HideTablesCellBased([toTable]));

        var columns = action.Clause.Split(',').Select(c => c.Trim()).ToList();

        var columnIndex = 0;
        foreach (var column in columns)
        {
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
}