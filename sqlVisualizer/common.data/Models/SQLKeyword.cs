namespace visualizer.Models;

public enum SQLKeyword
{
    FROM,
    JOIN,
    INNER_JOIN,
    LEFT_JOIN,
    LEFT_OUTER_JOIN,
    RIGHT_JOIN,
    RIGHT_OUTER_JOIN,
    FULL_JOIN,
    FULL_OUTER_JOIN,
    WHERE,
    GROUP_BY,
    HAVING,
    SELECT,
    // DISTINCT,
    ORDER_BY,
    LIMIT,
    OFFSET,
    WITH
}

public static class SQLKeywordExtensions
{
    public static string ToSQLString(this SQLKeyword keyword)
    {
        return keyword switch
        {
            SQLKeyword.WITH => "WITH",
            SQLKeyword.SELECT => "SELECT",
            SQLKeyword.FROM => "FROM",
            SQLKeyword.WHERE => "WHERE",
            SQLKeyword.JOIN => "JOIN",
            SQLKeyword.INNER_JOIN => "INNER JOIN",
            SQLKeyword.LEFT_JOIN => "LEFT JOIN",
            SQLKeyword.LEFT_OUTER_JOIN => "LEFT OUTER JOIN",
            SQLKeyword.RIGHT_JOIN => "RIGHT JOIN",
            SQLKeyword.RIGHT_OUTER_JOIN => "RIGHT OUTER JOIN",
            SQLKeyword.FULL_JOIN => "FULL JOIN",
            SQLKeyword.FULL_OUTER_JOIN => "FULL OUTER JOIN",
            SQLKeyword.GROUP_BY => "GROUP BY",
            SQLKeyword.HAVING => "HAVING",
            SQLKeyword.ORDER_BY => "ORDER BY",
            // SQLKeyword.DISTINCT => "DISTINCT",
            SQLKeyword.LIMIT => "LIMIT",
            SQLKeyword.OFFSET => "OFFSET",
        };
    }

    public static bool IsJoin(this SQLKeyword keyword)
    {
        return keyword switch
        {
            SQLKeyword.JOIN => true,
            SQLKeyword.INNER_JOIN => true,
            SQLKeyword.LEFT_JOIN => true,
            SQLKeyword.LEFT_OUTER_JOIN => true,
            SQLKeyword.RIGHT_JOIN => true,
            SQLKeyword.RIGHT_OUTER_JOIN => true,
            SQLKeyword.FULL_JOIN => true,
            SQLKeyword.FULL_OUTER_JOIN => true,
            _ => false,
        };
    }

    /*
     * SQL Execution order
     * FROM and/or JOIN clause
     * WHERE clause
     * GROUP BY clause
     * HAVING clause
     * SELECT statement
     * DISTINCT clause
     * ORDER BY clause
     * LIMIT and/or OFFSET clause
     */
    public static int ExecutionPrecedence(this SQLKeyword keyword)
    {
        if (keyword.IsJoin()) return 1;
        return keyword switch
        {
            SQLKeyword.WITH => -1,
            SQLKeyword.FROM => 0,
            SQLKeyword.WHERE => 2,
            SQLKeyword.GROUP_BY => 3,
            SQLKeyword.HAVING => 4,
            SQLKeyword.SELECT => 5,
            // SQLKeyword.DISTINCT => 6,
            SQLKeyword.ORDER_BY => 7,
            SQLKeyword.LIMIT => 8,
            SQLKeyword.OFFSET => 8
        };
    }
    
    public static int SyntaxPrecedence(this SQLKeyword keyword)
    {
        if(keyword.IsJoin()) return 2;
        return keyword switch
        {
            SQLKeyword.WITH => -1,
            SQLKeyword.SELECT => 0,
            // SQLKeyword.DISTINCT => 0,
            SQLKeyword.FROM => 1,
            SQLKeyword.WHERE => 3,
            SQLKeyword.GROUP_BY => 4,
            SQLKeyword.HAVING => 5,
            SQLKeyword.ORDER_BY => 7,
            SQLKeyword.LIMIT => 8,
            SQLKeyword.OFFSET => 8
        };
    }
}