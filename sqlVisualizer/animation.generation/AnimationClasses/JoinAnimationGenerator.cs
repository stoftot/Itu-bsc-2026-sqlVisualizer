using System.Diagnostics;
using System.Text.RegularExpressions;
using visualizer.Exstensions;
using visualizer.Models;
using visualizer.Repositories.Contracts;
using visualizer.Utility;

namespace visualizer.Repositories.AnimationClasses;

public static class JoinAnimationGenerator
{
    private static TableVisualModifier tvm = new();

    public static List<Action> Generate(List<DisplayTable> fromTables, DisplayTable toTable,
        ISQLComponent sql)
    {
        var joiningTableName = UtilRegex.ExtractTableNameFromJoin(sql.Clause());

        var primaryTable = fromTables.First(t => t.Name != joiningTableName);
        var joiningTable = fromTables.First(t => t.Name == joiningTableName);
        
        var conditionColumns = ExtractPotentialConditionColumns(sql.Clause());
        
        var primaryColumnsToHighlightIndexes = primaryTable.IndexOfColumns(conditionColumns, ignoreColumnsNotFound: true);
        var joiningColumnsToHighlightIndexes = joiningTable.IndexOfColumns(conditionColumns, ignoreColumnsNotFound: true);

        return sql.Keyword() switch
        {
            SQLKeyword.JOIN or SQLKeyword.INNER_JOIN 
                => GenerateJoinAndInnerJoin(primaryTable, joiningTable, toTable, primaryColumnsToHighlightIndexes, joiningColumnsToHighlightIndexes),
            SQLKeyword.LEFT_JOIN or SQLKeyword.LEFT_OUTER_JOIN 
                => GenerateLeftJoinAndLeftOuterJoin(primaryTable, joiningTable, toTable, primaryColumnsToHighlightIndexes, joiningColumnsToHighlightIndexes),
            SQLKeyword.RIGHT_JOIN or SQLKeyword.RIGHT_OUTER_JOIN 
                => GenerateRightJoinAndRightOuterJoin(primaryTable, joiningTable, toTable, primaryColumnsToHighlightIndexes, joiningColumnsToHighlightIndexes),
            SQLKeyword.FULL_JOIN or SQLKeyword.FULL_OUTER_JOIN 
                => GenerateFullJoinAndFullOuterJoin(primaryTable, joiningTable, toTable, primaryColumnsToHighlightIndexes, joiningColumnsToHighlightIndexes),
            _ => throw new NotImplementedException("not supported keyword: " + sql.Keyword())
        };
    }

    private static List<Action> GenerateJoinAndInnerJoin(DisplayTable primaryTable, DisplayTable joiningTable,
        DisplayTable toTable, IList<int> primaryColumnsToHighlightIndexes, 
        IList<int> joiningColumnsToHighlightIndexes)
    {
        var steps = new List<Action>();
        
        steps.Add(tvm.HideTableCellBased(toTable));
        
        //set highlight color of condition colors
        steps.Add(tvm.CombineActions(
            [
                tvm.ChangeHighlightColourColumns(primaryTable, primaryColumnsToHighlightIndexes, UtilColor.SecondaryHighlightColor),
                tvm.ChangeHighlightColourColumns(joiningTable, joiningColumnsToHighlightIndexes, UtilColor.SecondaryHighlightColor)
            ]));
        
        var currentResultIndex = 0;
        List<Action> toToggle = [];
        List<Action> deToggle = [];
        for(int primaryRow = 0; primaryRow < primaryTable.Rows.Count; primaryRow++)
        {
            var primaryEntry = primaryTable[primaryRow];
            var primaryToggle = tvm.CombineActions(
                [
                    tvm.GenerateToggleHighlightRow(primaryEntry),
                    tvm.GenerateToggleHighlightCells(primaryTable, primaryRow, primaryColumnsToHighlightIndexes)
                ]);
            
            toToggle.Add(primaryToggle);
            
            for(int joiningRow = 0; joiningRow < joiningTable.Rows.Count; joiningRow++)
            {
                var joiningEntry = joiningTable[joiningRow];
                var joiningStep = tvm.CombineActions(
                [
                    tvm.GenerateToggleHighlightRow(joiningEntry),
                    tvm.GenerateToggleHighlightCells(joiningTable, joiningRow, joiningColumnsToHighlightIndexes)
                ]);
                toToggle.Add(joiningStep);
                deToggle.Add(joiningStep);
                
                if (currentResultIndex < toTable.Rows.Count
                    && primaryEntry.AreJoinEquivalentToResult(joiningEntry, toTable[currentResultIndex]
                    ))
                {
                    toToggle.AddRange(
                    [
                        tvm.GenerateToggleVisibleCellsInRow(toTable[currentResultIndex]),
                        tvm.GenerateToggleHighlightRow(toTable[currentResultIndex])
                    ]);
                    deToggle.Add(tvm.GenerateToggleHighlightRow(toTable[currentResultIndex]));
                    currentResultIndex++;
                }

                steps.Add(toToggle.ToOneAction());
                toToggle.Clear();
                toToggle.AddRange(deToggle);
                deToggle.Clear();
            }

            toToggle.Add(primaryToggle);
        }

        steps.Add(toToggle.ToOneAction());

        return steps;
    }

