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
    //
    // //----------------------
    // [InlineData("""
    //             xxx
    //             """,
    //     """
    //     xxx
    //     """ 
    // )]
    // [InlineData("""
    //             xxx
    //             """,
    //     """
    //     xxx
    //     """ 
    // )]
    // [InlineData("""
    //             xxx
    //             """,
    //     """
    //     xxx
    //     """ 
    // )]
    // [InlineData("""
    //             xxx
    //             """,
    //     """
    //     xxx
    //     """ 
    // )]
    // [InlineData("""
    //             xxx
    //             """,
    //     """
    //     xxx
    //     """ 
    // )]
    // [InlineData("""
    //             xxx
    //             """,
    //     """
    //     xxx
    //     """ 
    // )]
    // [InlineData("""
    //             xxx
    //             """,
    //     """
    //     xxx
    //     """ 
    // )]
    // [InlineData("""
    //             xxx
    //             """,
    //     """
    //     xxx
    //     """ 
    // )]
    // [InlineData("""
    //             xxx
    //             """,
    //     """
    //     xxx
    //     """ 
    // )]
    // [InlineData("""
    //             xxx
    //             """,
    //     """
    //     xxx
    //     """ 
    // )]
    // [InlineData("""
    //             xxx
    //             """,
    //     """
    //     xxx
    //     """ 
    // )]
    // [InlineData("""
    //             xxx
    //             """,
    //     """
    //     xxx
    //     """ 
    // )]
    
    public void Test(string query, string expected)
    {
        var actual = new AliasReplacer().ReplaceAliases(query);
        Assert.Equal(expected, actual);
    }
}