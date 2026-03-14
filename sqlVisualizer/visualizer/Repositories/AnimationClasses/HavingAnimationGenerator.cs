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
            var step = new List<Action> { tvm.ToggleHighlightAggregations(fromTable) };

            if (toTableIndex < toTables.Count &&
                fromTable.Entries.SequenceEqual(toTables[toTableIndex].Entries))
            {
                step.Add(tvm.GenerateToggleHighlightTable(fromTable));
                step.Add(tvm.GenerateToggleHighlightTable(toTables[toTableIndex]));

                
                steps.Add(tvm.CombineActions([step.ToOneAction(), tvm.HideTableCellBased(toTables[toTableIndex])]));
                
                toTableIndex++;
            }
            else
                steps.Add(step.ToOneAction());

            steps.Add(step.ToOneAction());
        }

        return new Animation(steps);
    }
}