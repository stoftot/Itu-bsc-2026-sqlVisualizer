using animationGeneration.Contracts;
using animationGeneration.Models;
using commonDataModels;

namespace animationGeneration.AnimationClasses;

internal static class WhereAnimationGenerator
{
    private static TableVisualModifier tvm = new();

    public static List<Action> Generate(DisplayTable fromTable, DisplayTable toTable, 
        ISQLComponent sql)
    {
        var columnsInClause = UtilRegex.ExtractReferencedColumns(sql.Clause());
        var columnsInClauseIndexes = fromTable.IndexOfColumns(columnsInClause, ignoreColumnsNotFound: true);

        var steps = new List<Action>
        {
            tvm.CombineActions(
            [
                tvm.HideTableCellBased(toTable),
                tvm.ChangeHighlightColourColumns(fromTable, columnsInClauseIndexes, UtilColor.SecondaryHighlightColor)
            ])
        };

        var remainingResultRows = toTable.Rows.ToList();
        
        for(int i = 0; i < fromTable.Rows.Count; i++)
        {
            var fromEntry = fromTable[i];
            var highlightSource = tvm.CombineActions(
                [
                    tvm.GenerateToggleHighlightRow(fromEntry),
                    tvm.GenerateToggleHighlightCells(fromTable, i, columnsInClauseIndexes)
                ]);

            var matchingResult = remainingResultRows.FirstOrDefault(r =>
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

        return steps;
    }
}
