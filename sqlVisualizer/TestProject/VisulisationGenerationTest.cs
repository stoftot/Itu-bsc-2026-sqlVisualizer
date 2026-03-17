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
                SELECT username, SUM(price)
                FROM purchase
                JOIN product ON purchase.productname = product.productname
                GROUP BY username
                HAVING SUM(price) > 1000 or SUM(price) < 500
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
                SELECT username, SUM(p1.price + p2.price), AVG(p1.price - p2.price), MIN(p1.price * p2.price), MAX(p1.price / p2.price)
                FROM purchase
                JOIN product p1 ON purchase.productname = p1.productname
                JOIN product p2 ON purchase.productname = p2.productname
                GROUP BY username
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
    public void Combination(string query)
    {
        TestQuery(query);
    }

    private void TestQuery(string query)
    {
        var visualisations = generator.Generate(query);
        Assert.True(visualisations.Count > 0);
    }
}