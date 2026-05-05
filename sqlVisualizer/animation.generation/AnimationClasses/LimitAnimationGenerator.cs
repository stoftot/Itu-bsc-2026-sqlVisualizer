using animationGeneration.Contracts;
using animationGeneration.Models;

namespace animationGeneration.AnimationClasses;

public static class LimitAnimationGenerator
{
    private static TableVisualModifier tvm = new();

    public static List<Action> Generate(DisplayTable fromTable, DisplayTable toTable, ISQLComponent sql)
    {
        var steps = new List<Action>{tvm.HideTableCellBased(toTable)};
        
        if (!int.TryParse(sql.Clause().Trim(), out var limitCount) || limitCount <= 0)
        {
            throw new ArgumentException($"Invalid LIMIT value: {sql.Clause()}");
        }
        
        for (int i = 0; i < fromTable.Rows.Count && i < limitCount; i++)
        {
            var fromEntry = fromTable[i];
            var highlightSource = tvm.GenerateToggleHighlightRow(fromEntry);

            var matchingResult = toTable.Rows.FirstOrDefault(r =>
                r.Cells.Select(v => v.Value)
                    .SequenceEqual(fromEntry.Cells.Select(v => v.Value)));

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

        return steps;
    }
}

