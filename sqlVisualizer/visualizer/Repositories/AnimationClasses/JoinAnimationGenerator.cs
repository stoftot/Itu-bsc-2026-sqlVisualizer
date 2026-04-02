using System.Diagnostics;
using System.Text.RegularExpressions;
using visualizer.Exstensions;
using visualizer.Models;
using visualizer.Utility;

namespace visualizer.Repositories.AnimationClasses;

public static class JoinAnimationGenerator
{
    private static TableVisualModifier tvm = new();

    public static Animation Generate(List<Table> fromTables, Table toTable,
        SQLDecompositionComponent action)
    {
        var joiningTableName = ExtractSourceName(action.Clause);

        var primaryTable = fromTables.First(t => t.Name != joiningTableName);
        var joiningTable = fromTables.First(t => t.Name == joiningTableName);
        
        var conditionColumns = ExtractPotentialConditionColumns(action.Clause);
        
        var primaryColumnsToHighlightIndexes = primaryTable.IndexOfColumns(conditionColumns, ignoreColumnsNotFound: true);
        var joiningColumnsToHighlightIndexes = joiningTable.IndexOfColumns(conditionColumns, ignoreColumnsNotFound: true);

        return action.Keyword switch
        {
            SQLKeyword.JOIN or SQLKeyword.INNER_JOIN 
                => GenerateJoinAndInnerJoin(primaryTable, joiningTable, toTable, primaryColumnsToHighlightIndexes, joiningColumnsToHighlightIndexes),
            SQLKeyword.LEFT_JOIN or SQLKeyword.LEFT_OUTER_JOIN 
                => GenerateLeftJoinAndLeftOuterJoin(primaryTable, joiningTable, toTable, primaryColumnsToHighlightIndexes, joiningColumnsToHighlightIndexes),
            SQLKeyword.RIGHT_JOIN or SQLKeyword.RIGHT_OUTER_JOIN 
                => GenerateRightJoinAndRightOuterJoin(primaryTable, joiningTable, toTable, primaryColumnsToHighlightIndexes, joiningColumnsToHighlightIndexes),
            SQLKeyword.FULL_JOIN or SQLKeyword.FULL_OUTER_JOIN 
                => GenerateFullJoinAndFullOuterJoin(primaryTable, joiningTable, toTable, primaryColumnsToHighlightIndexes, joiningColumnsToHighlightIndexes),
            _ => throw new NotImplementedException("not supported keyword: " + action.Keyword)
        };
    }

