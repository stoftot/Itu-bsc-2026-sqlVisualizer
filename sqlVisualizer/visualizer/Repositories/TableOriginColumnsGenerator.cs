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
            table.ColumnsOriginalTableNames.AddRange(
                Enumerable.Repeat(table.Name, table.ColumnNames.Count)
            );
        }
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
            throw new ArgumentException("select is only allowed from when selecting from tabels" +
                                        ", that all contain the same colunmns");

        var toTable = vis.ToTables[0];
        var fromTable = vis.FromTables[0];

        if (vis.Component.Clause.Trim().Equals("*"))
            DuplicateOriginOnColumnsToSingle(fromTable, toTable);


        var columnsSelected = vis.Component.Clause.Split(',').Select(c => c.Trim()).ToList();

        foreach (var column in columnsSelected)
        {
            //check if agregate founction
            if (column.Contains('('))
            {
                toTable.ColumnsOriginalTableNames.Add("()");
                continue;
            }

            var parts = column.Split('.', 2);
            var tableName = parts.Length == 2 ? parts[0] : null;
            var columnName = parts.Length == 2 ? parts[1] : parts[0];

            for (int i = 0; i < fromTable.ColumnNames.Count; i++)
            {
                if (fromTable.ColumnNames[i].Equals(columnName, StringComparison.InvariantCultureIgnoreCase) &&
                    (tableName == null ||
                     fromTable.ColumnsOriginalTableNames[i]
                         .Equals(tableName, StringComparison.InvariantCultureIgnoreCase)))
                {
                    toTable.ColumnsOriginalTableNames.Add(fromTable.ColumnsOriginalTableNames[i]);
                }
            }
        }

        if (toTable.ColumnsOriginalTableNames.Count != toTable.ColumnNames.Count)
            throw new Exception("count of original table names are supposed to match with the count of columns" +
                                $"\n{toTable.ColumnsOriginalTableNames.Count} :  {toTable.ColumnNames.Count}");
    }
}