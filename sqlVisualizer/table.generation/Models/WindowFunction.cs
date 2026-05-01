using System.Text.RegularExpressions;

namespace visualizer.Models;

public record Order(string ColumnName, bool IsAscending);
public class WindowFunction
{
    public required string SQL { get; init; }
    public required string Function { get; init; }
    public required string Argument { get; init; }
    public required string Extra { get; init; }
    public required List<string> PartitionNames { get; init; }
    public required List<Order> Orders { get; init; }

    public static WindowFunction FromString(string windowFunction)
    {
        var overIndex = IndexOfPhraseAtTopLevel(windowFunction, "OVER");
        if (overIndex < 0)
            throw new ArgumentException($"No OVER clause found in window function: {windowFunction}", nameof(windowFunction));

        var invocationSection = windowFunction[..overIndex].Trim();
        var overSection = windowFunction[(overIndex + "OVER".Length)..].Trim();

        var (function, argument, extra) = ParseInvocation(invocationSection);
        var (partitions, orders) = ParseOverSection(overSection);

        return new WindowFunction
        {
            SQL = windowFunction,
            Function = function,
            Argument = argument,
            Extra = extra,
            PartitionNames = partitions,
            Orders = orders
        };
    }

    private static (string Function, string Argument, string Extra) ParseInvocation(string invocation)
    {
        var openingParen = invocation.IndexOf('(');
        if (openingParen < 0)
            throw new ArgumentException($"Invalid window function invocation: {invocation}", nameof(invocation));

        var closingParen = FindMatchingParen(invocation, openingParen);
        if (closingParen < 0)
            throw new ArgumentException($"Unbalanced parentheses in invocation: {invocation}", nameof(invocation));

        var function = invocation[..openingParen].Trim();
        var argsContent = invocation[(openingParen + 1)..closingParen].Trim();
        var args = SplitTopLevel(argsContent, ',');

        var argument = args.Count > 0 ? args[0] : string.Empty;
        var extra = args.Count > 1 ? string.Join(", ", args.Skip(1)) : string.Empty;
        return (function, argument, extra);
    }

    private static (List<string> Partitions, List<Order> Orders) ParseOverSection(string overSection)
    {
        if (!overSection.StartsWith("("))
        {
            // Named windows (OVER some_window) are not expanded here.
            return ([], []);
        }

        var closingParen = FindMatchingParen(overSection, 0);
        if (closingParen < 0)
            throw new ArgumentException($"Unbalanced OVER clause: {overSection}", nameof(overSection));

        var spec = overSection[1..closingParen].Trim();
        if (string.IsNullOrWhiteSpace(spec))
            return ([], []);

        var partitionIndex = IndexOfPhraseAtTopLevel(spec, "PARTITION BY");
        var orderIndex = IndexOfPhraseAtTopLevel(spec, "ORDER BY");

        var frameIndexes = new[]
        {
            IndexOfPhraseAtTopLevel(spec, "ROWS"),
            IndexOfPhraseAtTopLevel(spec, "RANGE"),
            IndexOfPhraseAtTopLevel(spec, "GROUPS"),
            IndexOfPhraseAtTopLevel(spec, "EXCLUDE")
        };

        var partitions = new List<string>();
        if (partitionIndex >= 0)
        {
            var start = partitionIndex + "PARTITION BY".Length;
            var end = EarliestPositiveIndexAfter(start, orderIndex, frameIndexes);
            var partitionText = Slice(spec, start, end).Trim();
            partitions = SplitTopLevel(partitionText, ',');
        }

        var orders = new List<Order>();
        if (orderIndex >= 0)
        {
            var start = orderIndex + "ORDER BY".Length;
            var end = EarliestPositiveIndexAfter(start, -1, frameIndexes);
            var orderText = Slice(spec, start, end).Trim();
            orders = OrdersFromString(orderText);
        }

        return (partitions, orders);
    }