    private static List<Action> GenerateLeftJoinAndLeftOuterJoin(DisplayTable primaryTable, DisplayTable joiningTable,
        DisplayTable toTable, IList<int> primaryColumnsToHighlightIndexes, 
        IList<int> joiningColumnsToHighlightIndexes)
    {
        var steps = new List<Action>();
        
        steps.Add(tvm.HideTableCellBased(toTable));
        
        //set highlight color of condition colors
        steps.Add(tvm.CombineActions(
            [
                tvm.ChangeHighlightColourColumns(primaryTable, primaryColumnsToHighlightIndexes, UtilColor.SecondaryHighlightColor),
                tvm.ChangeHighlightColourColumns(joiningTable, joiningColumnsToHighlightIndexes, UtilColor.SecondaryHighlightColor)
            ]));
        
        var currentResultIndex = 0;
        List<Action> toToggle = [];
        List<Action> deToggle = [];
        for(int primaryRow = 0; primaryRow < primaryTable.Rows.Count; primaryRow++)
        {
            var foundMatcInJoiningTable = false;
            
            var primaryEntry = primaryTable[primaryRow];
            var primaryToggle = tvm.CombineActions(
                [
                    tvm.GenerateToggleHighlightRow(primaryEntry),
                    tvm.GenerateToggleHighlightCells(primaryTable, primaryRow, primaryColumnsToHighlightIndexes)
                ]);
            
            toToggle.Add(primaryToggle);
            
            for(int joiningRow = 0; joiningRow < joiningTable.Rows.Count; joiningRow++)
            {
                var joiningEntry = joiningTable[joiningRow];
                var joiningStep = tvm.CombineActions(
                [
                    tvm.GenerateToggleHighlightRow(joiningEntry),
                    tvm.GenerateToggleHighlightCells(joiningTable, joiningRow, joiningColumnsToHighlightIndexes)
                ]);
                toToggle.Add(joiningStep);
                deToggle.Add(joiningStep);
                
                if (currentResultIndex < toTable.Rows.Count
                    && primaryEntry.AreJoinEquivalentToResult(joiningEntry, toTable[currentResultIndex]
                    ))
                {
                    foundMatcInJoiningTable = true;
                    toToggle.AddRange(
                    [
                        tvm.GenerateToggleVisibleCellsInRow(toTable[currentResultIndex]),
                        tvm.GenerateToggleHighlightRow(toTable[currentResultIndex])
                    ]);
                    deToggle.Add(tvm.GenerateToggleHighlightRow(toTable[currentResultIndex]));
                    currentResultIndex++;
                }

                steps.Add(toToggle.ToOneAction());
                toToggle.Clear();
                toToggle.AddRange(deToggle);
                deToggle.Clear();
            }

            if (!foundMatcInJoiningTable)
            {
               steps.Add(tvm.CombineActions(toToggle,
                   [
                       tvm.GenerateToggleVisibleCellsInRow(toTable[currentResultIndex]),
                       tvm.GenerateToggleHighlightRow(toTable[currentResultIndex])
                   ]));
               toToggle.Clear();
               toToggle.AddRange(
               [
                   primaryToggle,
                   tvm.GenerateToggleHighlightRow(toTable[currentResultIndex])
               ]);
               currentResultIndex++;
            } else
                toToggle.Add(primaryToggle);
        }

        steps.Add(toToggle.ToOneAction());

        return steps;
    }
    
