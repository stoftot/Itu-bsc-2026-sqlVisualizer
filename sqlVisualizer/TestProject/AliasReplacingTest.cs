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

    [Theory]
    [InlineData(
        """
        SELECT coffee_name name, sum(cs.price_per_unit * cs.quantity) sale
        FROM coffee_sales cs 
        JOIN coffee_types ct ON cs.coffee_id = ct.coffee_id
        WHERE name not like '%Espresso'
        GROUP BY name
        ORDER BY sale
        """,
        """
        SELECT coffee_name name, sum(cs.price_per_unit * cs.quantity) sale
        FROM coffee_sales cs 
        JOIN coffee_types ct ON cs.coffee_id = ct.coffee_id
        WHERE coffee_name not like '%Espresso'
        GROUP BY coffee_name
        ORDER BY sale
        """
        )]
    [InlineData(
        """
        SELECT coffee_name name, sum(cs.price_per_unit * cs.quantity) sale
        FROM coffee_sales cs 
        JOIN coffee_types ct ON cs.coffee_id = ct.coffee_id
        WHERE name not like '%Espresso'
        GROUP BY name
        ORDER BY sale
        """,
        """
        SELECT coffee_name name, sum(cs.price_per_unit * cs.quantity) sale
        FROM coffee_sales cs 
        JOIN coffee_types ct ON cs.coffee_id = ct.coffee_id
        WHERE coffee_name not like '%Espresso'
        GROUP BY coffee_name
        ORDER BY sale
        """
        )]

[InlineData(
        """
        SELECT product_name pname, category c
        FROM products
        WHERE pname = 'Tea'
        GROUP BY pname, c
        HAVING c <> 'Cold'
        ORDER BY pname
        """,
        """
        SELECT product_name pname, category c
        FROM products
        WHERE product_name = 'Tea'
        GROUP BY product_name, category
        HAVING category <> 'Cold'
        ORDER BY pname
        """
        )]

[InlineData(
        """
        SELECT customer_id cid, count(*) total_orders
        FROM orders
        WHERE cid > 10
        GROUP BY cid
        HAVING total_orders > 3
        ORDER BY total_orders desc
        """,
        """
        SELECT customer_id cid, count(*) total_orders
        FROM orders
        WHERE customer_id > 10
        GROUP BY customer_id
        HAVING count(*) > 3
        ORDER BY total_orders desc
        """
        )]

[InlineData(
        """
        SELECT price * quantity total, product_id pid
        FROM order_lines
        WHERE total > 100
        GROUP BY pid, total
        ORDER BY total
        """,
        """
        SELECT price * quantity total, product_id pid
        FROM order_lines
        WHERE price * quantity > 100
        GROUP BY product_id, price * quantity
        ORDER BY total
        """
        )]

[InlineData(
        """
        SELECT first_name || ' ' || last_name full_name, department dept
        FROM employees
        WHERE full_name like 'A%'
        GROUP BY full_name, dept
        HAVING dept = 'Sales'
        ORDER BY full_name
        """,
        """
        SELECT first_name || ' ' || last_name full_name, department dept
        FROM employees
        WHERE first_name || ' ' || last_name like 'A%'
        GROUP BY first_name || ' ' || last_name, department
        HAVING department = 'Sales'
        ORDER BY full_name
        """
        )]

[InlineData(
        """
        SELECT region r, sum(amount) revenue
        FROM sales
        WHERE r in ('North', 'South')
        GROUP BY r
        HAVING revenue >= 1000
        LIMIT 5
        OFFSET 2
        """,
        """
        SELECT region r, sum(amount) revenue
        FROM sales
        WHERE region in ('North', 'South')
        GROUP BY region
        HAVING sum(amount) >= 1000
        LIMIT 5
        OFFSET 2
        """
        )]

[InlineData(
        """
        SELECT order_date d, customer_id cid, sum(total_price) total_sales
        FROM orders
        WHERE d >= '2025-01-01'
        GROUP BY d, cid
        HAVING total_sales > 500
        ORDER BY d, total_sales
        """,
        """
        SELECT order_date d, customer_id cid, sum(total_price) total_sales
        FROM orders
        WHERE order_date >= '2025-01-01'
        GROUP BY order_date, customer_id
        HAVING sum(total_price) > 500
        ORDER BY d, total_sales
        """
        )]

[InlineData(
        """
        SELECT city town, count(*) visits
        FROM customers
        WHERE town is not null
        GROUP BY town
        HAVING visits > 1
        ORDER BY town asc
        LIMIT 10
        """,
        """
        SELECT city town, count(*) visits
        FROM customers
        WHERE city is not null
        GROUP BY city
        HAVING count(*) > 1
        ORDER BY town asc
        LIMIT 10
        """
        )]

