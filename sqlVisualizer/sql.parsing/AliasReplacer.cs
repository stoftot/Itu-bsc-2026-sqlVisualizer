using System.Text;

namespace inputParsing;

internal class AliasReplacer
{
    public string ReplaceAliases(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
            return sql;

        var tokens = Tokenize(sql);
        if (tokens.Count == 0)
            return sql;

        var aliases = ExtractSelectAliases(sql, tokens);
        if (aliases.Count == 0)
            return sql.TrimEnd();

        return ReplaceAliasUsages(sql, tokens, aliases).TrimEnd();
    }

    private static Dictionary<string, string> ExtractSelectAliases(string sql, List<Token> tokens)
    {
        var aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var selectIndex = FindTopLevelKeyword(tokens, "SELECT");
        var fromIndex = FindTopLevelKeyword(tokens, "FROM");

        if (selectIndex == -1 || fromIndex == -1 || fromIndex <= selectIndex)
            return aliases;

        var itemStart = tokens[selectIndex].End;

        for (var index = selectIndex + 1; index <= fromIndex; index++)
        {
            var isBoundary = index == fromIndex;
            var isComma = !isBoundary && tokens[index].Depth == 0 && tokens[index].Text == ",";

            if (!isBoundary && !isComma)
                continue;

            var itemEnd = isBoundary ? tokens[fromIndex].Start : tokens[index].Start;
            ExtractAliasFromSelectItem(sql, tokens, itemStart, itemEnd, aliases);

            if (!isBoundary)
                itemStart = tokens[index].End;
        }

        return aliases;
    }

    private static void ExtractAliasFromSelectItem(string sql, List<Token> tokens, int itemStart, int itemEnd, Dictionary<string, string> aliases)
    {
        var itemTokens = tokens
            .Where(token => token.Start >= itemStart && token.End <= itemEnd && token.Depth == 0)
            .ToList();

        if (itemTokens.Count <= 1)
            return;

        var aliasToken = itemTokens[^1];
        if (!aliasToken.IsIdentifier)
            return;

        var previousToken = itemTokens[^2];
        var expressionEnd = aliasToken.Start;

        if (IsKeyword(previousToken.Text, "AS"))
        {
            expressionEnd = previousToken.Start;
        }
        else
        {
            var expressionText = sql[itemStart..aliasToken.Start].Trim();
            if (string.IsNullOrWhiteSpace(expressionText) || string.Equals(expressionText, "DISTINCT", StringComparison.OrdinalIgnoreCase))
                return;

            if (!CanPrecedeImplicitAlias(previousToken.Text))
                return;
        }

        var expression = sql[itemStart..expressionEnd].Trim();
        if (string.IsNullOrWhiteSpace(expression))
            return;

        aliases[aliasToken.Text] = expression;
    }

    private static string ReplaceAliasUsages(string sql, List<Token> tokens, Dictionary<string, string> aliases)
    {
        var builder = new StringBuilder(sql.Length);
        var cursor = 0;
        var clause = Clause.None;
        var seenFrom = false;

        for (var index = 0; index < tokens.Count; index++)
        {
            var token = tokens[index];

            if (cursor < token.Start)
                builder.Append(sql, cursor, token.Start - cursor);

            if (token.Depth == 0)
            {
                clause = GetClause(tokens, index, clause);
                if (clause is Clause.From or Clause.Join)
                    seenFrom = true;
            }

            string? replacement = null;
            var hasReplacement = token.IsIdentifier && aliases.TryGetValue(token.Text, out replacement);
            var canReplace = seenFrom &&
                             clause is Clause.On or Clause.Where or Clause.GroupBy or Clause.Having &&
                             hasReplacement &&
                             !IsQualifiedIdentifier(tokens, index) &&
                             !IsFunctionCall(tokens, index);

            builder.Append(canReplace ? replacement : sql[token.Start..token.End]);
            cursor = token.End;
        }

        if (cursor < sql.Length)
            builder.Append(sql, cursor, sql.Length - cursor);

        return builder.ToString();
    }

    private static Clause GetClause(List<Token> tokens, int index, Clause currentClause)
    {
        var token = tokens[index];

        if (IsKeyword(token.Text, "FROM"))
            return Clause.From;

        if (IsJoinStarter(token.Text))
            return Clause.Join;

        if (IsKeyword(token.Text, "ON"))
            return Clause.On;

        if (IsKeyword(token.Text, "WHERE"))
            return Clause.Where;

        if (IsKeyword(token.Text, "GROUP"))
            return Clause.GroupBy;

        if (IsKeyword(token.Text, "HAVING"))
            return Clause.Having;

        if (IsKeyword(token.Text, "ORDER"))
            return Clause.OrderBy;

        if (IsKeyword(token.Text, "LIMIT"))
            return Clause.Limit;

        if (IsKeyword(token.Text, "OFFSET"))
            return Clause.Offset;

        if (IsKeyword(token.Text, "UNION") || IsKeyword(token.Text, "INTERSECT") || IsKeyword(token.Text, "EXCEPT"))
            return Clause.None;

        return currentClause;
    }

