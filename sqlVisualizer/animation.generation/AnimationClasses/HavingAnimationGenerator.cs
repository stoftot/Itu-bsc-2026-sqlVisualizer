using animationGeneration.Models;

namespace animationGeneration.AnimationClasses;

internal static class HavingAnimationGenerator
{
    private static TableVisualModifier tvm = new();

    public static List<Action> Generate(List<DisplayTable> fromTables, List<DisplayTable> toTables)
    {
        var steps = new List<Action> { tvm.HideTablesCellBased(toTables) };

        var toTableIndex = 0;
        foreach (var fromTable in fromTables)
        {
            var toggleAggregate = tvm.ToggleHighlightAggregations(fromTable);

            steps.Add(toggleAggregate);

            if (toTableIndex < toTables.Count &&
                fromTable.Rows.SequenceEqual(toTables[toTableIndex].Rows))
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

        return steps;
    }
}
