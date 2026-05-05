using commonDataModels;
using commonDataModels.Models;
using tableGeneration.Models;

namespace tableGeneration;

public class TableOriginColumnsGenerator
{
    public void GenerateTableOriginOnToTablesColumns(ExecutedStep step)
    {
        switch (step.Step.Keyword)
        {
            case SQLKeyword.JOIN:
            case SQLKeyword.INNER_JOIN:
            case SQLKeyword.LEFT_JOIN:
            case SQLKeyword.LEFT_OUTER_JOIN:
            case SQLKeyword.RIGHT_JOIN:
            case SQLKeyword.RIGHT_OUTER_JOIN:
            case SQLKeyword.FULL_JOIN:
            case SQLKeyword.FULL_OUTER_JOIN:
            case SQLKeyword.WHERE:
            case SQLKeyword.LIMIT:
            case SQLKeyword.ORDER_BY:
                DuplicateOriginOnColumnsToSingle(step.FromTables, step.ToTables[0]);
                break;
            case SQLKeyword.SELECT:
                GenerateTableOriginOnColumnsForSelect(step);
                break;
            case SQLKeyword.GROUP_BY:
                DuplicateOriginOnColumnsToMulti(step.FromTables[0], step.ToTables);
                break;
            case SQLKeyword.HAVING:
                //since fromTables are copied to toTables, they already have origin
                break;
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

    private void GenerateTableOriginOnColumnsForSelect(ExecutedStep step)
    {
        var allTablesHaveSameColumns = step.FromTables
            .All(t => t.ColumnNames.SequenceEqual(step.FromTables[0].ColumnNames));
        if (!allTablesHaveSameColumns)
            throw new ArgumentException("select is only allowed when selecting from tabels" +
                                        ", that all contain the same colunmns");

        var toTable = step.ToTables[0];
        var fromTable = step.FromTables[0];

        if (step.Step.Clause.Trim().Equals("*"))
            DuplicateOriginOnColumnsToSingle(fromTable, toTable);
        else
            GenerateTableOriginOnColumnsForSelectSpecificColumns(fromTable, toTable, step.Step.Clause);
    }

    private void GenerateTableOriginOnColumnsForSelectSpecificColumns(Table fromTable, Table toTable, string clause)
    {
        var columnsSelected = UtilRegex.SplitSelectColumns(clause);

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