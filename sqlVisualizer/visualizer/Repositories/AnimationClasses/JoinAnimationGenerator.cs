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
                => throw new NotImplementedException(),
            SQLKeyword.FULL_JOIN or SQLKeyword.FULL_OUTER_JOIN 
                => throw new NotImplementedException(),
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
            var currentResultIndexSnapshot = currentResultIndex;
            
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

            if (currentResultIndexSnapshot == currentResultIndex)
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