    private static Animation GenerateJoinAndInnerJoin(Table primaryTable, Table joiningTable,
        Table toTable, IList<int> primaryColumnsToHighlightIndexes, IList<int> joiningColumnsToHighlightIndexes)
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
        for(int primaryRow = 0; primaryRow < primaryTable.Entries.Count; primaryRow++)
        {
            var primaryEntry = primaryTable.Entries[primaryRow];
            var primaryToggle = tvm.CombineActions(
                [
                    tvm.GenerateToggleHighlightRow(primaryEntry),
                    tvm.GenerateToggleHighlightCells(primaryTable, primaryRow, primaryColumnsToHighlightIndexes)
                ]);
            
            toToggle.Add(primaryToggle);
            
            for(int joiningRow = 0; joiningRow < joiningTable.Entries.Count; joiningRow++)
            {
                var joiningEntry = joiningTable.Entries[joiningRow];
                var joiningStep = tvm.CombineActions(
                [
                    tvm.GenerateToggleHighlightRow(joiningEntry),
                    tvm.GenerateToggleHighlightCells(joiningTable, joiningRow, joiningColumnsToHighlightIndexes)
                ]);
                toToggle.Add(joiningStep);
                deToggle.Add(joiningStep);
                
                if (currentResultIndex < toTable.Entries.Count
                    && AreJoinEquivalentToResult(
                        primaryEntry, joiningEntry, toTable.Entries[currentResultIndex]
                    ))
                {
                    toToggle.AddRange(
                    [
                        tvm.GenerateToggleVisibleCellsInRow(toTable.Entries[currentResultIndex]),
                        tvm.GenerateToggleHighlightRow(toTable.Entries[currentResultIndex])
                    ]);
                    deToggle.Add(tvm.GenerateToggleHighlightRow(toTable.Entries[currentResultIndex]));
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

        return new Animation(steps);
    }

    private static Animation GenerateLeftJoinAndLeftOuterJoin(Table primaryTable, Table joiningTable,
        Table toTable, IList<int> primaryColumnsToHighlightIndexes, IList<int> joiningColumnsToHighlightIndexes)
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
        for(int primaryRow = 0; primaryRow < primaryTable.Entries.Count; primaryRow++)
        {
            var foundMatcInJoiningTable = false;
            
            var primaryEntry = primaryTable.Entries[primaryRow];
            var primaryToggle = tvm.CombineActions(
                [
                    tvm.GenerateToggleHighlightRow(primaryEntry),
                    tvm.GenerateToggleHighlightCells(primaryTable, primaryRow, primaryColumnsToHighlightIndexes)
                ]);
            
            toToggle.Add(primaryToggle);
            
            for(int joiningRow = 0; joiningRow < joiningTable.Entries.Count; joiningRow++)
            {
                var joiningEntry = joiningTable.Entries[joiningRow];
                var joiningStep = tvm.CombineActions(
                [
                    tvm.GenerateToggleHighlightRow(joiningEntry),
                    tvm.GenerateToggleHighlightCells(joiningTable, joiningRow, joiningColumnsToHighlightIndexes)
                ]);
                toToggle.Add(joiningStep);
                deToggle.Add(joiningStep);
                
                if (currentResultIndex < toTable.Entries.Count
                    && AreJoinEquivalentToResult(
                        primaryEntry, joiningEntry, toTable.Entries[currentResultIndex]
                    ))
                {
                    foundMatcInJoiningTable = true;
                    toToggle.AddRange(
                    [
                        tvm.GenerateToggleVisibleCellsInRow(toTable.Entries[currentResultIndex]),
                        tvm.GenerateToggleHighlightRow(toTable.Entries[currentResultIndex])
                    ]);
                    deToggle.Add(tvm.GenerateToggleHighlightRow(toTable.Entries[currentResultIndex]));
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
                       tvm.GenerateToggleVisibleCellsInRow(toTable.Entries[currentResultIndex]),
                       tvm.GenerateToggleHighlightRow(toTable.Entries[currentResultIndex])
                   ]));
               toToggle.Clear();
               toToggle.AddRange(
               [
                   primaryToggle,
                   tvm.GenerateToggleHighlightRow(toTable.Entries[currentResultIndex])
               ]);
               currentResultIndex++;
            } else
                toToggle.Add(primaryToggle);
        }

        steps.Add(toToggle.ToOneAction());

