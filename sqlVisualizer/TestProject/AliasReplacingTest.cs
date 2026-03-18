using Visualizer;

namespace TestProject1;

public class AliasReplacingTest
{
    [Theory]
    [InlineData("""
                SELECT p.price pic, pu.productname, pu.purchasetime as ptime
                FROM product as p
                JOIN purchase as pu ON p.productname = pu.productname
                JOIN user u on pu.username = u.username
                """,
        """
                SELECT product.price, purchase.productname, purchase.purchasetime
                FROM product
                JOIN purchase ON product.productname = purchase.productname
                JOIN user on purchase.username = user.username
                """
                )]
    [InlineData("""
                SELECT p.price p, pu.productname as pu, pu.purchasetime product
                FROM product as p
                JOIN purchase as pu ON p.productname = pu.productname
                JOIN user u on pu.username = u.username
                """,
        """
                SELECT product.price, purchase.productname, purchase.purchasetime
                FROM product
                JOIN purchase ON product.productname = purchase.productname
                JOIN user on purchase.username = user.username
                """
                )]
    [InlineData("""
                SELECT p.price p, pu.productname as pu, pu.purchasetime product FROM product as p JOIN purchase as pu ON p.productname = pu.productname JOIN user u on pu.username = u.username
                """,
        """
                SELECT product.price, purchase.productname, purchase.purchasetime FROM product JOIN purchase ON product.productname = purchase.productname JOIN user on purchase.username = user.username
                """
    )]
    [InlineData("""
                SELECT product.price, purchase.productname, purchase.purchasetime
                FROM product
                JOIN purchase ON product.productname = purchase.productname
                JOIN user on purchase.username = user.username
                """,
        """
                SELECT product.price, purchase.productname, purchase.purchasetime
                FROM product
                JOIN purchase ON product.productname = purchase.productname
                JOIN user on purchase.username = user.username
                """
    )]
    [InlineData("""
                SELECT DISTINCT product.productname FROM product
                """,
        """
                SELECT DISTINCT product.productname FROM product
                """ 
    )]
    [InlineData("""
                SELECT DISTINCT product.productname pic FROM product
                """,
        """
                SELECT DISTINCT product.productname FROM product
                """ 
    )]
    [InlineData("""
                SELECT DISTINCT product.productname as pic FROM product
                """,
        """
                SELECT DISTINCT product.productname FROM product
                """ 
    )]
    [InlineData("""
                SELECT DISTINCT product.productname as DISTINCT_Pname FROM product
                """,
        """
                SELECT DISTINCT product.productname FROM product
                """ 
    )]
    [InlineData("""
                SELECT DISTINCT product.productname DISTINCT_Pname FROM product
                """,
        """
                SELECT DISTINCT product.productname FROM product
                """ 
    )]
    [InlineData("""
                SELECT productname, count() 
                FROM purchase 
                GROUP BY productname
                HAVING COUNT() > 2
                """,
        """
                SELECT productname, count() 
                FROM purchase 
                GROUP BY productname
                HAVING COUNT() > 2
                """ 
    )]
    [InlineData("""
                SELECT "*" FROM "123" 
                JOIN "45" ON "45"."user" = "123"."user"
                """,
        """
                SELECT "*" FROM "123" 
                JOIN "45" ON "45"."user" = "123"."user"
                """ 
    )]
    [InlineData("""
                SELECT "65".theBest, "65".theSecondBest as TSB FROM "123" 
                JOIN "45" as "65" ON "65"."user" = "123"."user"
                """,
        """
                SELECT "45".theBest, "45".theSecondBest FROM "123" 
                JOIN "45" ON "45"."user" = "123"."user"
                """ 
    )]
    [InlineData("""
                SELECT SUM(hello) FROM TEST
                """,
        """
                SELECT SUM(hello) FROM TEST
                """ 
    )]
    [InlineData("""
                SELECT AVG(hello) FROM TEST
                """,
        """
                SELECT AVG(hello) FROM TEST
                """ 
    )]
    [InlineData("""
                SELECT MAX(hello) FROM TEST
                """,
        """
                SELECT MAX(hello) FROM TEST
                """ 
    )]
    [InlineData("""
                SELECT MIN(hello) FROM TEST
                """,
        """
                SELECT MIN(hello) FROM TEST
                """ 
    )]
    [InlineData("""
                SELECT COUNT(hello) FROM TEST
                """,
        """
                SELECT COUNT(hello) FROM TEST
                """ 
    )]
    [InlineData("""
                SELECT SUM(hello + 123) FROM TEST
                """,
        """
                SELECT SUM(hello + 123) FROM TEST
                """ 
    )]
    [InlineData("""
                SELECT SUM(hello + 123) as SS FROM TEST
                """,
        """
                SELECT SUM(hello + 123) FROM TEST
                """ 
    )]
    [InlineData("""
                SELECT SUM(hello + 123) SS FROM TEST
                """,
        """
                SELECT SUM(hello + 123) FROM TEST
                """ 
    )]
    [InlineData("""
                SELECT SUM("123" + "123") FROM TEST
                """,
        """
                SELECT SUM("123" + "123") FROM TEST
                """ 
    )]
    [InlineData("""
                SELECT SUM("*" + "/") FROM TEST
                """,
        """
                SELECT SUM("*" + "/") FROM TEST
                """ 
    )]
    [InlineData("""
                SELECT SUM("*" + "/") as S1 FROM TEST
                """,
        """
                SELECT SUM("*" + "/") FROM TEST
                """ 
    )]
    [InlineData("""
                SELECT SUM("*" + "/") S1 FROM TEST
                """,
        """
                SELECT SUM("*" + "/") FROM TEST
                """ 
    )]
    [InlineData("""
                SELECT SUM("*" + 123 + "/") FROM TEST
                """,
        """
                SELECT SUM("*" + 123 + "/") FROM TEST
                """ 
    )]
    [InlineData("""
                SELECT SUM("*" + "/" + 123) FROM TEST
                """,
        """
                SELECT SUM("*" + "/" + 123) FROM TEST
                """ 
    )]
    [InlineData("""
                SELECT SUM(123 + "*" + "/") FROM TEST
                """,
        """
                SELECT SUM(123 + "*" + "/") FROM TEST
                """ 
    )]
    [InlineData("""
                SELECT p.productname
                FROM product p
                WHERE p.price > 10
                ORDER BY p.productname
                """,
        """
                SELECT product.productname
                FROM product
                WHERE product.price > 10
                ORDER BY product.productname
                """
    )]
    [InlineData("""
                SELECT COALESCE(p.productname, p.price) display_value
                FROM product p
                """,
        """
                SELECT COALESCE(product.productname, product.price)
                FROM product
                """
    )]
    [InlineData("""
                select username, productname, purchasetime, sum(price) over (partition by username order by productname desc, username ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW) as cum_sales
                from sales;
                """,
        """
                select username, productname, purchasetime, sum(price) over (partition by username order by productname desc, username ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW)
                from sales;
                """ 
    )]
    [InlineData("""
                select s.username, productname, s.purchasetime, sum(s.price) over (partition by s.username order by s.productname desc, username ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW) as cum_sales
                from sales as s;
                """,
        """
                select sales.username, productname, sales.purchasetime, sum(sales.price) over (partition by sales.username order by sales.productname desc, username ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW)
                from sales;
                """ 
    )]
    [InlineData("""
                SELECT SUM("*" + "123"), "*", "*" + 100, "321"."user" as U123, 123
                FROM "123" as "321"
                group by "*", "user"
                """,
        """
                SELECT SUM("*" + "123"), "*", "*" + 100, "123"."user", 123
                FROM "123"
                group by "*", "user"
                """ 
    )]
    [InlineData("""
                SELECT 123 FROM abc
                """,
        """
                SELECT 123 FROM abc
                """ 
    )]
    [InlineData("""
                SELECT price + 123, SUM(price * 0.25) as Discounts
                FROM sales
                GROUP BY price
                """,
        """
                SELECT price + 123, SUM(price * 0.25)
                FROM sales
                GROUP BY price
                """ 
    )]
    [InlineData("""
                SELECT _2025 FROM abc
                """,
        """
                SELECT _2025 FROM abc
                """ 
    )]
    [InlineData("""
                SELECT 123 + 123 + price FROM sales 
                """,
        """
                SELECT 123 + 123 + price FROM sales
                """ 
    )]
    [InlineData("""
                SELECT 
                    SUM("!@#" + "456") AS "sum$1",
                    "weird column" AS " ",
                    "abc" || "def" AS "concat",
                    "t1"."col1" AS c1,
                    789 AS "789"
                FROM "table-name" AS "t1"
                GROUP BY "weird column", "col1";
                """,
        """
                SELECT 
                    SUM("!@#" + "456"),
                    "weird column",
                    "abc" || "def",
                    "table-name"."col1",
                    789
                FROM "table-name"
                GROUP BY "weird column", "col1";
                """ 
    )]
    [InlineData("""
                SELECT 
                    SUM("select" + "from") AS "group",
                    "order" AS "by",
                    "x" * 2 AS "join",
                    "a"."where" AS "cond",
                    42 AS answer
                FROM "select" AS "a"
                GROUP BY "order", "where";
                """,
        """
                SELECT 
                    SUM("select" + "from"),
                    "order",
                    "x" * 2,
                    "select"."where",
                    42
                FROM "select"
                GROUP BY "order", "where";
                """ 
    )]
    [InlineData("""
                SELECT 
                    SUM("a.b" + "c.d") AS "sum.dot",
                    "strange-name" AS "alias-1",
                    "x/y" + 10 AS "math?",
                    "tbl.alias"."col:name" AS "colAlias",
                    0 AS "zero"
                FROM "tbl.alias" AS "tbl.alias"
                GROUP BY "strange-name", "col:name";
                """,
        """
                SELECT 
                    SUM("a.b" + "c.d"),
                    "strange-name",
                    "x/y" + 10,
                    "tbl.alias"."col:name",
                    0
                FROM "tbl.alias"
                GROUP BY "strange-name", "col:name";
                """ 
    )]
    [InlineData("""
                SELECT 
                    SUM(("1" + "2") * ("3" + 4)) AS total,
                    "col space",
                    ("*" || "*") AS stars,
                    "x1"."y2" AS z3,
                    999
                FROM "x1" AS "x1"
                GROUP BY "col space", "y2";
                """,
        """
                SELECT 
                    SUM(("1" + "2") * ("3" + 4)),
                    "col space",
                    ("*" || "*"),
                    "x1"."y2",
                    999
                FROM "x1"
                GROUP BY "col space", "y2";
                """ 
    )]
    [InlineData("""
                SELECT 
                    SUM("a"+"b")   "sumAlias",
                    "col1"    cAlias,
                    "col2"+5   "5col",
                    "t"."u"   u1,
                    1   one
                FROM "t" AS t
                GROUP BY "col1", "u";
                """,
        """
                SELECT 
                    SUM("a"+"b"),
                    "col1",
                    "col2"+5,
                    "t"."u",
                    1
                FROM "t"
                GROUP BY "col1", "u";
                """ 
    )]
    [InlineData("""
                SELECT 
                    SUM("1" + "2") AS "3",
                    "4" AS "5",
                    "6" * 7 AS "8",
                    "9"."10" AS "11",
                    12 AS "13"
                FROM "9" AS "9"
                GROUP BY "4", "10";
                """,
        """
                SELECT 
                    SUM("1" + "2"),
                    "4",
                    "6" * 7,
                    "9"."10",
                    12
                FROM "9"
                GROUP BY "4", "10";
                """ 
    )]
    [InlineData("""
                SELECT SUM("*" + col1) AS total_sum, name AS person_name, price + 100 AS adjusted_price, t.user AS u123, 123 AS n
                FROM users AS t
                GROUP BY name, user;
                """,
        """
                SELECT SUM("*" + col1), name, price + 100, users.user, 123
                FROM users
                GROUP BY name, user;
                """ 
    )]
    [InlineData("""
                SELECT SUM(product_id + "123") AS s1, product_name AS pname, "*" + 100 AS weird_math, p.category AS cat_alias, 999 AS x
                FROM products AS p
                GROUP BY product_name, category;
                """,
        """
                SELECT SUM(product_id + "123"), product_name, "*" + 100, products.category, 999
                FROM products
                GROUP BY product_name, category;
                """ 
    )]
    [InlineData("""
                SELECT SUM(amount + tax) AS total, customer AS c, "odd-name" + 1 AS strange_value, o."user" AS u, 0 AS zero
                FROM orders AS o
                GROUP BY customer, "user";
                """,
        """
                SELECT SUM(amount + tax), customer, "odd-name" + 1, orders."user", 0
                FROM orders
                GROUP BY customer, "user";
                """ 
    )]
    [InlineData("""
                SELECT SUM("123" + quantity) AS mixed_sum, item AS i, quantity * 2 AS doubled, s.stock_id AS sid, 42 AS answer
                FROM stock AS s
                GROUP BY item, stock_id;
                """,
        """
                SELECT SUM("123" + quantity), item, quantity * 2, stock.stock_id, 42
                FROM stock
                GROUP BY item, stock_id;
                """ 
    )]
    [InlineData("""
                SELECT SUM(a + b) AS sum_ab, normal_col AS nc, "space col" AS sc, x.id AS xid, 7 AS seven
                FROM data_table AS x
                GROUP BY normal_col, id;
                """,
        """
                SELECT SUM(a + b), normal_col, "space col", data_table.id, 7
                FROM data_table
                GROUP BY normal_col, id;
                """ 
    )]
    [InlineData("""
                SELECT SUM(value + "*") AS weird_total, label AS l, score + 5 AS boosted, r.rank AS r1, 55 AS num
                FROM results AS r
                GROUP BY label, rank;
                """,
        """
                SELECT SUM(value + "*"), label, score + 5, results.rank, 55
                FROM results
                GROUP BY label, rank;
                """ 
    )]
    [InlineData("""
                SELECT SUM(emp_id + "001") AS emp_sum, emp_name AS ename, salary + bonus AS total_pay, e.department AS dept, 1 AS one
                FROM employees AS e
                GROUP BY emp_name, department;
                """,
        """
                SELECT SUM(emp_id + "001"), emp_name, salary + bonus, employees.department, 1
                FROM employees
                GROUP BY emp_name, department;
                """ 
    )]
    [InlineData("""
                SELECT SUM("!" + points) AS exclaim_sum, player AS p, level + 10 AS next_level, g.game_id AS gid, 88 AS lucky
                FROM games AS g
                GROUP BY player, game_id;
                """,
        """
                SELECT SUM("!" + points), player, level + 10, games.game_id, 88
                FROM games
                GROUP BY player, game_id;
                """ 
    )]
    [InlineData("""
                SELECT SUM(total + discount) AS final_total, region AS r, "strange*" + 3 AS calc, s.sales_rep AS rep, 314 AS pi
                FROM sales AS s
                GROUP BY region, sales_rep;
                """,
        """
                SELECT SUM(total + discount), region, "strange*" + 3, sales.sales_rep, 314
                FROM sales
                GROUP BY region, sales_rep;
                """ 
    )]
    [InlineData("""
                SELECT SUM(base + "bonus") AS comp, username AS uname, age + 1 AS next_age, a.role AS r, 2 AS two
                FROM accounts AS a
                GROUP BY username, role;
                """,
        """
                SELECT SUM(base + "bonus"), username, age + 1, accounts.role, 2
                FROM accounts
                GROUP BY username, role;
                """ 
    )]
    [InlineData("""
                SELECT SUM(price) OVER (PARTITION BY category ORDER BY id) AS running_sum
                FROM sales AS s
                """,
        """
                SELECT SUM(price) OVER (PARTITION BY category ORDER BY id)
                FROM sales
                """ 
    )]
    [InlineData("""
                SELECT price, SUM(price) OVER (PARTITION BY category) AS cat_total
                FROM sales AS s
                """,
        """
                SELECT price, SUM(price) OVER (PARTITION BY category)
                FROM sales
                """
    )]
    [InlineData("""
                SELECT SUM("price") OVER (PARTITION BY "group" ORDER BY "id") AS total
                FROM "sales" AS s
                """,
        """
                SELECT SUM("price") OVER (PARTITION BY "group" ORDER BY "id")
                FROM "sales"
                """
    )]
    [InlineData("""
                SELECT SUM(price * 2) OVER (PARTITION BY category) AS doubled_sum
                FROM sales AS s
                """,
        """
                SELECT SUM(price * 2) OVER (PARTITION BY category)
                FROM sales
                """
    )]
    [InlineData("""
                SELECT SUM(s.price) OVER (PARTITION BY "category" ORDER BY s.id) AS total_sum
                FROM sales AS s
                """,
        """
                SELECT SUM(sales.price) OVER (PARTITION BY "category" ORDER BY sales.id)
                FROM sales
                """
    )]
    [InlineData("""
                SELECT 
                    SUM(price) OVER (PARTITION BY category) AS total,
                    AVG(price) OVER (PARTITION BY category ORDER BY id) AS avg_price
                FROM sales AS s
                """,
        """
                SELECT 
                    SUM(price) OVER (PARTITION BY category),
                    AVG(price) OVER (PARTITION BY category ORDER BY id)
                FROM sales
                """
    )]
    [InlineData("""
                SELECT ROW_NUMBER() OVER (PARTITION BY category ORDER BY id) AS rn
                FROM sales AS s
                """,
        """
                SELECT ROW_NUMBER() OVER (PARTITION BY category ORDER BY id)
                FROM sales
                """
    )]
    [InlineData("""
                SELECT ROW_NUMBER() OVER (PARTITION BY "group" ORDER BY "user") AS r
                FROM "123" AS "t1"
                """,
        """
                SELECT ROW_NUMBER() OVER (PARTITION BY "group" ORDER BY "user")
                FROM "123"
                """
    )]
    [InlineData("""
                SELECT SUM(price) OVER (ORDER BY id) AS running_total
                FROM sales AS s
                """,
        """
                SELECT SUM(price) OVER (ORDER BY id)
                FROM sales
                """
    )]
    [InlineData("""
                SELECT SUM(price) OVER (PARTITION BY category) AS total
                FROM sales AS s
                """,
        """
                SELECT SUM(price) OVER (PARTITION BY category)
                FROM sales
                """
    )]
    [InlineData("""
                SELECT SUM("price" + tax) OVER (PARTITION BY category ORDER BY "id") AS total_sum
                FROM sales AS s
                """,
        """
                SELECT SUM("price" + tax) OVER (PARTITION BY category ORDER BY "id")
                FROM sales
                """
    )]
    [InlineData("""
                SELECT id, category, SUM(price) OVER (PARTITION BY category ORDER BY id) AS total
                FROM sales AS s
                """,
        """
                SELECT id, category, SUM(price) OVER (PARTITION BY category ORDER BY id)
                FROM sales
                """
    )]
    // [InlineData("""
    //             xxx
    //             """,
    //     """
    //             xxx
    //             """ 
    // )]
    // [InlineData("""
    //             xxx
    //             """,
    //     """
    //             xxx
    //             """ 
    // )]
    // [InlineData("""
    //             xxx
    //             """,
    //     """
    //             xxx
    //             """ 
    // )]
    // [InlineData("""
    //             xxx
    //             """,
    //     """
    //             xxx
    //             """ 
    // )]
    // [InlineData("""
    //             xxx
    //             """,
    //     """
    //             xxx
    //             """ 
    // )]
    // [InlineData("""
    //             xxx
    //             """,
    //     """
    //             xxx
    //             """ 
    // )]
    // [InlineData("""
    //             xxx
    //             """,
    //     """
    //             xxx
    //             """ 
    // )]
    // //----------------------
    // [InlineData("""
    //             xxx
    //             """,
    //     """
    //             xxx
    //             """ 
    // )]
    // [InlineData("""
    //             xxx
    //             """,
    //     """
    //             xxx
    //             """ 
    // )]
    // [InlineData("""
    //             xxx
    //             """,
    //     """
    //             xxx
    //             """ 
    // )]
    // [InlineData("""
    //             xxx
    //             """,
    //     """
    //             xxx
    //             """ 
    // )]
    // [InlineData("""
    //             xxx
    //             """,
    //     """
    //             xxx
    //             """ 
    // )]
    // [InlineData("""
    //             xxx
    //             """,
    //     """
    //             xxx
    //             """ 
    // )]
    // [InlineData("""
    //             xxx
    //             """,
    //     """
    //             xxx
    //             """ 
    // )]
    // [InlineData("""
    //             xxx
    //             """,
    //     """
    //             xxx
    //             """ 
    // )]
    // [InlineData("""
    //             xxx
    //             """,
    //     """
    //             xxx
    //             """ 
    // )]
    // [InlineData("""
    //             xxx
    //             """,
    //     """
    //             xxx
    //             """ 
    // )]
    // [InlineData("""
    //             xxx
    //             """,
    //     """
    //             xxx
    //             """ 
    // )]
    // [InlineData("""
    //             xxx
    //             """,
    //     """
    //             xxx
    //             """ 
    // )]
    
    public void Test(string query, string expected)
    {
        var actual = new AliasReplacer().ReplaceAliases(query);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ReplaceAliases_ClearsStateBetweenCallsOnTheSameInstance()
    {
        var replacer = new AliasReplacer();

        var first = replacer.ReplaceAliases("""
                                           SELECT p.price
                                           FROM product p
                                           """);
        var second = replacer.ReplaceAliases("""
                                            SELECT price
                                            FROM purchase
                                            """);

        Assert.Equal("""
                     SELECT product.price
                     FROM product
                     """.Replace("\n", Environment.NewLine), first);
        Assert.Equal("""
                     SELECT price
                     FROM purchase
                     """.Replace("\n", Environment.NewLine), second);
    }

    [Fact]
    public void RemoveSelectAliases_RemovesOnlySelectAliases()
    {
        var actual = new AliasReplacer().RemoveSelectAliases("""
                                                            SELECT p.price AS price_alias, p.name display_name
                                                            FROM product AS p
                                                            WHERE p.price > 10
                                                            """);

        Assert.Equal("""
                     SELECT p.price, p.name
                     FROM product AS p
                     WHERE p.price > 10
                     """, actual);
    }
}
