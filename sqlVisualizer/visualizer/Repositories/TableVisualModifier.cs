using visualizer.Exstensions;
using visualizer.Models;

namespace visualizer.Repositories;

public class TableVisualModifier
{
    public Action GenerateToggleHighlightRows(IReadOnlyList<TableEntry> entries)
    {
        //capture the list, so when its changed it doesn't apply to all functions
        var snapshot = entries.ToList();
        return () =>
        {
            foreach (var t in snapshot) t.ToggleHighlight();
        };
    }

    public Action GenerateToggleHighlightRow(TableEntry entry)
    {
        return entry.ToggleHighlight;
    }

    public Action GenerateToggleHighlightCells(Table table, int row, ICollection<int> column)
    {
        return column
            .Select(i => GenerateToggleHighlightCell(table, row, i))
            .ToOneAction();
    }

    public Action GenerateToggleHighlightCell(Table table, int row, int column)
    {
        return table.Entries[row].Values[column].ToggleHighlight;
    }

    public void ChangeHighlightColourCells(Table table, int row, ICollection<int> columns, string hexColour)
    {
        foreach (var col in columns)
            ChangeHighlightColourCell(table, row, col, hexColour);
    }

    public void ChangeHighlightColourCell(Table table, int row, int column, string hexColour)
    {
        table.Entries[row].Values[column].SetHighlightHexColor(hexColour);
    }

    public Action GenerateToggleHighlightColumn(Table table, int index)
    {
        return () =>
        {
            foreach (var te in table.Entries)
            {
                te.Values[index].ToggleHighlight();
            }
        };
    }

    public Action GenerateToggleVisibleColumn(Table table, int index)
    {
        return () =>
        {
            foreach (var te in table.Entries)
            {
                te.Values[index].ToggleVisible();
            }
        };
    }

    public Action GenerateToggleVisibleCellsInRow(TableEntry row)
    {
        var hide = new List<Action>();
        foreach (var tableValue in row.Values)
        {
            hide.Add(() => tableValue.ToggleVisible());
        }

        return hide.ToOneAction();
    }

    public Action GenerateToggleVisibleCell(Table table, int row, int column)
    {
        return table.Entries[row].Values[column].ToggleVisible;
    }

    public Action HideTablesCellBased(List<Table> tables)
    {
        var hide = new List<Action>();
        foreach (var table in tables)
            for (int i = 0; i < table.ColumnNames.Count; i++)
                hide.Add(GenerateToggleVisibleColumn(table, i));

        return hide.ToOneAction();
    }

    public Action CombineActions(List<Action> a, List<Action> b)
    {
        //capture the list, so when its changed it doesn't apply to all functions
        var snapshot = a.ToList();
        snapshot.AddRange(b.ToList());
        return () =>
        {
            foreach (var action in snapshot) action();
        };
    }

    public Action CombineActions(IEnumerable<Action> actions)
    {
        return actions.ToOneAction();
    }
}