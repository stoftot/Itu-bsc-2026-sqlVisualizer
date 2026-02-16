using System.Text.RegularExpressions;
using Microsoft.VisualBasic;
using visualizer.Components.Exstension_methods;
using visualizer.Models;

namespace visualizer.Repositories;

public static class AnimationGenerator
{
    public static Animation Generate(List<Table> fromTables, List<Table> toTables, SQLDecompositionComponent action)
    {
        return action.Keyword switch
        {
            SQLKeyword.FROM => throw new NotImplementedException(),
            SQLKeyword.JOIN or SQLKeyword.INNER_JOIN => GenerateJoinAnimation(fromTables, toTables[0], action),
            SQLKeyword.LEFT_JOIN => throw new NotImplementedException(),
            SQLKeyword.RIGHT_JOIN => throw new NotImplementedException(),
            SQLKeyword.FULL_JOIN => throw new NotImplementedException(),
            SQLKeyword.WHERE => throw new NotImplementedException(),
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
        foreach (var p in primaryTable.Entries)
        {
            toToggle.Add(p);
            foreach (var j in joiningTable.Entries)
            {
                toToggle.Add(j);
                deToggle.Add(j);
                if (currentResultIndex < toTable.Entries.Count && AreEquivalent(
                        p.Values, j.Values,
                        toTable.Entries[currentResultIndex].Values))
                {
                    toToggle.Add(toTable.Entries[currentResultIndex]);
                    deToggle.Add(toTable.Entries[currentResultIndex]);
                    currentResultIndex++;
                }

                steps.Add(GenerateToggleHighlightRows(toToggle));
                toToggle.Clear();
                toToggle.AddRange(deToggle);
                deToggle.Clear();
            }

            toToggle.Add(p);
        }

        steps.Add(GenerateToggleHighlightRows(toToggle));

        return new Animation(steps);
    }

    private static Animation GenerateSelectAnimation(List<Table> fromTables, Table toTable,
        SQLDecompositionComponent action)
    {
        var steps = new List<Action>();

        steps.Add(HideTablesCellBased([toTable]));

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
                    HandelNormalSelectionSingleTable(fromTables[0], toTable, column, columnIndex, steps);
                else
                    HandelNormalSelectionMultiTables(fromTables, toTable, column, columnIndex, steps);
            }

            columnIndex++;
        }

