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
    public void Test(string query, string expected)
    {
        var actual = new AliasReplacer().ReplaceAliases(query);
        Assert.Equal(expected, actual);
    }
}