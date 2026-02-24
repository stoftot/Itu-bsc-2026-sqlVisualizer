using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic;
using visualizer.Exstensions;
using visualizer.Models;

namespace visualizer.Repositories;

public static class AnimationGenerator
{
    private static TableVisualModifier tvm = new();
    public static Animation Generate(List<Table> fromTables, List<Table> toTables, SQLDecompositionComponent action)
    {
        return action.Keyword switch
        {
            SQLKeyword.FROM => throw new NotImplementedException(),
            SQLKeyword.JOIN or SQLKeyword.INNER_JOIN => GenerateJoinAnimation(fromTables, toTables[0], action),
            SQLKeyword.LEFT_JOIN => throw new NotImplementedException(),
            SQLKeyword.RIGHT_JOIN => throw new NotImplementedException(),
            SQLKeyword.FULL_JOIN => throw new NotImplementedException(),
            SQLKeyword.WHERE => fromTables.Count > 1 && toTables.Count > 1
                ? throw new ArgumentException("where animation can only be generated from one table to another")
                : GenerateWhereAnimation(fromTables[0], toTables[0], action),
            SQLKeyword.GROUP_BY =>
                fromTables.Count > 1
                    ? throw new ArgumentException("group by animations can only be generated from one tables")
                    : GenerateGroupByAnimation(fromTables[0], toTables, action),
            SQLKeyword.HAVING => throw new NotImplementedException(),
            SQLKeyword.SELECT =>
                toTables.Count > 1
                    ? throw new ArgumentException("select animations can only be generated to one table")
                    : GenerateSelectAnimation(fromTables, toTables[0], action),
            SQLKeyword.ORDER_BY => throw new NotImplementedException(),
            SQLKeyword.LIMIT => throw new NotImplementedException(),
            SQLKeyword.OFFSET => throw new NotImplementedException(),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private static Animation GenerateJoinAnimation(List<Table> fromTables, Table toTable,
        SQLDecompositionComponent action)
    {
        if (fromTables.Count != 2)
            throw new ArgumentException("Join animations can only be generated from two tables to one");

        var steps = new List<Action>();

        var primaryTable = fromTables.First(t => t.Name != action.Clause.Split(' ')[0]);
        var joiningTable = fromTables.First(t => t.Name == action.Clause.Split(' ')[0]);

        var currentResultIndex = 0;
        List<TableEntry> toToggle = [];
        List<TableEntry> deToggle = [];
        foreach (var primaryEntry in primaryTable.Entries)
        {
            toToggle.Add(primaryEntry);
            foreach (var joiningEntry in joiningTable.Entries)
            {
                toToggle.Add(joiningEntry);
                deToggle.Add(joiningEntry);
                if (currentResultIndex < toTable.Entries.Count
                    && AreJoinEquivalentToResult(
                        primaryEntry, joiningEntry, toTable.Entries[currentResultIndex]
                    ))
                {
                    toToggle.Add(toTable.Entries[currentResultIndex]);
                    deToggle.Add(toTable.Entries[currentResultIndex]);
                    currentResultIndex++;
                }

                steps.Add(tvm.GenerateToggleHighlightRows(toToggle));
                toToggle.Clear();
                toToggle.AddRange(deToggle);
                deToggle.Clear();
            }

            toToggle.Add(primaryEntry);
        }

        steps.Add(tvm.GenerateToggleHighlightRows(toToggle));

        return new Animation(steps);
    }

    private static Animation GenerateSelectAnimation(List<Table> fromTables, Table toTable,
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

    private static Animation GenerateGroupByAnimation(Table fromTable, List<Table> toTables,
        SQLDecompositionComponent action)
    {
        var steps = new List<Action>();

        steps.Add(tvm.HideTablesCellBased(toTables));

        var columnNamesToGroupBy = action.Clause.Split(',');

        var groupByIndexes = columnNamesToGroupBy
            .Select(columName => fromTable
                .IndexOfColumn(columName.Trim())).ToList();

        var toTableEntryValueMap =
            new Dictionary<ImmutableArray<TableValue>, int>(new ImmutableArrayComparer<TableValue>());

        toTables.ForEach(table => toTableEntryValueMap
            .Add(table.Entries[0].ValuesAsImmutableArray(groupByIndexes), 0));

        for (int row = 0; row < fromTable.Entries.Count; row++)
        {
            var fromAnimations = new List<Action>();
            var currRow = fromTable.Entries[row];
            fromAnimations.Add(tvm.GenerateToggleHighlightRow(currRow));
            tvm.ChangeHighlightColourCells(fromTable, row, groupByIndexes, "146af5");
            fromAnimations.Add(tvm.GenerateToggleHighlightCells(fromTable, row, groupByIndexes));

            var fromValues = currRow.ValuesAsImmutableArray(groupByIndexes);
            var toTable = toTables
                .First(t => t.Entries[0]
                    .ValuesAsImmutableArray(groupByIndexes)
                    .SequenceEqual(fromValues));

            var indexOfToRow = toTableEntryValueMap[toTable.Entries[0].ValuesAsImmutableArray(groupByIndexes)]++;

            tvm.ChangeHighlightColourCells(toTable, indexOfToRow, groupByIndexes, "146af5");
            steps.Add(tvm.CombineActions(fromAnimations,
            [
                tvm.GenerateToggleVisibleCellsInRow(toTable.Entries[indexOfToRow]),
                tvm.GenerateToggleHighlightRow(toTable.Entries[indexOfToRow]),
                tvm.GenerateToggleHighlightCells(toTable, indexOfToRow, groupByIndexes)
            ]));

            steps.Add(tvm.CombineActions(fromAnimations,
            [
                tvm.GenerateToggleHighlightRow(toTable.Entries[indexOfToRow]),
                tvm.GenerateToggleHighlightCells(toTable, indexOfToRow, groupByIndexes)
            ]));
        }

        return new Animation(steps);
    }

    private static Animation GenerateWhereAnimation(Table fromTable, Table toTable, SQLDecompositionComponent action)
    {
        var steps = new List<Action>();

        var remainingResultRows = toTable.Entries.ToList();

        foreach (var fromEntry in fromTable.Entries)
        {
            var highlightSource = tvm.GenerateToggleHighlightRow(fromEntry);

            var matchingResult = remainingResultRows.FirstOrDefault(r =>
                r.Values.Select(v => v.Value)
                    .SequenceEqual(fromEntry.Values.Select(v => v.Value)));

            if (matchingResult != null)
            {
                steps.Add(tvm.CombineActions([
                    highlightSource,
                    tvm.GenerateToggleHighlightRow(matchingResult)
                ]));

                steps.Add(tvm.CombineActions([
                    highlightSource,
                    tvm.GenerateToggleHighlightRow(matchingResult)
                ]));

                remainingResultRows.Remove(matchingResult);
            }
            else
            {
                steps.Add(highlightSource);
                steps.Add(highlightSource);
            }
        }

        return new Animation(steps);
    }

    private static bool AreJoinEquivalentToResult(TableEntry primary, TableEntry joining, TableEntry result)
    {
        var p = primary.Values.Select(tv => tv.Value).ToList();
        var j = joining.Values.Select(tv => tv.Value).ToList();
        var r = result.Values.Select(tv => tv.Value).ToList();

        return p.Concat(j)
            .OrderBy(x => x)
            .SequenceEqual(r.OrderBy(x => x));
    }

    private sealed class ImmutableArrayComparer<T> : IEqualityComparer<ImmutableArray<T>>
    {
        private static readonly EqualityComparer<T> ItemComparer = EqualityComparer<T>.Default;

        public bool Equals(ImmutableArray<T> x, ImmutableArray<T> y)
            => x.AsSpan().SequenceEqual(y.AsSpan());

        public int GetHashCode(ImmutableArray<T> obj)
        {
            var hash = new HashCode();
            foreach (var item in obj)
                hash.Add(item, ItemComparer);
            return hash.ToHashCode();
        }
    }
}