    private static List<Order> OrdersFromString(string ordersString)
    {
        List<Order> results = [];
        foreach (var order in SplitTopLevel(ordersString, ','))
        {
            var isAscending = !Regex.IsMatch(order, @"\bDESC\b", RegexOptions.IgnoreCase);
            var noNullOrdering = Regex.Replace(order, @"\s+NULLS\s+(FIRST|LAST)\s*$", string.Empty,
                RegexOptions.IgnoreCase);
            var expression = Regex.Replace(noNullOrdering, @"\s+(ASC|DESC)\s*$", string.Empty,
                RegexOptions.IgnoreCase).Trim();
            if (expression.Length > 0)
                results.Add(new Order(expression, isAscending));
        }

        return results;
    }

    private static List<string> SplitTopLevel(string input, char separator)
    {
        var values = new List<string>();
        if (string.IsNullOrWhiteSpace(input))
            return values;

        var depth = 0;
        var start = 0;
        var i = 0;
        while (i < input.Length)
        {
            var c = input[i];
            if (c == '\'')
            {
                i = SkipQuoted(input, i, '\'') + 1;
                continue;
            }

            if (c == '"')
            {
                i = SkipQuoted(input, i, '"') + 1;
                continue;
            }

            if (c == '(')
            {
                depth++;
            }
            else if (c == ')' && depth > 0)
            {
                depth--;
            }
            else if (c == separator && depth == 0)
            {
                var value = input[start..i].Trim();
                if (value.Length > 0)
                    values.Add(value);
                start = i + 1;
            }

            i++;
        }

        var tail = input[start..].Trim();
        if (tail.Length > 0)
            values.Add(tail);
        return values;
    }

    private static int SkipQuoted(string input, int start, char quote)
    {
        var i = start + 1;
        while (i < input.Length)
        {
            if (input[i] == quote)
            {
                if (i + 1 < input.Length && input[i + 1] == quote)
                {
                    i += 2;
                    continue;
                }

                return i;
            }

            i++;
        }

        return input.Length - 1;
    }

    private static int FindMatchingParen(string input, int openIndex)
    {
        var depth = 0;
        var i = openIndex;
        while (i < input.Length)
        {
            var c = input[i];
            if (c == '\'')
            {
                i = SkipQuoted(input, i, '\'');
            }
            else if (c == '"')
            {
                i = SkipQuoted(input, i, '"');
            }
            else if (c == '(')
            {
                depth++;
            }
            else if (c == ')')
            {
                depth--;
                if (depth == 0)
                    return i;
            }

            i++;
        }

        return -1;
    }

    private static int IndexOfPhraseAtTopLevel(string input, string phrase)
    {
        if (string.IsNullOrWhiteSpace(input) || string.IsNullOrWhiteSpace(phrase))
            return -1;

        var i = 0;
        var depth = 0;
        while (i < input.Length)
        {
            var c = input[i];
            if (c == '\'')
            {
                i = SkipQuoted(input, i, '\'') + 1;
                continue;
            }

            if (c == '"')
            {
                i = SkipQuoted(input, i, '"') + 1;
                continue;
            }

            if (c == '(')
            {
                depth++;
                i++;
                continue;
            }

            if (c == ')' && depth > 0)
            {
                depth--;
                i++;
                continue;
            }

            if (depth == 0 && StartsWithPhrase(input, i, phrase) && IsWordBoundary(input, i - 1) &&
                IsWordBoundary(input, i + phrase.Length))
            {
                return i;
            }

            i++;
        }

        return -1;
    }

    private static int EarliestPositiveIndexAfter(int start, int preferredEnd, IEnumerable<int> candidates)
    {
        var options = candidates.Where(index => index > start).ToList();
        if (preferredEnd > start)
            options.Add(preferredEnd);
        return options.Count == 0 ? -1 : options.Min();
    }

    private static string Slice(string input, int start, int end)
    {
        if (start < 0 || start >= input.Length)
            return string.Empty;
        if (end < 0 || end > input.Length)
            end = input.Length;
        if (end <= start)
            return string.Empty;
        return input[start..end];
    }

    private static bool StartsWithPhrase(string input, int index, string phrase)
    {
        if (index < 0 || index + phrase.Length > input.Length)
            return false;
        return input.AsSpan(index, phrase.Length).Equals(phrase.AsSpan(), StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsWordBoundary(string input, int index)
    {
        if (index < 0 || index >= input.Length)
            return true;
        var c = input[index];
        return !char.IsLetterOrDigit(c) && c != '_';
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