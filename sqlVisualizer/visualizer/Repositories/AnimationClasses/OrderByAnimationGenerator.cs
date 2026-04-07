using System.Text.RegularExpressions;
using visualizer.Models;
using visualizer.Utility;

namespace visualizer.Repositories.AnimationClasses;

public static class OrderByAnimationGenerator
{
    private static readonly TableVisualModifier tvm = new();

    public static Animation Generate(Table fromTable, Table toTable, SQLDecompositionComponent action)
    {
        var steps = new List<Action>();
        var orderByColumns = ParseOrderByColumns(action.Clause);
        var orderByColumnIndexes = toTable.IndexOfColumns(orderByColumns);

        var indexedResultTable = toTable.DeepClone();
        indexedResultTable.AppendRowIndex();

        var sortedEntries = new List<TableEntry>();
        steps.Add(() => toTable.Entries.Clear());

        for (int rowIndex = 0; rowIndex < fromTable.Entries.Count; rowIndex++)
        {
            var sourceEntry = fromTable.Entries[rowIndex];
            var indexedResultEntry = TakeMatchingIndexedEntry(indexedResultTable, sourceEntry);
            var insertIndex = InsertEntrySorted(indexedResultEntry, sortedEntries);

            //We only need to do highlighting in the from table,
            //as it is the same object we insert into the to table,
            //which means when we change it in the from table it's gonna change in the to table as well
            steps.Add(tvm.CombineActions(
            [
                () => toTable.Entries.Insert(insertIndex, sourceEntry),
                CreateInsertStep(fromTable, rowIndex, orderByColumnIndexes)
            ]));
            steps.Add(CreateResetHighlightStep(fromTable, rowIndex, orderByColumnIndexes));
        }

        return new Animation(steps);
    }

    private static string[] ParseOrderByColumns(string clause) =>
        Regex.Replace(clause, " desc| asc", "", RegexOptions.IgnoreCase)
            .Split(",", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static TableEntry TakeMatchingIndexedEntry(Table indexedResultTable, TableEntry sourceEntry)
    {
        var matchingEntry = indexedResultTable.Entries.First(entry =>
            entry != null && entry.Values[..^1].SequenceEqual(sourceEntry.Values));

        indexedResultTable.Entries[indexedResultTable.Entries.IndexOf(matchingEntry)] = null;
        return matchingEntry;
    }
    
    private static Action CreateInsertStep(
        Table fromTable,
        int rowIndex,
        IList<int> orderByColumnIndexes)
    {
        return tvm.CombineActions(
        [
            tvm.GenerateToggleHighlightRow(fromTable, rowIndex),
            tvm.GenerateToggleHighlightCells(fromTable, rowIndex, orderByColumnIndexes),
            tvm.ChangeHighlightColourCells(fromTable, rowIndex, orderByColumnIndexes, UtilColor.SecondaryHighlightColor)
        ]);
    }

    private static Action CreateResetHighlightStep(Table fromTable, int rowIndex, IList<int> orderByColumnIndexes)
    {
        return tvm.CombineActions(
        [
            tvm.GenerateToggleHighlightRow(fromTable, rowIndex),
            tvm.GenerateToggleHighlightCells(fromTable, rowIndex, orderByColumnIndexes),
        ]);
    }

    private sealed class RowIndexComparer : IComparer<TableEntry>
    {
        public int Compare(TableEntry? x, TableEntry? y)
        {
            ArgumentNullException.ThrowIfNull(x);
            ArgumentNullException.ThrowIfNull(y);
            return x.Values.Last().Value.CompareTo(y.Values.Last().Value);
        }
    }

    private static int InsertEntrySorted(TableEntry entry, List<TableEntry> sortedEntries)
    {
        var insertIndex = sortedEntries.BinarySearch(entry, new RowIndexComparer());

        if (insertIndex < 0)
        {
            insertIndex = ~insertIndex;
        }

        sortedEntries.Insert(insertIndex, entry);
        return insertIndex;
    }
}