        return new Animation(steps);
    }
    
    private static Animation GenerateRightJoinAndRightOuterJoin(Table primaryTable, Table joiningTable,
        Table toTable, IList<int> primaryColumnsToHighlightIndexes, IList<int> joiningColumnsToHighlightIndexes)
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
        for(int joiningRow = 0; joiningRow < joiningTable.Entries.Count; joiningRow++)
        {
            var foundMatchInPrimaryTable = false;

            var joiningEntry = joiningTable.Entries[joiningRow];
            var joiningToggle = tvm.CombineActions(
                [
                    tvm.GenerateToggleHighlightRow(joiningEntry),
                    tvm.GenerateToggleHighlightCells(joiningTable, joiningRow, joiningColumnsToHighlightIndexes)
                ]);
            
            toToggle.Add(joiningToggle);
            
            for(int primaryRow = 0; primaryRow < primaryTable.Entries.Count; primaryRow++)
            {
                var primaryEntry = primaryTable.Entries[primaryRow];
                var primaryStep = tvm.CombineActions(
                [
                    tvm.GenerateToggleHighlightRow(primaryEntry),
                    tvm.GenerateToggleHighlightCells(primaryTable, primaryRow, primaryColumnsToHighlightIndexes)
                ]);
                toToggle.Add(primaryStep);
                deToggle.Add(primaryStep);
                
                if (currentResultIndex < toTable.Entries.Count
                    && AreJoinEquivalentToResult(
                        primaryEntry, joiningEntry, toTable.Entries[currentResultIndex]
                    ))
                {
                    foundMatchInPrimaryTable = true;
                    toToggle.AddRange(
                    [
                        tvm.GenerateToggleVisibleCellsInRow(toTable.Entries[currentResultIndex]),
                        tvm.GenerateToggleHighlightRow(toTable.Entries[currentResultIndex])
                    ]);
                    deToggle.Add(tvm.GenerateToggleHighlightRow(toTable.Entries[currentResultIndex]));
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
                       tvm.GenerateToggleVisibleCellsInRow(toTable.Entries[currentResultIndex]),
                       tvm.GenerateToggleHighlightRow(toTable.Entries[currentResultIndex])
                   ]));
               toToggle.Clear();
               toToggle.AddRange(
               [
                   joiningToggle,
                   tvm.GenerateToggleHighlightRow(toTable.Entries[currentResultIndex])
               ]);
               currentResultIndex++;
            } else
                toToggle.Add(joiningToggle);
        }

        steps.Add(toToggle.ToOneAction());

        return new Animation(steps);
    }
    
    private static Animation GenerateFullJoinAndFullOuterJoin(Table primaryTable, Table joiningTable,
        Table toTable, IList<int> primaryColumnsToHighlightIndexes, IList<int> joiningColumnsToHighlightIndexes)
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

        for (int primaryRow = 0; primaryRow < primaryTable.Entries.Count; primaryRow++)
        {
            var foundMatchInJoiningTable = false;

            var primaryEntry = primaryTable.Entries[primaryRow];
            var primaryToggle = tvm.CombineActions(
                [
                    tvm.GenerateToggleHighlightRow(primaryEntry),
                    tvm.GenerateToggleHighlightCells(primaryTable, primaryRow, primaryColumnsToHighlightIndexes)
                ]);

            toToggle.Add(primaryToggle);

            for (int joiningRow = 0; joiningRow < joiningTable.Entries.Count; joiningRow++)
            {
                var joiningEntry = joiningTable.Entries[joiningRow];
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

                if (currentResultIndex < toTable.Entries.Count
                    && AreJoinEquivalentToResult(
                        primaryEntry, joiningEntry, toTable.Entries[currentResultIndex]
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
                        tvm.GenerateToggleVisibleCellsInRow(toTable.Entries[currentResultIndex]),
                        tvm.GenerateToggleHighlightRow(toTable.Entries[currentResultIndex])
                    ]);
                    deToggle.Add(tvm.GenerateToggleHighlightRow(toTable.Entries[currentResultIndex]));
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
                        tvm.GenerateToggleVisibleCellsInRow(toTable.Entries[currentResultIndex]),
                        tvm.GenerateToggleHighlightRow(toTable.Entries[currentResultIndex])
                    ]));
                toToggle.Clear();
                toToggle.AddRange(
                [
                    primaryToggle,
                    tvm.GenerateToggleHighlightRow(toTable.Entries[currentResultIndex])
                ]);
                currentResultIndex++;
            }
            else
            {
                toToggle.Add(primaryToggle);
            }
        }

        var finalActions = new List<Action>();

        for (int joiningRow = 0; joiningRow < joiningTable.Entries.Count; joiningRow++)
        {
            if (matchedJoiningRowIndexes.Contains(joiningRow))
                continue;

            var joiningEntry = joiningTable.Entries[joiningRow];
            finalActions.Add(() =>
            {
                joiningEntry.SetHighlightColorDefault();
                joiningEntry.IsHighlighted = true;
            });
        }

        for (int resultRow = currentResultIndex; resultRow < toTable.Entries.Count; resultRow++)
        {
            finalActions.Add(tvm.GenerateToggleVisibleCellsInRow(toTable.Entries[resultRow]));
            finalActions.Add(tvm.GenerateToggleHighlightRow(toTable.Entries[resultRow]));
        }
        
        steps.Add(toToggle.ToOneAction());
        if(finalActions.Count > 0)
            steps.Add(finalActions.ToOneAction());
        
        steps.Add(tvm.ResetTables([joiningTable, toTable]));
        
        return new Animation(steps);
    }

    private static bool AreJoinEquivalentToResult(TableEntry primary, TableEntry joining, TableEntry result)
    {
        var p = primary.Values.ToList();
        var j = joining.Values.ToList();
        var r = result.Values.ToList();

        return p.Concat(j)
            .SequenceEqual(r);
    }

    private static string ExtractSourceName(string clause)
    {
        var onIndex = clause.IndexOf(" ON ", StringComparison.OrdinalIgnoreCase);
        var beforeOn = onIndex >= 0 ? clause[..onIndex].Trim() : clause.Trim();
        var parts = beforeOn.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length >= 2)
            return parts[^1];

        return parts[0];
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
