using Visualizer;
using visualizer.Repositories;

namespace TestProject1;

[Collection("DuckDb seeded")]
public class VisulisationGenerationTest : IClassFixture<DuckDbFixture>
{
    private readonly VisualisationsGenerator generator;

    public VisulisationGenerationTest(DuckDbFixture fixture)
    {
        generator = new VisualisationsGenerator(
            new SQLDecomposer(),
            new TableGenerator(new SQLExecutor(fixture.CreateConnection()), new TableOriginColumnsGenerator()),
            new TableOriginColumnsGenerator(),
            new AliasReplacer());
    }

    // [Theory]
    // [InlineData("""
    //             
    //             """)]
    // public void Template(string query)
    // {
    //     TestQuery(query);
    // }

    [Theory]
    [InlineData("""
                SELECT shift.day FROM shift
                """)]
    [InlineData("""
                SELECT day FROM shift
                """)]
    [InlineData("""
                SELECT * FROM shift
                """)]
    [InlineData("""
                SELECT shift.* FROM shift
                """)]
    [InlineData("""
                SELECT product.*, purchase.*
                FROM purchase
                JOIN product  on purchase.productname = product.productname
                """)]
    [InlineData("""
                SELECT "*"
                FROM "123"
                """)]
    [InlineData("""
                SELECT "123"."*"
                FROM "123"
                """)]
    [InlineData("""
                SELECT product.*, price, purchase.*
                FROM purchase
                JOIN product  on purchase.productname = product.productname
                """)]
    public void Select(string query)
    {
        TestQuery(query);
    }

    [Theory]
    [InlineData("""
                SELECT productname, username FROM purchase
                where username == 'Martin'
                """)]
    [InlineData("""
                SELECT productname, username FROM purchase
                where username LIKE 'M%'
                """)]
    [InlineData("""
                SELECT * FROM shift
                GROUP BY day, cashier
                """
        )]
    public void Where(string query)
    {
        TestQuery(query);
    }

    [Theory]
    [InlineData("""
                SELECT * FROM product
                JOIN purchase ON product.productname = purchase.productname
                """)]
    [InlineData("""
                SELECT * FROM product
                INNER JOIN purchase ON product.productname = purchase.productname
                """)]
    [InlineData("""
                SELECT * FROM product
                LEFT JOIN purchase ON product.productname = purchase.productname
                """)]
    [InlineData("""
                SELECT * FROM purchase
                RIGHT JOIN product ON product.productname = purchase.productname
                """)]
    [InlineData("""
                SELECT * FROM product
                FULL JOIN purchase ON product.productname = purchase.productname
                """)]
    [InlineData("""
                SELECT * FROM product
                FULL JOIN user ON product.productname = user.username
                """)]
    [InlineData("""
                SELECT product.productname, purchase.purchasetime, user.username FROM product
                JOIN purchase ON product.productname = purchase.productname
                JOIN user on purchase.username = user.username
                """)]
    public void Join(string query)
    {
        TestQuery(query);
    }

    [Theory]
    [InlineData("""
                SELECT productname FROM purchase 
                GROUP BY productname
                """)]
    [InlineData("""
                SELECT productname, username FROM purchase 
                GROUP BY productname, purchase.username
                """)]
    public void GroupBy(string query)
    {
        TestQuery(query);
    }
    
    [Theory]
    [InlineData("""
                SELECT productname, count() 
                FROM purchase 
                GROUP BY productname
                HAVING COUNT() > 2
                """)]
    [InlineData("""
                SELECT productname, count() 
                FROM purchase 
                GROUP BY productname
                HAVING COUNT() between 1 and 3
                """)]
    [InlineData("""
                SELECT productname, count() 
                FROM purchase 
                GROUP BY productname
                HAVING COUNT() != 1 or COUNT(*) != 3
                """)]
    [InlineData("""
                SELECT username, SUM(price)
                FROM purchase
                JOIN product ON purchase.productname = product.productname
                GROUP BY username
                HAVING SUM(price) > 1000 or SUM(price) < 500
                """)]
    [InlineData("""
                SELECT username
                FROM purchase pu
                JOIN product pr ON pu.productname = pr.productname
                GROUP BY username
                HAVING SUM(price * 300) > 1000 or SUM(price + 100) < 500
                """)]
    public void Having(string query)
    {
        TestQuery(query);
    }

    [Theory]
    [InlineData("""
                SELECT coUnT() FROM purchase
                """)]
    [InlineData("""
                SELECT COUNT(username) FROM purchase
                """)]
    [InlineData("""
                SELECT username, SUM(price)
                FROM purchase
                JOIN product ON purchase.productname = product.productname
                GROUP BY username
                """)]
    [InlineData("""
                SELECT username, AVG(price)
                FROM purchase
                JOIN product ON purchase.productname = product.productname
                GROUP BY username
                """)]
    [InlineData("""
                SELECT username, Min(price)
                FROM purchase
                JOIN product ON purchase.productname = product.productname
                GROUP BY username
                """)]
    [InlineData("""
                SELECT username, MAX(price)
                FROM purchase
                JOIN product ON purchase.productname = product.productname
                GROUP BY username
                """)]
    [InlineData("""
                SELECT username, SUM(price + 123)
                FROM purchase
                JOIN product ON purchase.productname = product.productname
                GROUP BY username
                """)]
    [InlineData("""
                SELECT username, AVG(price - 123)
                FROM purchase
                JOIN product ON purchase.productname = product.productname
                GROUP BY username
                """)]
    [InlineData("""
                SELECT username, Min(price / 123)
                FROM purchase
                JOIN product ON purchase.productname = product.productname
                GROUP BY username
                """)]
    [InlineData("""
                SELECT username, MAX(price * 123)
                FROM purchase
                JOIN product ON purchase.productname = product.productname
                GROUP BY username
                """)]
    [InlineData("""
                SELECT username, SUM(p1.price + p2.price), AVG(p1.price - p2.price), MIN(p1.price * p2.price), MAX(p1.price / p2.price)
                FROM purchase
                JOIN product p1 ON purchase.productname = p1.productname
                JOIN product p2 ON purchase.productname = p2.productname
                GROUP BY username
                """)]
    [InlineData("""
                SELECT SUM("*" + "123"), "*", "321"."user" as U123
                FROM "123" as "321"
                group by "*", "user"
                """)]
    public void AggreGateFunctions(string query)
    {
         TestQuery(query);
    }