    private static List<Action> GenerateRightJoinAndRightOuterJoin(DisplayTable primaryTable, DisplayTable joiningTable,
        DisplayTable toTable, IList<int> primaryColumnsToHighlightIndexes, 
        IList<int> joiningColumnsToHighlightIndexes)
    {
        var steps = new List<Action>();
        
        steps.Add(tvm.HideTableCellBased(toTable));
        
        //set highlight color of condition columns
        steps.Add(tvm.CombineActions(
            [
                tvm.ChangeHighlightColourColumns(primaryTable, primaryColumnsToHighlightIndexes, UtilColor.SecondaryHighlightColor),
                tvm.ChangeHighlightColourColumns(joiningTable, joiningColumnsToHighlightIndexes, UtilColor.SecondaryHighlightColor)
            ]));
        
        var currentResultIndex = 0;
        List<Action> toToggle = [];
        List<Action> deToggle = [];
        for(int joiningRow = 0; joiningRow < joiningTable.Rows.Count; joiningRow++)
        {
            var foundMatchInPrimaryTable = false;

            var joiningEntry = joiningTable[joiningRow];
            var joiningToggle = tvm.CombineActions(
                [
                    tvm.GenerateToggleHighlightRow(joiningEntry),
                    tvm.GenerateToggleHighlightCells(joiningTable, joiningRow, joiningColumnsToHighlightIndexes)
                ]);
            
            toToggle.Add(joiningToggle);
            
            for(int primaryRow = 0; primaryRow < primaryTable.Rows.Count; primaryRow++)
            {
                var primaryEntry = primaryTable[primaryRow];
                var primaryStep = tvm.CombineActions(
                [
                    tvm.GenerateToggleHighlightRow(primaryEntry),
                    tvm.GenerateToggleHighlightCells(primaryTable, primaryRow, primaryColumnsToHighlightIndexes)
                ]);
                toToggle.Add(primaryStep);
                deToggle.Add(primaryStep);
                
                if (currentResultIndex < toTable.Rows.Count
                    && primaryEntry.AreJoinEquivalentToResult(joiningEntry, toTable[currentResultIndex]
                    ))
                {
                    foundMatchInPrimaryTable = true;
                    toToggle.AddRange(
                    [
                        tvm.GenerateToggleVisibleCellsInRow(toTable[currentResultIndex]),
                        tvm.GenerateToggleHighlightRow(toTable[currentResultIndex])
                    ]);
                    deToggle.Add(tvm.GenerateToggleHighlightRow(toTable[currentResultIndex]));
                    currentResultIndex++;
                }

                steps.Add(toToggle.ToOneAction());
                toToggle.Clear();
                toToggle.AddRange(deToggle);
                deToggle.Clear();
            }

            if (!foundMatchInPrimaryTable)
            {
               steps.Add(tvm.CombineActions(toToggle,
                   [
                       tvm.GenerateToggleVisibleCellsInRow(toTable[currentResultIndex]),
                       tvm.GenerateToggleHighlightRow(toTable[currentResultIndex])
                   ]));
               toToggle.Clear();
               toToggle.AddRange(
               [
                   joiningToggle,
                   tvm.GenerateToggleHighlightRow(toTable[currentResultIndex])
               ]);
               currentResultIndex++;
            } else
                toToggle.Add(joiningToggle);
        }

        steps.Add(toToggle.ToOneAction());

        return steps;
    }
    
