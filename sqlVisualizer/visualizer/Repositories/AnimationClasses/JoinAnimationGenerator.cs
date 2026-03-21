using System.Diagnostics;
using visualizer.Models;

namespace visualizer.Repositories.AnimationClasses;

public static class JoinAnimationGenerator
{
    private static TableVisualModifier tvm = new();

    public static Animation Generate(List<Table> fromTables, Table toTable,
        SQLDecompositionComponent action)
    {
        switch (action.Keyword)
        {
            case SQLKeyword.JOIN:
            case SQLKeyword.INNER_JOIN:
                return GenerateJoinAndInnerJoin(fromTables, toTable, action);
            case SQLKeyword.LEFT_JOIN:
            case SQLKeyword.LEFT_OUTER_JOIN:
                throw new NotImplementedException();
            case SQLKeyword.RIGHT_JOIN:
            case SQLKeyword.RIGHT_OUTER_JOIN:
                throw new NotImplementedException();
            case SQLKeyword.FULL_JOIN:
            case SQLKeyword.FULL_OUTER_JOIN:
                throw new NotImplementedException();
            default:
                throw new NotImplementedException("not supported keyword: " + action.Keyword);
        }
    }

    private static Animation GenerateJoinAndInnerJoin(List<Table> fromTables, Table toTable,
        SQLDecompositionComponent action)
    {
        if (fromTables.Count != 2)
            throw new ArgumentException("Join animations can only be generated from two tables to one");

        var steps = new List<Action>();
        var joiningTableName = ExtractSourceName(action.Clause);

        var primaryTable = fromTables.First(t => t.Name != joiningTableName);
        var joiningTable = fromTables.First(t => t.Name == joiningTableName);

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

    private static bool AreJoinEquivalentToResult(TableEntry primary, TableEntry joining, TableEntry result)
    {
        var p = primary.Values.Select(tv => tv.Value).ToList();
        var j = joining.Values.Select(tv => tv.Value).ToList();
        var r = result.Values.Select(tv => tv.Value).ToList();

        return p.Concat(j)
            .OrderBy(x => x)
            .SequenceEqual(r.OrderBy(x => x));
    }

    private static string ExtractSourceName(string clause)
    {
        var onIndex = clause.IndexOf(" ON ", StringComparison.OrdinalIgnoreCase);
        var beforeOn = onIndex >= 0 ? clause[..onIndex].Trim() : clause.Trim();
        var parts = beforeOn.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length >= 2)
            return parts[^1];

        return parts[0];
    }
}