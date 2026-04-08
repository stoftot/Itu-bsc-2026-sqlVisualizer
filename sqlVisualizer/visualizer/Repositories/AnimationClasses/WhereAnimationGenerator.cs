using visualizer.Models;
using visualizer.Utility;

namespace visualizer.Repositories.AnimationClasses;

public static class WhereAnimationGenerator
{
    private static TableVisualModifier tvm = new();

    public static Animation Generate(Table fromTable, Table toTable, SQLDecompositionComponent action)
    {
        var columnsInClause = UtilRegex.ExtractReferencedColumns(action.Clause);
        var columnsInClauseIndexes = fromTable.IndexOfColumns(columnsInClause, ignoreColumnsNotFound: true);

        var steps = new List<Action>
        {
            tvm.CombineActions(
            [
                tvm.HideTableCellBased(toTable),
                tvm.ChangeHighlightColourColumns(fromTable, columnsInClauseIndexes, UtilColor.SecondaryHighlightColor)
            ])
        };

        var remainingResultRows = toTable.Entries.ToList();
        
        for(int i = 0; i < fromTable.Entries.Count; i++)
        {
            var fromEntry = fromTable.Entries[i];
            var highlightSource = tvm.CombineActions(
                [
                    tvm.GenerateToggleHighlightRow(fromEntry),
                    tvm.GenerateToggleHighlightCells(fromTable, i, columnsInClauseIndexes)
                ]);

            var matchingResult = remainingResultRows.FirstOrDefault(r =>
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
                    tvm.GenerateToggleHighlightRow(matchingResult),
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