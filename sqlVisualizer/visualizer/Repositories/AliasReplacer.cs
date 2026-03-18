using System.Text;
using visualizer.Models;

namespace Visualizer;

public class AliasReplacer
{
    private Dictionary<string, string> AliasToTableMap { get; } = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, string> AliasReferenceMap { get; } = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, string> SelectAliasMap { get; } = new(StringComparer.OrdinalIgnoreCase);

    public string ReplaceAliases(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
            return sql;

        AliasToTableMap.Clear();
        AliasReferenceMap.Clear();
        SelectAliasMap.Clear();

        var tokens = Tokenize(sql);
        if (tokens.Count == 0)
            return sql;

        var edits = new List<TextEdit>();
        var tableAliasDefinitions = ExtractTableAliases(sql, tokens);

        PlanTableAliasEdits(tableAliasDefinitions, edits);
        ExtractSelectAliases(sql, tokens, edits);

        var withoutAliasDefinitions = ApplyEdits(sql, edits);
        return ReplaceTableAliasReferences(withoutAliasDefinitions).TrimEnd();
    }

    public string RemoveSelectAliases(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
            return sql;

        AliasToTableMap.Clear();
        AliasReferenceMap.Clear();
        SelectAliasMap.Clear();

        var tokens = Tokenize(sql);
        if (tokens.Count == 0)
            return sql;

        var edits = new List<TextEdit>();
        ExtractSelectAliases(sql, tokens, edits);

        return ApplyEdits(sql, edits).TrimEnd();
    }

    public void InsertAliases(List<Visualisation> visualisations)
    {
        var selectVis = visualisations.First(v => v.Component.Keyword == SQLKeyword.SELECT);
        var table = selectVis.ToTables[0];

        foreach (var aliasEntry in SelectAliasMap)
        {
            var parts = aliasEntry.Key.Trim().Split('.');
            if (parts.Length != 2)
                continue;

            var tableReference = ResolveTableReference(parts[0]);
            var columnName = $"{tableReference}.{parts[1]}";
            var index = table.IndexOfColumn(columnName);

            if (index >= 0)
                table.ColumnNames[index] = aliasEntry.Value;
        }
    }

    private List<TableAliasDefinition> ExtractTableAliases(string sql, List<Token> tokens)
    {
        var definitions = new List<TableAliasDefinition>();

        for (var index = 0; index < tokens.Count; index++)
        {
            if (!IsTableSourceStarter(tokens[index]))
                continue;

            var tableStartIndex = index + 1;
            if (!IsIdentifierToken(tokens, tableStartIndex))
                continue;

            var tableEndIndex = ConsumeQualifiedIdentifier(tokens, tableStartIndex);
            var aliasKeywordIndex = tableEndIndex + 1;
            var aliasIndex = -1;

            if (aliasKeywordIndex < tokens.Count && IsKeyword(tokens[aliasKeywordIndex], "AS") && IsIdentifierToken(tokens, aliasKeywordIndex + 1))
            {
                aliasIndex = aliasKeywordIndex + 1;
            }
            else if (IsIdentifierToken(tokens, aliasKeywordIndex) && !IsClauseBoundary(tokens[aliasKeywordIndex].Text))
            {
                aliasIndex = aliasKeywordIndex;
            }

            if (aliasIndex == -1)
                continue;

            var tableName = sql[tokens[tableStartIndex].Start..tokens[tableEndIndex].End];
            var alias = tokens[aliasIndex].Text;

            AliasToTableMap[alias] = tableName;
            definitions.Add(new TableAliasDefinition(
                alias,
                tableName,
                tokens[aliasIndex].Start,
                tokens[aliasIndex].End,
                tokens[tableEndIndex].End,
                tokens[aliasIndex].End));
        }

        return definitions;
    }

    private void PlanTableAliasEdits(List<TableAliasDefinition> definitions, List<TextEdit> edits)
    {
        var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var definition in definitions)
        {
            usedNames.Add(definition.TableName);
            usedNames.Add(definition.Alias);
        }

