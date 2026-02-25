using visualizer.Models;

namespace visualizer.Repositories.AnimationClasses;

public static class WhereAnimationGenerator
{
    private static TableVisualModifier tvm = new();
    public static Animation Generate(Table fromTable, Table toTable, SQLDecompositionComponent action)
    {
        var steps = new List<Action>();

        var remainingResultRows = toTable.Entries.ToList();

        foreach (var fromEntry in fromTable.Entries)
        {
            var highlightSource = tvm.GenerateToggleHighlightRow(fromEntry);

            var matchingResult = remainingResultRows.FirstOrDefault(r =>
                r.Values.Select(v => v.Value)
                    .SequenceEqual(fromEntry.Values.Select(v => v.Value)));

            if (matchingResult != null)
            {
                steps.Add(tvm.CombineActions([
                    highlightSource,
                    tvm.GenerateToggleHighlightRow(matchingResult)
                ]));

                steps.Add(tvm.CombineActions([
                    highlightSource,
                    tvm.GenerateToggleHighlightRow(matchingResult)
                ]));

                remainingResultRows.Remove(matchingResult);
            }
            else
            {
                steps.Add(highlightSource);
                steps.Add(highlightSource);
            }
        }

        return new Animation(steps);
    }
}