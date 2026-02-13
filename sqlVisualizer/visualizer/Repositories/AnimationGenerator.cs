using System.Text.RegularExpressions;
using visualizer.Models;

namespace visualizer.Repositories;

public static class AnimationGenerator
{
    public static Animation Generate(List<Table> fromTables, Table toTable, SQLDecompositionComponent action)
    {
        return action.Keyword switch
        {
            SQLKeyword.FROM => throw new NotImplementedException(),
            SQLKeyword.JOIN or SQLKeyword.INNER_JOIN => GenerateJoinAnimation(fromTables, toTable, action),
            SQLKeyword.LEFT_JOIN => throw new NotImplementedException(),
            SQLKeyword.RIGHT_JOIN => throw new NotImplementedException(),
            SQLKeyword.FULL_JOIN => throw new NotImplementedException(),
            SQLKeyword.WHERE => throw new NotImplementedException(),
            SQLKeyword.GROUP_BY => throw new NotImplementedException(),
            SQLKeyword.HAVING => throw new NotImplementedException(),
            SQLKeyword.SELECT =>
                fromTables.Count > 1
                    ? throw new ArgumentException("select animation can only be generated from one table to another")
                    : GenerateSelectAnimation(fromTables[0], toTable, action),
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

                steps.Add(GenerateToggle(toToggle));
                toToggle.Clear();
                toToggle.AddRange(deToggle);
                deToggle.Clear();
            }

            toToggle.Add(p);
        }

        steps.Add(GenerateToggle(toToggle));

        return new Animation(steps);
    }

    private static Animation GenerateSelectAnimation(Table fromTable, Table toTable, SQLDecompositionComponent action)
    {
        var steps = new List<Action>();

        var hideResultTable = new List<Action>();
        for (int i = 0; i < toTable.ColumnNames.Count; i++)
            hideResultTable.Add(GenerateToggleVisibleColumn(toTable, i));

        steps.Add(CombineActions(hideResultTable));

        var columns = action.Clause.Split(',').Select(c => c.Trim()).ToList();

        var columnIndex = 0;
        foreach (var column in columns)
        {
            var parts = column.Split('.', 2);
            var tableName  = parts.Length == 2 ? parts[0] : null;
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

                    columnIndex++;
                    steps.Add(fromAnimation);
                    break;
                }

                steps.Add(fromAnimation);
                steps.Add(fromAnimation);
            }
        }

        return new Animation(steps);
    }

    private static Action GenerateToggle(List<TableEntry> entries)
    {
        //capture the list, so when its changed it doesn't apply to all functions
        var snapshot = entries.ToList();
        return () =>
        {
            foreach (var t in snapshot) t.ToggleHighlight();
        };
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

    private static Action CombineActions(List<Action> actions)
    {
        //capture the list, so when its changed it doesn't apply to all functions
        var snapshot = actions.ToList();
        return () =>
        {
            foreach (var action in snapshot) action();
        };
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