    private static bool IsJoinStarter(string text)
    {
        return IsKeyword(text, "JOIN") ||
               IsKeyword(text, "INNER") ||
               IsKeyword(text, "LEFT") ||
               IsKeyword(text, "RIGHT") ||
               IsKeyword(text, "FULL") ||
               IsKeyword(text, "CROSS") ||
               IsKeyword(text, "NATURAL");
    }

    private static bool IsQualifiedIdentifier(List<Token> tokens, int index)
    {
        return index + 1 < tokens.Count && tokens[index + 1].Text == ".";
    }

    private static bool IsFunctionCall(List<Token> tokens, int index)
    {
        return index + 1 < tokens.Count && tokens[index + 1].Text == "(";
    }

    private static bool CanPrecedeImplicitAlias(string text)
    {
        return text != "." &&
               text != "+" &&
               text != "-" &&
               text != "*" &&
               text != "/" &&
               text != "=" &&
               text != ">" &&
               text != "<" &&
               text != "|" &&
               text != "&" &&
               text != "(";
    }

    private static int FindTopLevelKeyword(List<Token> tokens, string keyword)
    {
        for (var index = 0; index < tokens.Count; index++)
        {
            if (tokens[index].Depth == 0 && IsKeyword(tokens[index].Text, keyword))
                return index;
        }

        return -1;
    }

    private static bool IsKeyword(string text, string keyword)
    {
        return text.Equals(keyword, StringComparison.OrdinalIgnoreCase);
    }

    private static List<Token> Tokenize(string sql)
    {
        var tokens = new List<Token>();
        var index = 0;
        var depth = 0;

        while (index < sql.Length)
        {
            var current = sql[index];

            if (char.IsWhiteSpace(current))
            {
                index++;
                continue;
            }

            if (current == '"')
            {
                var start = index++;
                while (index < sql.Length)
                {
                    if (sql[index] == '"' && index + 1 < sql.Length && sql[index + 1] == '"')
                    {
                        index += 2;
                        continue;
                    }

                    if (sql[index] == '"')
                    {
                        index++;
                        break;
                    }

                    index++;
                }

                tokens.Add(new Token(sql[start..index], start, index, depth, true));
                continue;
            }

            if (current == '\'')
            {
                var start = index++;
                while (index < sql.Length)
                {
                    if (sql[index] == '\'' && index + 1 < sql.Length && sql[index + 1] == '\'')
                    {
                        index += 2;
                        continue;
                    }

                    if (sql[index] == '\'')
                    {
                        index++;
                        break;
                    }

                    index++;
                }

                tokens.Add(new Token(sql[start..index], start, index, depth, false));
                continue;
            }

            if (current == '(')
            {
                tokens.Add(new Token("(", index, index + 1, depth, false));
                depth++;
                index++;
                continue;
            }

            if (current == ')')
            {
                depth = Math.Max(0, depth - 1);
                tokens.Add(new Token(")", index, index + 1, depth, false));
                index++;
                continue;
            }

            if (current is '.' or ',' or '+' or '-' or '*' or '/' or '=' or '>' or '<')
            {
                tokens.Add(new Token(sql[index].ToString(), index, index + 1, depth, false));
                index++;
                continue;
            }

            if (char.IsDigit(current))
            {
                var start = index++;
                while (index < sql.Length && char.IsDigit(sql[index]))
                    index++;

                tokens.Add(new Token(sql[start..index], start, index, depth, false));
                continue;
            }

            if (char.IsLetter(current) || current == '_')
            {
                var start = index++;
                while (index < sql.Length && (char.IsLetterOrDigit(sql[index]) || sql[index] == '_'))
                    index++;

                tokens.Add(new Token(sql[start..index], start, index, depth, true));
                continue;
            }

            tokens.Add(new Token(sql[index].ToString(), index, index + 1, depth, false));
            index++;
        }

        return tokens;
    }

    private enum Clause
    {
        None,
        From,
        Join,
        On,
        Where,
        GroupBy,
        Having,
        OrderBy,
        Limit,
        Offset
    }

    private sealed record Token(string Text, int Start, int End, int Depth, bool IsIdentifier);
}
