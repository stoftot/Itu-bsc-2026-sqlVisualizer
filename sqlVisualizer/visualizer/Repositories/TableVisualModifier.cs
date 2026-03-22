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

    public Action ChangeHighlightColourRow(Table table, int row, string hexColour)
    {
        var entry = table.Entries[row];
        return () => { entry.SetHighlightHexColor(hexColour); };
    }

    public Action ChangeHighlightColourCells(Table table, int row, ICollection<int> columns, string hexColour)
        => columns.Select(col => ChangeHighlightColourCell(table, row, col, hexColour)).ToOneAction();

    public Action ChangeHighlightColourCell(Table table, int row, int column, string hexColour)
    {
        return () => table.Entries[row].Values[column].SetHighlightHexColor(hexColour);
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

    public Action GenerateToggleHighlightColumns(Table table, List<int> indexes)
        => indexes.Select(i => GenerateToggleHighlightColumn(table, i)).ToOneAction();

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

    public Action HideTableCellBased(Table table)
    {
        var hide = new List<Action>();
        for (int i = 0; i < table.ColumnNames.Count; i++)
            hide.Add(GenerateToggleVisibleColumn(table, i));
        return hide.ToOneAction();
    }

    public Action HideTablesCellBased(List<Table> tables) =>
        tables.Select(HideTableCellBased).ToOneAction();

    public Action GenerateToggleHighlightTable(Table table)
        => table.Entries.Select(GenerateToggleHighlightRow).ToOneAction();

    public Action GenerateToggleHighlightTables(List<Table> tables)
        => tables.Select(GenerateToggleHighlightTable).ToOneAction();

    public Action ToggleHighlightAggregations(Table table) =>
        table.Aggregations
            .Select(aggr => (Action)aggr.ToggleHighlight)
            .ToOneAction();

    public Action SetTablesHighlightStyleDefault(List<Table> tables)
        => tables.Select(SetTableHighlightStyleDefault).ToOneAction();

    public Action SetTableHighlightStyleDefault(Table table)
    {
        return () => table.Entries.Select<TableEntry, Action>(entry =>
            entry.Values.Select<TableValue, Action>(v =>
                v.SetHighlightStyleDefault).ToOneAction());
    }
    
    public Action ResetTable(Table table)
    {
        return () => table.Entries.Select<TableEntry, Action>(entry =>
            entry.Values.Select<TableValue, Action>(v =>
                v.ResetStyleAndVisual).ToOneAction());
    }

    public Action ResetTables(List<Table> tables)
        => tables.Select(ResetTable).ToOneAction();
    public Action CombineActions(IEnumerable<Action> a, IEnumerable<Action> b)
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

    public Action CombineActions(IEnumerable<IEnumerable<Action>> actions)
    {
        var snapShot = actions.ToList().Select(a => a.ToList()).ToList();

        return () => { snapShot.ForEach(a => a.ForEach(action => action())); };
    }
}