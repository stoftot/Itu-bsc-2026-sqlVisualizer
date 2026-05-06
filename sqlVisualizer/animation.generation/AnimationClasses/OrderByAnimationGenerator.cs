using System.Text.RegularExpressions;
using animationGeneration.Contracts;
using animationGeneration.Models;
using commonDataModels;

namespace animationGeneration.AnimationClasses;

internal static class OrderByAnimationGenerator
{
    private static readonly TableVisualModifier tvm = new();

    public static List<Action> Generate(DisplayTable fromTable, DisplayTable toTable, ISQLComponent sql)
    {
        var steps = new List<Action>();
        var orderByColumns = ParseOrderByColumns(sql.Clause());
        var orderByColumnIndexes = toTable.IndexOfColumns(orderByColumns);

        var indexedResultTable = toTable.DeepClone();
        indexedResultTable.AppendRowIndex();

        var sortedEntries = new List<DisplayTableRow>();
        steps.Add(() => toTable.Rows.Clear());

        for (int rowIndex = 0; rowIndex < fromTable.Rows.Count; rowIndex++)
        {
            var sourceEntry = fromTable[rowIndex];
            var indexedResultEntry = TakeMatchingIndexedEntry(indexedResultTable, sourceEntry);
            var insertIndex = InsertEntrySorted(indexedResultEntry, sortedEntries);

            //We only need to do highlighting in the from table,
            //as it is the same object we insert into the to table,
            //which means when we change it in the from table it's gonna change in the to table as well
            steps.Add(tvm.CombineActions(
            [
                () => toTable.Rows.Insert(insertIndex, sourceEntry),
                CreateInsertStep(fromTable, rowIndex, orderByColumnIndexes)
            ]));
            steps.Add(CreateResetHighlightStep(fromTable, rowIndex, orderByColumnIndexes));
        }

        return steps;
    }

    private static string[] ParseOrderByColumns(string clause) =>
        Regex.Replace(clause, " desc| asc", "", RegexOptions.IgnoreCase)
            .Split(",", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static DisplayTableRow TakeMatchingIndexedEntry(DisplayTable indexedResultTable, 
        DisplayTableRow sourceEntry)
    {
        var matchingEntry = indexedResultTable.Rows.First(entry =>
            entry != null && entry.Cells[..^1].SequenceEqual(sourceEntry.Cells));

        indexedResultTable.Rows[indexedResultTable.Rows.IndexOf(matchingEntry)] = null;
        return matchingEntry;
    }
    
    private static Action CreateInsertStep(
        DisplayTable fromTable,
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

    private static Action CreateResetHighlightStep(DisplayTable fromTable, 
        int rowIndex, IList<int> orderByColumnIndexes)
    {
        return tvm.CombineActions(
        [
            tvm.GenerateToggleHighlightRow(fromTable, rowIndex),
            tvm.GenerateToggleHighlightCells(fromTable, rowIndex, orderByColumnIndexes),
        ]);
    }

    private sealed class RowIndexComparer : IComparer<DisplayTableRow>
    {
        public int Compare(DisplayTableRow? x, DisplayTableRow? y)
        {
            ArgumentNullException.ThrowIfNull(x);
            ArgumentNullException.ThrowIfNull(y);
            return x.Cells.Last().Value.CompareTo(y.Cells.Last().Value);
        }
    }

    private static int InsertEntrySorted(DisplayTableRow entry, List<DisplayTableRow> sortedEntries)
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