        foreach (var group in definitions.GroupBy(d => d.TableName, StringComparer.OrdinalIgnoreCase))
        {
            var occurrence = 0;

            foreach (var definition in group)
            {
                occurrence++;

                if (occurrence == 1)
                {
                    AliasReferenceMap[definition.Alias] = definition.TableName;
                    edits.Add(new TextEdit(definition.RemoveStart, definition.RemoveEnd, string.Empty));
                    continue;
                }

                var syntheticAlias = CreateSyntheticAlias(definition.TableName, occurrence, usedNames);
                AliasReferenceMap[definition.Alias] = syntheticAlias;
                edits.Add(new TextEdit(definition.AliasStart, definition.AliasEnd, syntheticAlias));
            }
        }
    }

    private void ExtractSelectAliases(string sql, List<Token> tokens, List<TextEdit> edits)
    {
        var selectIndex = FindTopLevelKeyword(tokens, "SELECT");
        var fromIndex = FindTopLevelKeyword(tokens, "FROM");

        if (selectIndex == -1 || fromIndex == -1 || fromIndex <= selectIndex)
            return;

        var itemStart = tokens[selectIndex].End;

        for (var index = selectIndex + 1; index <= fromIndex; index++)
        {
            var isBoundary = index == fromIndex;
            var isComma = !isBoundary && tokens[index].Text == "," && tokens[index].Depth == 0;

            if (!isBoundary && !isComma)
                continue;

            var itemEnd = isBoundary ? tokens[fromIndex].Start : tokens[index].Start;
            ProcessSelectItem(sql, tokens, itemStart, itemEnd, edits);

            if (!isBoundary)
                itemStart = tokens[index].End;
        }
    }

    private void ProcessSelectItem(string sql, List<Token> tokens, int itemStart, int itemEnd, List<TextEdit> edits)
    {
        var itemTokens = tokens
            .Where(t => t.Start >= itemStart && t.End <= itemEnd && t.Depth == 0)
            .ToList();

        if (itemTokens.Count <= 1)
            return;

        var aliasToken = itemTokens[^1];
        if (!aliasToken.IsIdentifier)
            return;

        var previousToken = itemTokens[^2];
        var aliasStart = aliasToken.Start;
        var expressionEnd = aliasToken.Start;

        if (IsKeyword(previousToken.Text, "AS"))
        {
            aliasStart = previousToken.Start;
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

        var columnReference = TryExtractColumnReference(expression);
        if (columnReference is not null)
            SelectAliasMap[columnReference] = aliasToken.Text;

        while (aliasStart > itemStart && char.IsWhiteSpace(sql[aliasStart - 1]))
            aliasStart--;

        edits.Add(new TextEdit(aliasStart, aliasToken.End, string.Empty));
    }

    private string ReplaceTableAliasReferences(string sql)
    {
        if (AliasReferenceMap.Count == 0)
            return sql;

        var tokens = Tokenize(sql);
        var builder = new StringBuilder(sql.Length);
        var cursor = 0;

        for (var index = 0; index < tokens.Count; index++)
        {
            var token = tokens[index];
            if (cursor < token.Start)
                builder.Append(sql, cursor, token.Start - cursor);

            if (token.IsIdentifier &&
                AliasReferenceMap.TryGetValue(token.Text, out var replacement) &&
                index + 1 < tokens.Count &&
                tokens[index + 1].Text == ".")
            {
                builder.Append(replacement);
            }
            else
            {
                builder.Append(sql, token.Start, token.End - token.Start);
            }

            cursor = token.End;
        }

        if (cursor < sql.Length)
            builder.Append(sql, cursor, sql.Length - cursor);

        return builder.ToString();
    }

    private static string ApplyEdits(string sql, List<TextEdit> edits)
    {
        if (edits.Count == 0)
            return sql;

        var orderedEdits = edits
            .Where(edit => edit.End >= edit.Start)
            .OrderBy(edit => edit.Start)
            .ToList();

        var builder = new StringBuilder(sql.Length);
        var cursor = 0;

        foreach (var edit in orderedEdits)
        {
            if (edit.Start < cursor)
                continue;

            builder.Append(sql, cursor, edit.Start - cursor);
            builder.Append(edit.Replacement);
            cursor = edit.End;
        }

        if (cursor < sql.Length)
            builder.Append(sql, cursor, sql.Length - cursor);

        return builder.ToString();
    }

    private static string? TryExtractColumnReference(string expression)
    {
        var trimmed = expression.Trim();
        if (trimmed.StartsWith("DISTINCT ", StringComparison.OrdinalIgnoreCase))
            trimmed = trimmed["DISTINCT ".Length..].TrimStart();

        var tokens = Tokenize(trimmed);
        if (tokens.Count == 1 && tokens[0].IsIdentifier)
            return tokens[0].Text;

        if (tokens.Count == 3 && tokens[0].IsIdentifier && tokens[1].Text == "." && tokens[2].IsIdentifier)
            return $"{tokens[0].Text}.{tokens[2].Text}";

        return null;
    }

    private string ResolveTableReference(string reference)
    {
        if (AliasReferenceMap.TryGetValue(reference, out var replacement))
            return replacement;

        if (AliasToTableMap.TryGetValue(reference, out var tableName))
            return tableName;

        return reference;
    }

    private static string CreateSyntheticAlias(string tableName, int occurrence, HashSet<string> usedNames)
    {
        var baseName = ExtractAliasBaseName(tableName);
        var candidate = $"{baseName}_{occurrence}";

        while (!usedNames.Add(candidate))
            candidate = $"{candidate}_x";

        return candidate;
    }

    private static string ExtractAliasBaseName(string tableName)
    {
        var tokens = Tokenize(tableName);
        var lastIdentifier = tokens.LastOrDefault(t => t.IsIdentifier)?.Text ?? "table";

        if (lastIdentifier.StartsWith('"') && lastIdentifier.EndsWith('"') && lastIdentifier.Length >= 2)
            lastIdentifier = lastIdentifier[1..^1];

        var builder = new StringBuilder(lastIdentifier.Length);
        foreach (var character in lastIdentifier)
        {
            builder.Append(char.IsLetterOrDigit(character) || character == '_' ? character : '_');
        }

        if (builder.Length == 0 || !char.IsLetter(builder[0]) && builder[0] != '_')
            builder.Insert(0, '_');

        return builder.ToString();
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

    private static int ConsumeQualifiedIdentifier(List<Token> tokens, int startIndex)
    {
        var index = startIndex;

        while (index + 2 < tokens.Count &&
               tokens[index].IsIdentifier &&
               tokens[index + 1].Text == "." &&
               tokens[index + 2].IsIdentifier)
        {
            index += 2;
        }

        return index;
    }

    private static bool IsTableSourceStarter(Token token)
    {
        return token.Depth == 0 && (IsKeyword(token.Text, "FROM") || IsKeyword(token.Text, "JOIN"));
    }

    private static bool IsIdentifierToken(List<Token> tokens, int index)
    {
        return index >= 0 && index < tokens.Count && tokens[index].IsIdentifier;
    }

    private static bool IsClauseBoundary(string text)
    {
        return text.Equals(",", StringComparison.Ordinal) ||
               IsKeyword(text, "JOIN") ||
               IsKeyword(text, "INNER") ||
               IsKeyword(text, "LEFT") ||
               IsKeyword(text, "RIGHT") ||
               IsKeyword(text, "FULL") ||
               IsKeyword(text, "CROSS") ||
               IsKeyword(text, "NATURAL") ||
               IsKeyword(text, "WHERE") ||
               IsKeyword(text, "GROUP") ||
               IsKeyword(text, "HAVING") ||
               IsKeyword(text, "ORDER") ||
               IsKeyword(text, "LIMIT") ||
               IsKeyword(text, "OFFSET") ||
               IsKeyword(text, "WINDOW") ||
               IsKeyword(text, "UNION") ||
               IsKeyword(text, "INTERSECT") ||
               IsKeyword(text, "EXCEPT") ||
               IsKeyword(text, "ON");
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

    private static bool IsKeyword(Token token, string keyword)
    {
        return IsKeyword(token.Text, keyword);
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

    private sealed record Token(string Text, int Start, int End, int Depth, bool IsIdentifier);
    private sealed record TextEdit(int Start, int End, string Replacement);
    private sealed record TableAliasDefinition(
        string Alias,
        string TableName,
        int AliasStart,
        int AliasEnd,
        int RemoveStart,
        int RemoveEnd);
}
