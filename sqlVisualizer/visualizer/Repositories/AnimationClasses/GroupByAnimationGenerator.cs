using System.Collections.Immutable;
using visualizer.Models;

namespace visualizer.Repositories.AnimationClasses;

public static class GroupByAnimationGenerator
{
    private static TableVisualModifier tvm = new();
    public static Animation Generate(Table fromTable, List<Table> toTables,
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