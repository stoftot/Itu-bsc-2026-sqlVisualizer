using visualizer.Models;

namespace TestProject1;

public class WindowFunctionParsingTest
{
    [Fact]
    public void BasicAggregateWindowFunction_IsParsed()
    {
        var sql = "SUM(amount) OVER (PARTITION BY customer_id ORDER BY created_at DESC)";

        var parsed = WindowFunction.FromString(sql);

        Assert.Equal("SUM", parsed.Function);
        Assert.Equal("amount", parsed.Argument);
        Assert.Equal(string.Empty, parsed.Extra);
        Assert.Equal(["customer_id"], parsed.PartitionNames);
        Assert.Single(parsed.Orders);
        Assert.Equal("created_at", parsed.Orders[0].ColumnName);
        Assert.False(parsed.Orders[0].IsAscending);
    }

    [Fact]
    public void ValueWindowFunction_WithOffsetAndDefault_IsParsed()
    {
        var sql = "LAG(salary, 2, 0) OVER (PARTITION BY dept ORDER BY id ASC)";

        var parsed = WindowFunction.FromString(sql);

        Assert.Equal("LAG", parsed.Function);
        Assert.Equal("salary", parsed.Argument);
        Assert.Equal("2, 0", parsed.Extra);
        Assert.Equal(["dept"], parsed.PartitionNames);
        Assert.Single(parsed.Orders);
        Assert.Equal("id", parsed.Orders[0].ColumnName);
        Assert.True(parsed.Orders[0].IsAscending);
    }

    [Fact]
    public void PartitionAndOrder_IgnoreCommasInsideNestedExpressions()
    {
        var sql =
            "SUM(COALESCE(amount, 0)) OVER (PARTITION BY date_trunc('day', ts), shop_id ORDER BY COALESCE(priority, 0) DESC, ts)";

        var parsed = WindowFunction.FromString(sql);

        Assert.Equal("SUM", parsed.Function);
        Assert.Equal("COALESCE(amount, 0)", parsed.Argument);
        Assert.Equal(["date_trunc('day', ts)", "shop_id"], parsed.PartitionNames);

        Assert.Equal(2, parsed.Orders.Count);
        Assert.Equal("COALESCE(priority, 0)", parsed.Orders[0].ColumnName);
        Assert.False(parsed.Orders[0].IsAscending);
        Assert.Equal("ts", parsed.Orders[1].ColumnName);
        Assert.True(parsed.Orders[1].IsAscending);
    }

    [Fact]
    public void FrameClause_DoesNotLeakIntoOrderBy()
    {
        var sql = "ROW_NUMBER() OVER (PARTITION BY grp ORDER BY ts DESC ROWS BETWEEN 1 PRECEDING AND CURRENT ROW)";

        var parsed = WindowFunction.FromString(sql);

        Assert.Equal("ROW_NUMBER", parsed.Function);
        Assert.Equal(string.Empty, parsed.Argument);
        Assert.Equal(["grp"], parsed.PartitionNames);
        Assert.Single(parsed.Orders);
        Assert.Equal("ts", parsed.Orders[0].ColumnName);
        Assert.False(parsed.Orders[0].IsAscending);
    }

    [Fact]
    public void NamedWindow_ParsesFunctionAndArguments()
    {
        var sql = "AVG(price) OVER w";

        var parsed = WindowFunction.FromString(sql);

        Assert.Equal("AVG", parsed.Function);
        Assert.Equal("price", parsed.Argument);
        Assert.Empty(parsed.PartitionNames);
        Assert.Empty(parsed.Orders);
    }
}