[InlineData(
        """
        SELECT salary * 12 yearly_salary, department dept
        FROM employees
        WHERE yearly_salary > 60000
        GROUP BY yearly_salary, dept
        HAVING dept <> 'HR'
        ORDER BY yearly_salary
        """,
        """
        SELECT salary * 12 yearly_salary, department dept
        FROM employees
        WHERE salary * 12 > 60000
        GROUP BY salary * 12, department
        HAVING department <> 'HR'
        ORDER BY yearly_salary
        """
        )]

[InlineData(
        """
        SELECT category cat, avg(price) avg_price
        FROM products
        WHERE cat <> 'Snacks'
        GROUP BY cat
        HAVING avg_price < 20
        ORDER BY avg_price
        OFFSET 4
        """,
        """
        SELECT category cat, avg(price) avg_price
        FROM products
        WHERE category <> 'Snacks'
        GROUP BY category
        HAVING avg(price) < 20
        ORDER BY avg_price
        OFFSET 4
        """
        )]

[InlineData(
        """
        SELECT u.username uname, u.age years
        FROM users u
        WHERE uname like 'M%'
        AND years >= 18
        GROUP BY uname, years
        ORDER BY uname
        """,
        """
        SELECT u.username uname, u.age years
        FROM users u
        WHERE u.username like 'M%'
        AND u.age >= 18
        GROUP BY u.username, u.age
        ORDER BY uname
        """
        )]

[InlineData(
        """
        SELECT p.productname pname, p.price cost
        FROM product p
        JOIN purchase pu ON pname = pu.productname
        WHERE cost > 50
        GROUP BY pname, cost, pu.username
        HAVING pname <> 'Tea'
        ORDER BY cost desc
        """,
        """
        SELECT p.productname pname, p.price cost
        FROM product p
        JOIN purchase pu ON p.productname = pu.productname
        WHERE p.price > 50
        GROUP BY p.productname, p.price, pu.username
        HAVING p.productname <> 'Tea'
        ORDER BY cost desc
        """
        )]

[InlineData(
        """
        SELECT extract(year from order_date) order_year, count(*) cnt
        FROM orders
        WHERE order_year = 2025
        GROUP BY order_year
        HAVING cnt > 10
        ORDER BY cnt desc
        """,
        """
        SELECT extract(year from order_date) order_year, count(*) cnt
        FROM orders
        WHERE extract(year from order_date) = 2025
        GROUP BY extract(year from order_date)
        HAVING count(*) > 10
        ORDER BY cnt desc
        """
        )]

[InlineData(
        """
        SELECT price + tax final_price, item_name item
        FROM items
        WHERE final_price between 100 and 200
        GROUP BY final_price, item
        HAVING item <> 'Mug'
        ORDER BY item
        """,
        """
        SELECT price + tax final_price, item_name item
        FROM items
        WHERE price + tax between 100 and 200
        GROUP BY price + tax, item_name
        HAVING item_name <> 'Mug'
        ORDER BY item
        """
        )]

[InlineData(
        """
        SELECT department dept, max(salary) top_salary
        FROM employees
        WHERE dept = 'Engineering'
        GROUP BY dept
        HAVING top_salary > 100000
        ORDER BY top_salary
        LIMIT 3
        OFFSET 1
        """,
        """
        SELECT department dept, max(salary) top_salary
        FROM employees
        WHERE department = 'Engineering'
        GROUP BY department
        HAVING max(salary) > 100000
        ORDER BY top_salary
        LIMIT 3
        OFFSET 1
        """
        )]
    [InlineData(
        """
        SELECT productname pname, price cost
        FROM product
        WHERE pname = 'Tea'
        AND cost >= 100
        ORDER BY pname, cost
        """,
        """
        SELECT productname pname, price cost
        FROM product
        WHERE productname = 'Tea'
        AND price >= 100
        ORDER BY pname, cost
        """
        )]

[InlineData(
        """
        SELECT p.productname pname, p.price price_value
        FROM product p
        JOIN purchase pu ON pname = pu.productname
        WHERE price_value > 80
        ORDER BY price_value desc
        """,
        """
        SELECT p.productname pname, p.price price_value
        FROM product p
        JOIN purchase pu ON p.productname = pu.productname
        WHERE p.price > 80
        ORDER BY price_value desc
        """
        )]

