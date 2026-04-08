using visualizer.Models;

namespace visualizer.Repositories.AnimationClasses;

public static class LimitAnimationGenerator
{
    private static TableVisualModifier tvm = new();

    public static Animation Generate(Table fromTable, Table toTable, SQLDecompositionComponent action)
    {
        var steps = new List<Action>{tvm.HideTableCellBased(toTable)};
        
        if (!int.TryParse(action.Clause.Trim(), out var limitCount) || limitCount <= 0)
        {
            throw new ArgumentException($"Invalid LIMIT value: {action.Clause}");
        }
        
        for (int i = 0; i < fromTable.Entries.Count && i < limitCount; i++)
        {
            var fromEntry = fromTable.Entries[i];
            var highlightSource = tvm.GenerateToggleHighlightRow(fromEntry);

            var matchingResult = toTable.Entries.FirstOrDefault(r =>
                r.Values.Select(v => v.Value)
                    .SequenceEqual(fromEntry.Values.Select(v => v.Value)));

            if (matchingResult != null)
            {
                steps.Add(tvm.CombineActions([
                    highlightSource,
                    tvm.GenerateToggleHighlightRow(matchingResult),
                    tvm.GenerateToggleVisibleCellsInRow(matchingResult)
                ]));

                steps.Add(tvm.CombineActions([
                    highlightSource,
                    tvm.GenerateToggleHighlightRow(matchingResult)
                ]));
            }
        }

        return new Animation(steps);
    }
}

