using visualizer.Exstensions;
using visualizer.Models;

namespace visualizer.Repositories.AnimationClasses;

public static class HavingAnimationGenerator
{
    private static TableVisualModifier tvm = new();

    public static Animation Generate(List<Table> fromTables, List<Table> toTables, SQLDecompositionComponent action)
    {
        var steps = new List<Action> { tvm.HideTablesCellBased(toTables) };

        var toTableIndex = 0;
        foreach (var fromTable in fromTables)
        {
            var toggleAggregate = tvm.ToggleHighlightAggregations(fromTable);

            steps.Add(toggleAggregate);

            if (toTableIndex < toTables.Count &&
                fromTable.Entries.SequenceEqual(toTables[toTableIndex].Entries))
            {
                var step = new List<Action>()
                {
                    tvm.GenerateToggleHighlightTable(fromTable),
                    tvm.GenerateToggleHighlightTable(toTables[toTableIndex])
                };

                steps.Add(tvm.CombineActions(
                    step,
                    [tvm.HideTableCellBased(toTables[toTableIndex])]));

                steps.Add(tvm.CombineActions(
                    step,
                    [toggleAggregate]));

                toTableIndex++;
            }
            else
                steps.Add(toggleAggregate);
        }

        return new Animation(steps);
    }
}