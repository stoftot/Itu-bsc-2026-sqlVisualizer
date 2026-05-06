using animationGeneration.Extensions;
using animationGeneration.Models;

namespace animationGeneration;

internal class TableVisualModifier
{
    public Action GenerateToggleHighlightRows(IReadOnlyList<DisplayTableRow> entries)
    {
        //capture the list, so when its changed it doesn't apply to all functions
        var snapshot = entries.ToList();
        return () =>
        {
            foreach (var t in snapshot) t.ToggleHighlight();
        };
    }

    public Action GenerateToggleHighlightRow(DisplayTableRow row)
    {
        return row.ToggleHighlight;
    }

    public Action GenerateToggleHighlightRow(DisplayTable table, int row)
    {
        return table[row].ToggleHighlight;
    }

    public Action GenerateToggleHighlightCells(DisplayTable table, int row, ICollection<int> column)
    {
        return column
            .Select(i => GenerateToggleHighlightCell(table, row, i))
            .ToOneAction();
    }

    public Action GenerateToggleHighlightCell(DisplayTable table, int row, int column)
    {
        return table[row][column].ToggleHighlight;
    }

    public Action SetHighlightColourDefaultRow(DisplayTable table, int row)
        => table[row].SetHighlightColorDefault;
    public Action ChangeHighlightColourRow(DisplayTable table, int row, string hexColour)
    {
        var entry = table[row];
        return () => { entry.SetHighlightHexColor(hexColour); };
    }

    public Action ChangeHighlightColourColumns(DisplayTable table, IList<int> columns, string hexColour) 
        => columns.Select(i => ChangeHighlightColourColumn(table, i, hexColour)).ToOneAction();
    public Action ChangeHighlightColourColumn(DisplayTable table, int column, string hexColour)
    {
        var actions = new List<Action>();
        for (int i = 0; i < table.Rows.Count; i++)
        {
            actions.Add(ChangeHighlightColourCell(table, i, column, hexColour));
        }
        return actions.ToOneAction();
    }
    
    public Action ChangeHighlightColourCells(DisplayTable table, int row, ICollection<int> columns, string hexColour)
        => columns.Select(col => ChangeHighlightColourCell(table, row, col, hexColour)).ToOneAction();

    public Action ChangeHighlightColourCell(DisplayTable table, int row, int column, string hexColour)
        => () => table[row][column].SetHighlightHexColor(hexColour);
    
    public Action SwitchToPreviousHighlightColorCell(DisplayTable table, int row, int column)
        =>  table[row][column].SetHighlightColorPrevious;
    
    public Action SwitchToPreviousHighlightColorRow(DisplayTable table, int row)
        =>  table[row].SetHighlightColorPrevious;

    public Action GenerateToggleHighlightColumn(DisplayTable table, int index)
    {
        return () =>
        {
            foreach (var tr in table.Rows)
            {
                tr[index].ToggleHighlight();
            }
        };
    }

    public Action GenerateToggleHighlightColumns(DisplayTable table, List<int> indexes)
        => indexes.Select(i => GenerateToggleHighlightColumn(table, i)).ToOneAction();

    public Action GenerateToggleVisibleColumn(DisplayTable table, int index)
    {
        return () =>
        {
            foreach (var tr in table.Rows)
            {
                tr[index].ToggleVisible();
            }
        };
    }

    public Action GenerateToggleVisibleCellsInRow(DisplayTableRow row)
    {
        var hide = new List<Action>();
        foreach (var cell in row.Cells)
        {
            hide.Add(() => cell.ToggleVisible());
        }

        return hide.ToOneAction();
    }

    public Action GenerateToggleVisibleCell(DisplayTable table, int row, int column)
    {
        return table[row][column].ToggleVisible;
    }

    public Action HideTableCellBased(DisplayTable table)
    {
        var hide = new List<Action>();
        for (int i = 0; i < table.ColumnNames.Count; i++)
            hide.Add(GenerateToggleVisibleColumn(table, i));
        return hide.ToOneAction();
    }

    public Action HideTablesCellBased(List<DisplayTable> tables) =>
        tables.Select(HideTableCellBased).ToOneAction();

    public Action GenerateToggleHighlightTable(DisplayTable table)
        => table.Rows.Select(GenerateToggleHighlightRow).ToOneAction();

    public Action GenerateToggleHighlightTables(List<DisplayTable> tables)
        => tables.Select(GenerateToggleHighlightTable).ToOneAction();

    public Action ToggleHighlightAggregations(DisplayTable table) =>
        table.Aggregations
            .Select(aggr => (Action)((DisplayAggregation)aggr).ToggleHighlight)
            .ToOneAction();

    public Action SetTablesHighlightStyleDefault(List<DisplayTable> tables)
        => tables.Select(SetTableHighlightStyleDefault).ToOneAction();

    public Action SetTableHighlightStyleDefault(DisplayTable table)
    {
        return () =>
        {
            foreach (var row in table.Rows)
            {
                row.SetHighlightStyleDefault();
                foreach (var cell in row.Cells)
                {
                    cell.SetHighlightStyleDefault();
                }
            }
        };
    }
    
    public Action ResetTables(List<DisplayTable> tables)
        => tables.Select(ResetTable).ToOneAction();
    public Action ResetTable(DisplayTable table)
    {
        return () =>
        {
            foreach (var row in table.Rows)
            {
                row.ResetStyleAndVisual();
                foreach (var cell in row.Cells)
                {
                    cell.ResetStyleAndVisual();
                }
            }

            foreach (var aggregation in table.Aggregations)
            {
                aggregation.ResetStyleAndVisual();
            }
        };
    }
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
