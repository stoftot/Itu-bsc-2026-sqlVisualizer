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
            var step = new List<Action>
            {
                tvm.ChangeHighlightColourRow(fromTable.AggregationTable!, 0, "146af5"),
                tvm.GenerateToggleVisibleAggregaton(fromTable),
                tvm.GenerateToggleHighlightRow(fromTable.AggregationTable!.Entries[0])
            };

            if (toTableIndex !>= toTables.Count &&
                fromTable.Entries.SequenceEqual(toTables[toTableIndex].Entries))
            {
                step.Add(tvm.GenerateToggleHighlightTable(fromTable));
                step.Add(tvm.GenerateToggleHighlightTable(toTables[toTableIndex]));
                
                
                toTableIndex++;
            }
            
            steps.Add(step.ToOneAction());
            steps.Add(step.ToOneAction());
        }
        
        return new Animation(steps);
    }
}