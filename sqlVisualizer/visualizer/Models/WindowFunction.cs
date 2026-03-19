using visualizer.Utility;

namespace visualizer.Models;

public record Order(string ColumnName, bool IsAscending);
public class WindowFunction
{
    public required string SQL { get; init; }
    public required string Function { get; init; }
    public required string Argument { get; init; }
    public required List<string> PartitionNames { get; init; }
    public required List<Order> Orders { get; init; }
    
    public static WindowFunction FromString(string windowFunction)
    {
        var match = UtilRegex.Match(windowFunction, UtilRegex.NamedWindowFunctionPattern);
        return new WindowFunction
        {
            SQL = windowFunction,
            Function = match.Groups["function"].Value,
            Argument = match.Groups["argument"].Value,
            PartitionNames = match.Groups["partitions"].Success
                ? match.Groups["partitions"].Value.Split(',').ToList()
                : [],
            Orders = match.Groups["orders"].Success ? OrdersFromString(match.Groups["orders"].Value) : []
        };
    }

    private static List<Order> OrdersFromString(string ordersString)
    {
        List<Order> results = [];
        List<string> orders = ordersString.Split(',').ToList();
        foreach (var order in orders)
        {
            if (order.Contains(" ", StringComparison.InvariantCultureIgnoreCase))
            {
                if (order.Contains(" DESC", StringComparison.InvariantCultureIgnoreCase))
                {
                    results.Add(new Order(order.Split(" ")[0], false));
                }
                else
                {
                    results.Add(new Order(order.Split(" ")[0], true));
                }
            }
            else
            {
                results.Add(new Order(order, true));
            }
        }
        return results;
    }

    public void Print()
    {
        Console.WriteLine("Printing the window function: " + this);
        Console.WriteLine("Function: " + Function);
        Console.WriteLine("argument: " + Argument);
        Console.WriteLine("Partitions: " + (PartitionNames.Count > 0 ? string.Join(", ", PartitionNames) : "None"));
        Console.WriteLine("orders: " + (Orders.Count > 0 ? string.Join(", ", Orders) : "None"));
    }
    
    public override string ToString()
    {
        return SQL;
    }
}