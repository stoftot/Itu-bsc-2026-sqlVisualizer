using visualizer.Models;

namespace visualizer.Repositories;

public class TableOriginColumnsGenerator
{
    public void GenerateTableOriginOnToTablesColumns(Visualisation vis)
    {
        switch (vis.Component.Keyword)
        {
            case SQLKeyword.JOIN:
            case SQLKeyword.INNER_JOIN:
            case SQLKeyword.LEFT_JOIN:
            case SQLKeyword.RIGHT_JOIN:
            case SQLKeyword.FULL_JOIN:
                DuplicateOriginOnColumnsToSingle(vis.FromTables, vis.ToTables[0]);
                break;
            case SQLKeyword.SELECT:
                GenerateTableOriginOnColumnsForSelect(vis);
                break;
            case SQLKeyword.GROUP_BY:
                DuplicateOriginOnColumnsToMulti(vis.FromTables[0], vis.ToTables);
                break;
            case SQLKeyword.WHERE:
                DuplicateOriginOnColumnsToSingle(vis.FromTables, vis.ToTables[0]);
                break;
            case SQLKeyword.HAVING:
                //since fromTables are copied to toTables, they already have origin
                break;
            case SQLKeyword.ORDER_BY:
            case SQLKeyword.LIMIT:
            case SQLKeyword.OFFSET:
                throw new NotImplementedException();
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public void GenerateTableOriginOnColumnsFromTableName(List<Table> tables)
    {
        foreach (var table in tables)
        {
            GenerateTableOriginOnColumnsFromTableName(table);
        }
    }

    public void GenerateTableOriginOnColumnsFromTableName(Table table)
    {
        table.ColumnsOriginalTableNames.AddRange(
            Enumerable.Repeat(table.Name, table.ColumnNames.Count));
    }

    public void DuplicateOriginOnColumnsToSingle(Table fromTable, Table toTable)
    {
        DuplicateOriginOnColumnsToSingle([fromTable], toTable);
    }

    private void DuplicateOriginOnColumnsToSingle(List<Table> fromTables, Table toTable)
    {
        foreach (var table in fromTables)
        {
            toTable.ColumnsOriginalTableNames.AddRange(table.ColumnsOriginalTableNames);
        }
    }

    private void DuplicateOriginOnColumnsToMulti(Table fromTable, List<Table> toTables)
    {
        foreach (var table in toTables)
        {
            DuplicateOriginOnColumnsToSingle(fromTable, table);
        }
    }

    public void DuplicateOriginOnColumnsToMulti(List<Table> fromTables, List<Table> toTables)
    {
        if (fromTables.Count != toTables.Count)
            throw new ArgumentException("count of from tables and to tables are supposed to match");
        for (int i = 0; i < fromTables.Count; i++)
        {
            DuplicateOriginOnColumnsToSingle(fromTables[i], toTables[i]);
        }
    }

    private void GenerateTableOriginOnColumnsForSelect(Visualisation vis)
    {
        var allTablesHaveSameColumns = vis.FromTables
            .All(t => t.ColumnNames.SequenceEqual(vis.FromTables[0].ColumnNames));
        if (!allTablesHaveSameColumns)
            throw new ArgumentException("select is only allowed when selecting from tabels" +
                                        ", that all contain the same colunmns");

        var toTable = vis.ToTables[0];
        var fromTable = vis.FromTables[0];

        if (vis.Component.Clause.Trim().Equals("*"))
            DuplicateOriginOnColumnsToSingle(fromTable, toTable);
        else
            GenerateTableOriginOnColumnsForSelectSpecificColumns(fromTable, toTable, vis.Component.Clause);
    }

    private void GenerateTableOriginOnColumnsForSelectSpecificColumns(Table fromTable, Table toTable, string clause)
    {
        var columnsSelected = clause.Split(',').Select(c => c.Trim()).ToList();

        foreach (var column in columnsSelected)
        {
            //check if agregate founction
            if (column.Contains('('))
            {
                toTable.ColumnsOriginalTableNames.Add("()");
                continue;
            }

            var fromIndex = fromTable.IndexOfColumn(column);

            if (fromIndex == -1)
            {
                var fromIndexes = fromTable.IndexOfOriginTableColumns(column);
                foreach (var index in fromIndexes)
                {
                    toTable.ColumnsOriginalTableNames
                        .Add(fromTable.ColumnsOriginalTableNames[index]);
                }
            }
            else
                toTable.ColumnsOriginalTableNames
                    .Add(fromTable.ColumnsOriginalTableNames[fromIndex]);
        }
    }
}