[InlineData(
        """
        SELECT productname as pname, price * 1.25 as taxed_price
        FROM product
        WHERE taxed_price > 100
        GROUP BY pname, taxed_price
        ORDER BY taxed_price
        """,
        """
        SELECT productname as pname, price * 1.25 as taxed_price
        FROM product
        WHERE price * 1.25 > 100
        GROUP BY productname, price * 1.25
        ORDER BY taxed_price
        """
        )]

[InlineData(
        """
        SELECT username uname, count(*) purchases
        FROM purchase
        WHERE uname is not null
        GROUP BY uname
        HAVING purchases > 1
        ORDER BY purchases desc
        """,
        """
        SELECT username uname, count(*) purchases
        FROM purchase
        WHERE username is not null
        GROUP BY username
        HAVING count(*) > 1
        ORDER BY purchases desc
        """
        )]

[InlineData(
        """
        SELECT purchasetime ptime, productname pname
        FROM purchase
        WHERE ptime >= TIMESTAMP '2025-02-12 00:00:00'
        GROUP BY ptime, pname
        ORDER BY ptime
        """,
        """
        SELECT purchasetime ptime, productname pname
        FROM purchase
        WHERE purchasetime >= TIMESTAMP '2025-02-12 00:00:00'
        GROUP BY purchasetime, productname
        ORDER BY ptime
        """
        )]

[InlineData(
        """
        SELECT p.productname name, coalesce(p.price, 0) safe_price
        FROM product p
        WHERE safe_price >= 50
        GROUP BY name, safe_price
        HAVING safe_price < 500
        ORDER BY safe_price
        """,
        """
        SELECT p.productname name, coalesce(p.price, 0) safe_price
        FROM product p
        WHERE coalesce(p.price, 0) >= 50
        GROUP BY p.productname, coalesce(p.price, 0)
        HAVING coalesce(p.price, 0) < 500
        ORDER BY safe_price
        """
        )]

[InlineData(
        """
        SELECT p.productname name, length(p.productname) len
        FROM product p
        WHERE len > 3
        GROUP BY name, len
        HAVING len < 20
        ORDER BY len
        """,
        """
        SELECT p.productname name, length(p.productname) len
        FROM product p
        WHERE length(p.productname) > 3
        GROUP BY p.productname, length(p.productname)
        HAVING length(p.productname) < 20
        ORDER BY len
        """
        )]

[InlineData(
        """
        SELECT productname name, upper(productname) upper_name
        FROM product
        WHERE upper_name like 'T%'
        GROUP BY name, upper_name
        ORDER BY upper_name
        """,
        """
        SELECT productname name, upper(productname) upper_name
        FROM product
        WHERE upper(productname) like 'T%'
        GROUP BY productname, upper(productname)
        ORDER BY upper_name
        """
        )]

[InlineData(
        """
        SELECT price + 10 adjusted, productname pname
        FROM product
        WHERE adjusted between 90 and 120
        GROUP BY adjusted, pname
        HAVING adjusted <> 95
        ORDER BY adjusted
        """,
        """
        SELECT price + 10 adjusted, productname pname
        FROM product
        WHERE price + 10 between 90 and 120
        GROUP BY price + 10, productname
        HAVING price + 10 <> 95
        ORDER BY adjusted
        """
        )]

[InlineData(
        """
        SELECT p.productname pname, p.price cost, pu.username uname
        FROM product p
        JOIN purchase pu ON pname = pu.productname AND uname is not null
        WHERE cost > 50
        GROUP BY pname, cost, uname
        ORDER BY uname, cost
        """,
        """
        SELECT p.productname pname, p.price cost, pu.username uname
        FROM product p
        JOIN purchase pu ON p.productname = pu.productname AND pu.username is not null
        WHERE p.price > 50
        GROUP BY p.productname, p.price, pu.username
        ORDER BY uname, cost
        """
        )]

[InlineData(
        """
        SELECT date_trunc('day', purchasetime) sale_day, count(*) cnt
        FROM purchase
        WHERE sale_day >= DATE '2025-02-12'
        GROUP BY sale_day
        HAVING cnt > 0
        ORDER BY sale_day
        """,
        """
        SELECT date_trunc('day', purchasetime) sale_day, count(*) cnt
        FROM purchase
        WHERE date_trunc('day', purchasetime) >= DATE '2025-02-12'
        GROUP BY date_trunc('day', purchasetime)
        HAVING count(*) > 0
        ORDER BY sale_day
        """
        )]