    [Theory]
    [InlineData("""
                SELECT productname, COUNT() FROM purchase 
                GROUP BY productname
                """)]
    [InlineData("""
                SELECT productname, user.email, COUNT() FROM purchase
                JOIN user ON user.username = purchase.username
                GROUP BY productname, user.email
                """)]
    [InlineData("""
                SELECT "username", MAX("price")
                FROM "purchase"
                JOIN "product" ON "purchase".productname = product."productname"
                GROUP BY "username"
                """)]
    public void Combination(string query)
    {
        TestQuery(query);
    }

    [Theory]
    [InlineData("""
                SELECT turbine_id, production_date, power_output,
                       SUM(power_output) OVER (PARTITION BY turbine_id ORDER BY production_date)
                FROM wind_turbine_production
                """)]
    [InlineData("""
                SELECT turbine_id, production_date, power_output,
                       AVG(power_output) OVER (PARTITION BY turbine_id ORDER BY production_date)
                FROM wind_turbine_production
                """)]
    [InlineData("""
                SELECT turbine_id, production_date, power_output,
                       MIN(power_output) OVER (PARTITION BY turbine_id ORDER BY production_date)
                FROM wind_turbine_production
                """)]
    [InlineData("""
                SELECT turbine_id, production_date, power_output,
                       MAX(power_output) OVER (PARTITION BY turbine_id ORDER BY production_date)
                FROM wind_turbine_production
                """)]
    [InlineData("""
                SELECT turbine_id, production_date, power_output,
                       COUNT(power_output) OVER (PARTITION BY turbine_id ORDER BY production_date)
                FROM wind_turbine_production
                """)]
    [InlineData("""
                SELECT turbine_id, production_date, power_output,
                       SUM(power_output) OVER (ORDER BY production_date)
                FROM wind_turbine_production
                """)]
    public void AggregateWindowFunctions(string query)
    {
        TestQuery(query);
    }

    [Theory]
    [InlineData("""
                SELECT turbine_id, production_date, power_output,
                       ROW_NUMBER() OVER (PARTITION BY turbine_id ORDER BY production_date)
                FROM wind_turbine_production
                """)]
    [InlineData("""
                SELECT turbine_id, production_date, power_output,
                       RANK() OVER (PARTITION BY turbine_id ORDER BY power_output DESC)
                FROM wind_turbine_production
                """)]
    [InlineData("""
                SELECT turbine_id, production_date, power_output,
                       DENSE_RANK() OVER (PARTITION BY turbine_id ORDER BY power_output DESC)
                FROM wind_turbine_production
                """)]
    [InlineData("""
                SELECT turbine_id, power_output,
                       NTILE(3) OVER (ORDER BY power_output)
                FROM wind_turbine_production
                """)]
    public void RankingWindowFunctions(string query)
    {
        TestQuery(query);
    }

    [Theory]
    [InlineData("""
                SELECT turbine_id, production_date, power_output,
                       LAG(power_output) OVER (PARTITION BY turbine_id ORDER BY production_date)
                FROM wind_turbine_production
                """)]
    [InlineData("""
                SELECT turbine_id, production_date, power_output,
                       LAG(power_output, 2) OVER (PARTITION BY turbine_id ORDER BY production_date)
                FROM wind_turbine_production
                """)]
    [InlineData("""
                SELECT turbine_id, production_date, power_output,
                       LEAD(power_output) OVER (PARTITION BY turbine_id ORDER BY production_date)
                FROM wind_turbine_production
                """)]
    [InlineData("""
                SELECT turbine_id, production_date, power_output,
                       FIRST_VALUE(power_output) OVER (PARTITION BY turbine_id ORDER BY production_date)
                FROM wind_turbine_production
                """)]
    [InlineData("""
                SELECT turbine_id, production_date, power_output,
                       LAST_VALUE(power_output) OVER (PARTITION BY turbine_id ORDER BY production_date)
                FROM wind_turbine_production
                """)]
    [InlineData("""
                SELECT turbine_id, production_date, power_output,
                       NTH_VALUE(power_output, 2) OVER (PARTITION BY turbine_id ORDER BY production_date)
                FROM wind_turbine_production
                """)]
    public void ValueWindowFunctions(string query)
    {
        TestQuery(query);
    }

    private void TestQuery(string query)
    {
        var visualisations = generator.Generate(query);
        Assert.True(visualisations.Count > 0);
    }
}