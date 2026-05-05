using System.Collections.Immutable;
using animationGeneration.Contracts;
using animationGeneration.Models;
using commonDataModels;

namespace animationGeneration.AnimationClasses;

public static class GroupByAnimationGenerator
{
    private static TableVisualModifier tvm = new();
    public static List<Action> Generate(DisplayTable fromTable, List<DisplayTable> toTables,
        ISQLComponent sql)
    {
        var steps = new List<Action>();

        steps.Add(tvm.HideTablesCellBased(toTables));

        var columnNamesToGroupBy = sql.Clause().Split(',');

        var groupByIndexes = columnNamesToGroupBy
            .Select(columName => fromTable
                .IndexOfColumn(columName.Trim())).ToList();

        var toTableEntryValueMap =
            new Dictionary<ImmutableArray<DisplayTableTableCell>, int>(new ImmutableArrayComparer<DisplayTableTableCell>());

        toTables.ForEach(table => toTableEntryValueMap
            .Add(table[0].ValuesAsImmutableArray(groupByIndexes), 0));

        for (int row = 0; row < fromTable.Rows.Count; row++)
        {
            var fromAnimations = new List<Action>();
            var currRow = fromTable[row];
            
            fromAnimations.AddRange(
            [
                tvm.GenerateToggleHighlightRow(currRow),
                tvm.ChangeHighlightColourCells(fromTable, row, groupByIndexes, UtilColor.SecondaryHighlightColor),
                tvm.GenerateToggleHighlightCells(fromTable, row, groupByIndexes)
            ]);

            var fromValues = currRow.ValuesAsImmutableArray(groupByIndexes);
            var toTable = toTables
                .First(t => t[0]
                    .ValuesAsImmutableArray(groupByIndexes)
                    .SequenceEqual(fromValues));

            var indexOfToRow = toTableEntryValueMap[toTable[0].ValuesAsImmutableArray(groupByIndexes)]++;

            steps.Add(tvm.CombineActions(fromAnimations,
            [
                tvm.ChangeHighlightColourCells(toTable, indexOfToRow, groupByIndexes, UtilColor.SecondaryHighlightColor),
                tvm.GenerateToggleVisibleCellsInRow(toTable[indexOfToRow]),
                tvm.GenerateToggleHighlightRow(toTable[indexOfToRow]),
                tvm.GenerateToggleHighlightCells(toTable, indexOfToRow, groupByIndexes)
            ]));

            steps.Add(tvm.CombineActions(fromAnimations,
            [
                tvm.GenerateToggleHighlightRow(toTable[indexOfToRow]),
                tvm.GenerateToggleHighlightCells(toTable, indexOfToRow, groupByIndexes)
            ]));
        }

        return steps;
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