[InlineData(
        """
        SELECT regexp_replace(productname, 'a', 'x') weird_name, price price_value
        FROM product
        WHERE weird_name <> 'Tea'
        AND price_value is not null
        GROUP BY weird_name, price_value
        ORDER BY weird_name
        """,
        """
        SELECT regexp_replace(productname, 'a', 'x') weird_name, price price_value
        FROM product
        WHERE regexp_replace(productname, 'a', 'x') <> 'Tea'
        AND price is not null
        GROUP BY regexp_replace(productname, 'a', 'x'), price
        ORDER BY weird_name
        """
        )]

[InlineData(
        """
        SELECT productname as "name", price as "value"
        FROM product
        WHERE "name" like 'T%'
        GROUP BY "name", "value"
        HAVING "value" > 50
        ORDER BY "value"
        """,
        """
        SELECT productname as "name", price as "value"
        FROM product
        WHERE productname like 'T%'
        GROUP BY productname, price
        HAVING price > 50
        ORDER BY "value"
        """
        )]

[InlineData(
        """
        SELECT "productname" as pname, "price" as cost
        FROM product
        WHERE pname is distinct from 'Coffee'
        GROUP BY pname, cost
        HAVING cost is not null
        ORDER BY cost
        """,
        """
        SELECT "productname" as pname, "price" as cost
        FROM product
        WHERE "productname" is distinct from 'Coffee'
        GROUP BY "productname", "price"
        HAVING "price" is not null
        ORDER BY cost
        """
        )]

[InlineData(
        """
        SELECT p.productname pname, p.price + 1 one_more
        FROM product p
        WHERE not (one_more < 10)
        GROUP BY pname, one_more
        HAVING one_more >= 1
        ORDER BY one_more
        """,
        """
        SELECT p.productname pname, p.price + 1 one_more
        FROM product p
        WHERE not (p.price + 1 < 10)
        GROUP BY p.productname, p.price + 1
        HAVING p.price + 1 >= 1
        ORDER BY one_more
        """
        )]

[InlineData(
        """
        SELECT p.productname pname, p.price cost
        FROM product p
        JOIN purchase pu ON lower(pname) = lower(pu.productname)
        WHERE cost * 2 > 100
        GROUP BY pname, cost
        ORDER BY pname
        """,
        """
        SELECT p.productname pname, p.price cost
        FROM product p
        JOIN purchase pu ON lower(p.productname) = lower(pu.productname)
        WHERE p.price * 2 > 100
        GROUP BY p.productname, p.price
        ORDER BY pname
        """
        )]

[InlineData(
        """
        SELECT p.productname pname, count(*) cnt
        FROM product p
        JOIN purchase pu ON pname = pu.productname
        GROUP BY pname
        HAVING cnt between 1 and 5
        ORDER BY cnt
        LIMIT 10
        OFFSET 2
        """,
        """
        SELECT p.productname pname, count(*) cnt
        FROM product p
        JOIN purchase pu ON p.productname = pu.productname
        GROUP BY p.productname
        HAVING count(*) between 1 and 5
        ORDER BY cnt
        LIMIT 10
        OFFSET 2
        """
        )]

[InlineData(
        """
        SELECT p.productname pname, sum(pu.purchasetime is not null) hits
        FROM product p
        LEFT JOIN purchase pu ON pname = pu.productname
        GROUP BY pname
        HAVING hits > 0
        ORDER BY hits desc
        """,
        """
        SELECT p.productname pname, sum(pu.purchasetime is not null) hits
        FROM product p
        LEFT JOIN purchase pu ON p.productname = pu.productname
        GROUP BY p.productname
        HAVING sum(pu.purchasetime is not null) > 0
        ORDER BY hits desc
        """
        )]

[InlineData(
        """
        SELECT productname pname, price cost
        FROM product
        WHERE (pname = 'Tea' OR pname = 'Small')
        AND cost >= 85
        GROUP BY pname, cost
        ORDER BY pname
        """,
        """
        SELECT productname pname, price cost
        FROM product
        WHERE (productname = 'Tea' OR productname = 'Small')
        AND price >= 85
        GROUP BY productname, price
        ORDER BY pname
        """
        )]

[InlineData(
        """
        SELECT substring(productname, 1, 2) prefix, price cost
        FROM product
        WHERE prefix = 'Te'
        GROUP BY prefix, cost
        HAVING cost > 10
        ORDER BY prefix
        """,
        """
        SELECT substring(productname, 1, 2) prefix, price cost
        FROM product
        WHERE substring(productname, 1, 2) = 'Te'
        GROUP BY substring(productname, 1, 2), price
        HAVING price > 10
        ORDER BY prefix
        """
        )]
    public void General(string query, string expected)
    {
        var actual = new AliasReplacer().ReplaceAliases(query);
        Assert.Equal(expected, actual);
    }
}