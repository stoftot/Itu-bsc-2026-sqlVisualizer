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
        var columnsToOrderBy = toTable.IndexOfColumns(columns);

        var copy = toTable.DeepClone();
        copy.AppendRowIndex();

        var positionReferenceList = new List<TableEntry>();
        
        //clear table;
        steps.Add(() => toTable.Entries.Clear());

        for(int i = 0; i < fromTable.Entries.Count; i++)
        {
            var entryToInsert = fromTable.Entries[i];
            var duplicateEntryToInsertBasedOn = copy.Entries.First(e => e != null && e.Values[..^1].SequenceEqual(entryToInsert.Values));
            copy.Entries[copy.Entries.IndexOf(duplicateEntryToInsertBasedOn)] = null;
            
            InsertEntrySorted(duplicateEntryToInsertBasedOn, positionReferenceList);
            var indexToInsertAt = positionReferenceList.IndexOf(duplicateEntryToInsertBasedOn);
            
            //We only need to do highlighting in the from table,
            //as it is the same object we insert into the to table,
            //which means when we change it in the from table it's gonna change in the to table as well
            steps.Add(tvm.CombineActions(
                [
                    () => toTable.Entries.Insert(indexToInsertAt, entryToInsert),
                    tvm.GenerateToggleHighlightRow(fromTable, i),
                    tvm.GenerateToggleHighlightCells(fromTable, i, columnsToOrderBy),
                    tvm.ChangeHighlightColourCells(fromTable, i, columnsToOrderBy, UtilColor.SecondaryHighlightColor)
                ]));
            
            steps.Add(tvm.CombineActions(
                [
                    tvm.GenerateToggleHighlightRow(fromTable, i),
                    tvm.GenerateToggleHighlightCells(fromTable, i, columnsToOrderBy),
                ]));
        }


        return new Animation(steps);
    }
    
    private class IndexCompare : IComparer<TableEntry>
    {
        public int Compare(TableEntry x, TableEntry y)
        {
            return x.Values.Last().Value.CompareTo(y.Values.Last().Value);
        }
    }

    private static void InsertEntrySorted(TableEntry entry, List<TableEntry> list)
    {
        var index = list.BinarySearch(entry, new IndexCompare());

        if (index < 0)
        {
            index = ~index;
        }

        list.Insert(index, entry);
    }
}