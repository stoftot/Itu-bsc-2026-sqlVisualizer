using Visualizer;

namespace TestProject1;

public class AliasReplacingTest
{
    [Fact]
    public void ReplaceAliases_ReplacesSelectAliasesInWhereAndGroupBy()
    {
        var actual = new AliasReplacer().ReplaceAliases("""
                                                       SELECT coffee_name as name, sum(cs.price_per_unit * cs.quantity) as sale
                                                       FROM coffee_sales cs 
                                                       JOIN coffee_types ct ON cs.coffee_id = ct.coffee_id
                                                       WHERE name not like '%Espresso'
                                                       GROUP BY name
                                                       ORDER BY sale
                                                       """);

        Assert.Equal("""
                     SELECT coffee_name as name, sum(cs.price_per_unit * cs.quantity) as sale
                     FROM coffee_sales cs 
                     JOIN coffee_types ct ON cs.coffee_id = ct.coffee_id
                     WHERE coffee_name not like '%Espresso'
                     GROUP BY coffee_name
                     ORDER BY sale
                     """, actual);
    }

    [Fact]
    public void ReplaceAliases_DoesNotTouchOrderByLimitOrOffset()
    {
        var actual = new AliasReplacer().ReplaceAliases("""
                                                       SELECT coffee_name AS name, sum(price) AS sale
                                                       FROM coffee_sales
                                                       WHERE name IS NOT NULL
                                                       ORDER BY sale
                                                       LIMIT sale
                                                       OFFSET sale
                                                       """);

        Assert.Equal("""
                     SELECT coffee_name AS name, sum(price) AS sale
                     FROM coffee_sales
                     WHERE coffee_name IS NOT NULL
                     ORDER BY sale
                     LIMIT sale
                     OFFSET sale
                     """, actual);
    }

    [Fact]
    public void ReplaceAliases_ReplacesAliasesInsideHavingAndJoinConditions()
    {
        var actual = new AliasReplacer().ReplaceAliases("""
                                                       SELECT ct.coffee_name AS name, SUM(cs.quantity) AS total_qty
                                                       FROM coffee_sales cs
                                                       JOIN coffee_types ct ON name = ct.coffee_name
                                                       GROUP BY name
                                                       HAVING total_qty > 10 AND name <> 'Latte'
                                                       ORDER BY total_qty
                                                       """);

        Assert.Equal("""
                     SELECT ct.coffee_name AS name, SUM(cs.quantity) AS total_qty
                     FROM coffee_sales cs
                     JOIN coffee_types ct ON ct.coffee_name = ct.coffee_name
                     GROUP BY ct.coffee_name
                     HAVING SUM(cs.quantity) > 10 AND ct.coffee_name <> 'Latte'
                     ORDER BY total_qty
                     """, actual);
    }

    [Fact]
    public void ReplaceAliases_LeavesQueriesWithoutSelectAliasesUnchanged()
    {
        var query = """
                    SELECT coffee_name, price
                    FROM coffee_sales
                    WHERE coffee_name LIKE 'E%'
                    ORDER BY price
                    """;

        var actual = new AliasReplacer().ReplaceAliases(query);

        Assert.Equal(query, actual);
    }
}