    private static List<Action> GenerateFullJoinAndFullOuterJoin(DisplayTable primaryTable, DisplayTable joiningTable,
        DisplayTable toTable, IList<int> primaryColumnsToHighlightIndexes, 
        IList<int> joiningColumnsToHighlightIndexes)
    {
        var steps = new List<Action>();

        steps.Add(tvm.HideTableCellBased(toTable));

        steps.Add(tvm.CombineActions(
            [
                tvm.ChangeHighlightColourColumns(primaryTable, primaryColumnsToHighlightIndexes, UtilColor.SecondaryHighlightColor),
                tvm.ChangeHighlightColourColumns(joiningTable, joiningColumnsToHighlightIndexes, UtilColor.SecondaryHighlightColor)
            ]));

        var matchedJoiningRowIndexes = new HashSet<int>();
        var currentResultIndex = 0;
        List<Action> toToggle = [];
        List<Action> deToggle = [];

        for (int primaryRow = 0; primaryRow < primaryTable.Rows.Count; primaryRow++)
        {
            var foundMatchInJoiningTable = false;

            var primaryEntry = primaryTable[primaryRow];
            var primaryToggle = tvm.CombineActions(
                [
                    tvm.GenerateToggleHighlightRow(primaryEntry),
                    tvm.GenerateToggleHighlightCells(primaryTable, primaryRow, primaryColumnsToHighlightIndexes)
                ]);

            toToggle.Add(primaryToggle);

            for (int joiningRow = 0; joiningRow < joiningTable.Rows.Count; joiningRow++)
            {
                var joiningEntry = joiningTable[joiningRow];
                var joiningCellToggle = tvm.GenerateToggleHighlightCells(joiningTable, joiningRow, joiningColumnsToHighlightIndexes);
                var joiningRowAlreadyMatched = matchedJoiningRowIndexes.Contains(joiningRow);

                var joiningStepActions = new List<Action>();
                if (!joiningRowAlreadyMatched)
                    joiningStepActions.Add(tvm.GenerateToggleHighlightRow(joiningEntry));
                joiningStepActions.Add(joiningCellToggle);

                var joiningStep = tvm.CombineActions(joiningStepActions);
                toToggle.AddRange(
                [
                    tvm.SetHighlightColourDefaultRow(joiningTable, joiningRow),
                    joiningStep
                ]);

                if (currentResultIndex < toTable.Rows.Count
                    && primaryEntry.AreJoinEquivalentToResult(joiningEntry, toTable[currentResultIndex]
                    ))
                {
                    foundMatchInJoiningTable = true;

                    matchedJoiningRowIndexes.Add(joiningRow);

                    deToggle.AddRange(
                    [
                        joiningCellToggle,
                        tvm.ChangeHighlightColourRow(joiningTable, joiningRow, UtilColor.GreenHighlightColor),
                    ]);
      
                    toToggle.AddRange(
                    [
                        tvm.GenerateToggleVisibleCellsInRow(toTable[currentResultIndex]),
                        tvm.GenerateToggleHighlightRow(toTable[currentResultIndex])
                    ]);
                    deToggle.Add(tvm.GenerateToggleHighlightRow(toTable[currentResultIndex]));
                    currentResultIndex++;
                }
                else
                {
                    deToggle.AddRange( 
                    [
                                tvm.SwitchToPreviousHighlightColorRow(joiningTable, joiningRow),
                                joiningStep
                            ]);
                }

                steps.Add(toToggle.ToOneAction());
                toToggle.Clear();
                toToggle.AddRange(deToggle);
                deToggle.Clear();
            }

            if (!foundMatchInJoiningTable)
            {
                steps.Add(tvm.CombineActions(toToggle,
                    [
                        tvm.GenerateToggleVisibleCellsInRow(toTable[currentResultIndex]),
                        tvm.GenerateToggleHighlightRow(toTable[currentResultIndex])
                    ]));
                toToggle.Clear();
                toToggle.AddRange(
                [
                    primaryToggle,
                    tvm.GenerateToggleHighlightRow(toTable[currentResultIndex])
                ]);
                currentResultIndex++;
            }
            else
            {
                toToggle.Add(primaryToggle);
            }
        }

        var finalActions = new List<Action>();

        for (int joiningRow = 0; joiningRow < joiningTable.Rows.Count; joiningRow++)
        {
            if (matchedJoiningRowIndexes.Contains(joiningRow))
                continue;

            var joiningEntry = joiningTable[joiningRow];
            finalActions.Add(() =>
            {
                joiningEntry.SetHighlightColorDefault();
                joiningEntry.IsHighlighted = true;
            });
        }

        for (int resultRow = currentResultIndex; resultRow < toTable.Rows.Count; resultRow++)
        {
            finalActions.Add(tvm.GenerateToggleVisibleCellsInRow(toTable[resultRow]));
            finalActions.Add(tvm.GenerateToggleHighlightRow(toTable[resultRow]));
        }
        
        steps.Add(toToggle.ToOneAction());
        if(finalActions.Count > 0)
            steps.Add(finalActions.ToOneAction());
        
        steps.Add(tvm.ResetTables([joiningTable, toTable]));
        
        return steps;
    }

    /// <summary>
    /// This is going to extract everything that isn't a number or a keyword.
    /// unfortunatley this means that condionts in like clauses e.g.
    /// LIKE '%@gmail.com' would return the %@gmail.com part
    /// </summary>
    /// <param name="clause"></param>
    /// <returns></returns>
    private static ISet<string> ExtractPotentialConditionColumns(string clause)
    {
        var columns = new HashSet<string>();
        
        var match = UtilRegex.Match(clause, UtilRegex.MatchWordsButNotNumbers);
        while (match.Success)
        {
            var column = Regex.Replace(match.Value, @"[,'""\)\(;]", "");
            if(!UtilRegex.joinConditionKeywordsBaseParts.Contains(column))
                columns.Add(column);
            match = match.NextMatch();
        }
        
        return columns;
    }
}
