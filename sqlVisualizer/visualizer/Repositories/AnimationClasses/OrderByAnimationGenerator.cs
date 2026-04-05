using System.Text.RegularExpressions;
using visualizer.Models;
using visualizer.Utility;

namespace visualizer.Repositories.AnimationClasses;

public class OrderByAnimationGenerator
{
    private static TableVisualModifier tvm = new();
    public static Animation Generate(Table fromTable, Table toTable,
        SQLDecompositionComponent action)
    {
        var steps = new List<Action>();
        var columns = 
            Regex.Replace(action.Clause, "desc|asc", "", RegexOptions.IgnoreCase)
            .Split(",", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var columnsToGroupBy = toTable.IndexOfColumns(columns);

        var copy = toTable.DeepClone();
        
        //clear table;
        steps.Add(() => toTable.Entries.Clear());

        for(int i = 0; i < fromTable.Entries.Count; i++)
        {
            var entryToInsert = fromTable.Entries[i];
            var indexOfEntryToInsert = copy.Entries.IndexOf(entryToInsert);
            copy.Entries[indexOfEntryToInsert] = null;
            var indexToInsertAt = Math.Min(indexOfEntryToInsert, i);
            
            //We only need to do highlighting in the from table,
            //as it is the same object we insert into the to table,
            //which means when we change it in the from table it's gonna change in the to table as well
            steps.Add(tvm.CombineActions(
                [
                    () => toTable.Entries.Insert(indexToInsertAt, entryToInsert),
                    tvm.GenerateToggleHighlightRow(fromTable, i),
                    tvm.GenerateToggleHighlightCells(fromTable, i, columnsToGroupBy),
                    tvm.ChangeHighlightColourCells(fromTable, i, columnsToGroupBy, UtilColor.SecondaryHighlightColor)
                ]));
            
            steps.Add(tvm.CombineActions(
                [
                    tvm.GenerateToggleHighlightRow(fromTable, i),
                    tvm.GenerateToggleHighlightCells(fromTable, i, columnsToGroupBy),
                ]));
        }


        return new Animation(steps);
    }
}