        return new Animation(steps);
    }

    private static void HandleAggregateColumnMultiTables(List<Table> fromTables, Table toTable,
        string column, int columnIndex, List<Action> steps)
    {
        var parts = column.Split('(', 2);
        SQLAggregateFunctionsKeyword keyword;

        if (!Enum.TryParse(parts[0].Trim(), out keyword))
        {
            throw new ArgumentException($"the aggregate function \"{parts[0].Trim()}\" is not supported");
        }

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
                steps.Add(CombineActions(
                [
                    GenerateToggleHighlightRows(table.Entries),
                    GenerateToggleVisibleCell(toTable, i, columnIndex),
                    GenerateToggleHighlightCell(toTable, i, columnIndex)
                ]));
                
                steps.Add(CombineActions(
                [
                    GenerateToggleHighlightRows(table.Entries),
                    GenerateToggleHighlightCell(toTable, i++, columnIndex)
                ]));
            }
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    private static void HandelNormalSelectionSingleTable(Table fromTable, Table toTable,
        string column, int columnIndex, List<Action> steps)
    {
        var parts = column.Split('.', 2);
        var tableName = parts.Length == 2 ? parts[0] : null;
        var columnName = parts.Length == 2 ? parts[1] : parts[0];

        for (int i = 0; i < fromTable.ColumnNames.Count; i++)
        {
            var fromAnimation = GenerateToggleHighlightColumn(fromTable, i);

            if (fromTable.ColumnNames[i].Equals(columnName, StringComparison.InvariantCultureIgnoreCase) &&
                (tableName == null ||
                 fromTable.OrginalTableNames[i].Equals(tableName, StringComparison.InvariantCultureIgnoreCase)))
            {
                steps.Add(CombineActions([
                    fromAnimation,
                    GenerateToggleVisibleColumn(toTable, columnIndex)
                ]));

                steps.Add(fromAnimation);
                return;
            }

            steps.Add(fromAnimation);
            steps.Add(fromAnimation);
        }
    }

    private static void HandelNormalSelectionMultiTables(List<Table> fromTables, Table toTable,
        string column, int columnIndex, List<Action> steps)
    {
        var parts = column.Split('.', 2);
        var tableName = parts.Length == 2 ? parts[0] : null;
        var columnName = parts.Length == 2 ? parts[1] : parts[0];

        for (int i = 0; i < fromTables[0].ColumnNames.Count; i++)
        {
            var fromAnimation = fromTables.Select(table
                    => GenerateToggleHighlightCell(table, 0, i))
                .ToList()
                .ToOneAction();


            if (fromTables[0].ColumnNames[i].Equals(columnName, StringComparison.InvariantCultureIgnoreCase) &&
                (tableName == null ||
                 fromTables[0].OrginalTableNames[i].Equals(tableName, StringComparison.InvariantCultureIgnoreCase)))
            {
                steps.Add(CombineActions(
                [
                    fromAnimation,
                    GenerateToggleVisibleColumn(toTable, columnIndex),
                    GenerateToggleHighlightColumn(toTable, columnIndex)
                ]));

                steps.Add(CombineActions(
                    [
                        fromAnimation,
                        GenerateToggleHighlightColumn(toTable, columnIndex)
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

        steps.Add(HideTablesCellBased(toTables));

        var columnNameGroupingBy = action.Clause.Trim();
        var columnIndexGroupingBy = fromTable.ColumnNames.IndexOf(columnNameGroupingBy);
        var toTableEntryValueMap =
            toTables.ToDictionary(table => table.Entries[0].Values[columnIndexGroupingBy], _ => 0);

        for (int row = 0; row < fromTable.Entries.Count; row++)
        {
            var fromAnimations = new List<Action>();
            var currRow = fromTable.Entries[row];
            fromAnimations.Add(GenerateToggleHighlightRow(currRow));
            ChangeHighlightColourCell(fromTable, row, columnIndexGroupingBy, "146af5");
            fromAnimations.Add(GenerateToggleHighlightCell(fromTable, row, columnIndexGroupingBy));

            var fromValue = currRow.Values[columnIndexGroupingBy];
            var toTable = toTables.First(t => t.Entries[0].Values[columnIndexGroupingBy].Equals(fromValue));

            var indexOfToRow = toTableEntryValueMap[toTable.Entries[0].Values[columnIndexGroupingBy]]++;

            ChangeHighlightColourCell(toTable, indexOfToRow, columnIndexGroupingBy, "146af5");
            steps.Add(CombineActions(fromAnimations,
            [
                GenerateToggleVisibleCellsInRow(toTable.Entries[indexOfToRow]),
                GenerateToggleHighlightRow(toTable.Entries[indexOfToRow]),
                GenerateToggleHighlightCell(toTable, indexOfToRow, columnIndexGroupingBy)
            ]));

            steps.Add(CombineActions(fromAnimations,
            [
                GenerateToggleHighlightRow(toTable.Entries[indexOfToRow]),
                GenerateToggleHighlightCell(toTable, indexOfToRow, columnIndexGroupingBy)
            ]));
        }

        return new Animation(steps);
    }

    private static Action GenerateToggleHighlightRows(IReadOnlyList<TableEntry> entries)
    {
        //capture the list, so when its changed it doesn't apply to all functions
        var snapshot = entries.ToList();
        return () =>
        {
            foreach (var t in snapshot) t.ToggleHighlight();
        };
    }

    private static Action GenerateToggleHighlightRow(TableEntry entry)
    {
        return entry.ToggleHighlight;
    }

    private static Action GenerateToggleHighlightCell(Table table, int row, int column)
    {
        return table.Entries[row].Values[column].ToggleHighlight;
    }

    private static void ChangeHighlightColourCell(Table table, int row, int column, string hexColour)
    {
        table.Entries[row].Values[column].SetHighlightHexColor(hexColour);
    }

    private static Action GenerateToggleHighlightColumn(Table table, int index)
    {
        return () =>
        {
            foreach (var te in table.Entries)
            {
                te.Values[index].ToggleHighlight();
            }
        };
    }

    private static Action GenerateToggleVisibleColumn(Table table, int index)
    {
        return () =>
        {
            foreach (var te in table.Entries)
            {
                te.Values[index].ToggleVisible();
            }
        };
    }

    private static Action GenerateToggleVisibleCellsInRow(TableEntry row)
    {
        var hide = new List<Action>();
        foreach (var tableValue in row.Values)
        {
            hide.Add(() => tableValue.ToggleVisible());
        }

        return CombineActions(hide);
    }

    private static Action GenerateToggleVisibleCell(Table table, int row, int column)
    {
        return table.Entries[row].Values[column].ToggleVisible;
    }

    private static Action HideTablesCellBased(List<Table> tables)
    {
        var hide = new List<Action>();
        foreach (var table in tables)
            for (int i = 0; i < table.ColumnNames.Count; i++)
                hide.Add(GenerateToggleVisibleColumn(table, i));

        return CombineActions(hide);
    }

    private static Action CombineActions(List<Action> actions)
    {
        //capture the list, so when its changed it doesn't apply to all functions
        var snapshot = actions.ToList();
        return () =>
        {
            foreach (var action in snapshot) action();
        };
    }

    private static Action CombineActions(List<Action> a, List<Action> b)
    {
        //capture the list, so when its changed it doesn't apply to all functions
        var snapshot = a.ToList();
        snapshot.AddRange(b.ToList());
        return () =>
        {
            foreach (var action in snapshot) action();
        };
    }

    private static Action ToOneAction(this List<Action> actions)
    {
        return CombineActions(actions);
    }

    private static bool AreEquivalent(List<TableValue> from1, List<TableValue> from2, List<TableValue> to)
    {
        var f1 = from1.Select(tv => tv.Value).ToList();
        var f2 = from2.Select(tv => tv.Value).ToList();
        var t = to.Select(tv => tv.Value).ToList();

        return f1.Concat(f2)
            .OrderBy(x => x)
            .SequenceEqual(t.OrderBy(x => x));
    }
}