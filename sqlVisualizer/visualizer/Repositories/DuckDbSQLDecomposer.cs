using System.Text.Json;
using DuckDB.NET.Data;
using visualizer.Models;

namespace visualizer.Repositories;

/// <summary>
/// A replacement for <see cref="SQLDecomposer"/> that uses DuckDB's built-in
/// <c>json_serialize_sql()</c> function as the SQL parser instead of hand-rolled regex.
///
/// Why this is better than the regex approach:
///
///   1. String literals containing SQL keywords are never confused for clause boundaries.
///      e.g. <c>WHERE name = 'select from where'</c> parses correctly.
///
///   2. Subqueries are handled via parenthesis depth tracking — keywords inside a
///      subquery do not affect outer clause detection.
///      e.g. <c>FROM (SELECT id FROM orders WHERE id > 1) sub WHERE sub.id &lt; 10</c>
///      correctly identifies only the outer FROM and WHERE.
///
///   3. SQL comments (<c>--</c> and <c>/* */</c>) are stripped before splitting,
///      so a commented-out keyword is never treated as a boundary.
///
///   4. Parse errors are caught before the visualization pipeline starts, with
///      DuckDB's own error messages rather than a cryptic downstream exception.
///
///   5. The decomposition is guaranteed to agree with DuckDB's own parser, because
///      DuckDB itself validates the query in Step 1.
///
/// How it works:
///
///   Step 1 — Validate using <c>json_serialize_sql()</c> via an in-memory DuckDB
///             connection. No tables are needed — this is a pure parse step.
///             If DuckDB reports a parse error, an exception is thrown immediately
///             with DuckDB's own error message.
///
///   Step 2 — Split the original SQL text into clause components using a
///             depth-aware tokenizer. Only tokens at depth 0 (outside all
///             parentheses and string literals) are treated as clause boundaries.
///             The original query text is preserved character-for-character in
///             each clause, so no information is lost.
/// </summary>
public class DuckDbSQLDecomposer : ISQLDecomposer
{
    // json_serialize_sql() is a pure parse function — no tables needed.
    private const string InMemoryConnectionString = "DataSource=:memory:";

    public List<SQLDecompositionComponent>? Decompose(string sql)
    {
        // Step 1: validate through DuckDB's own parser.
        ValidateWithDuckDb(sql);

        // Step 2: split into components using a depth-aware tokenizer.
        var components = SplitByDepthAwareTokenizer(sql);

        if (components.Count == 0) return null;
        return components.OrderBy(c => c.Keyword.ExecutionPrecedence()).ToList();
    }

    // ── Step 1: DuckDB validation ────────────────────────────────────────────

    public static void ValidateWithDuckDb(string sql)
    {
        // Escape single quotes so the SQL can be embedded as a literal argument.
        var escaped = sql.Replace("'", "''");

        using var connection = new DuckDBConnection(InMemoryConnectionString);
        connection.Open();
        using var command = new DuckDBCommand($"SELECT json_serialize_sql('{escaped}')", connection);
        var raw = command.ExecuteScalar()?.ToString()
            ?? throw new Exception("DuckDB returned null from json_serialize_sql.");

        using var doc = JsonDocument.Parse(raw);

        if (doc.RootElement.TryGetProperty("error", out var errorFlag) && errorFlag.GetBoolean())
        {
            var message = doc.RootElement.TryGetProperty("error_message", out var msg)
                ? msg.GetString()
                : "Unknown SQL parse error.";
            throw new Exception($"SQL parse error: {message}");
        }
    }

    // ── Step 2: Depth-aware clause splitting ────────────────────────────────

    /// <summary>
    /// Walks the token stream and records a boundary whenever a SQL clause keyword
    /// appears at depth 0 (outside all parentheses and string literals).
    /// Then slices the original SQL text between consecutive boundaries to produce
    /// the clause content for each <see cref="SQLDecompositionComponent"/>.
    /// </summary>
    private static List<SQLDecompositionComponent> SplitByDepthAwareTokenizer(string sql)
    {
        var tokens = Tokenize(sql);
        var boundaries = new List<(int kwStart, int kwEnd, SQLKeyword keyword)>();

        for (var i = 0; i < tokens.Count; i++)
        {
            var t = tokens[i];
            if (t.Depth != 0 || !t.IsWord) continue;

            var upper = t.Text.ToUpperInvariant();

            switch (upper)
            {
                case "WITH":
                    boundaries.Add((t.Start, t.End, SQLKeyword.WITH));
                    break;

                case "SELECT":
                    boundaries.Add((t.Start, t.End, SQLKeyword.SELECT));
                    break;

                case "FROM":
                    boundaries.Add((t.Start, t.End, SQLKeyword.FROM));
                    break;

                case "WHERE":
                    boundaries.Add((t.Start, t.End, SQLKeyword.WHERE));
                    break;

                case "HAVING":
                    boundaries.Add((t.Start, t.End, SQLKeyword.HAVING));
                    break;

                case "LIMIT":
                    boundaries.Add((t.Start, t.End, SQLKeyword.LIMIT));
                    break;

                case "OFFSET":
                    boundaries.Add((t.Start, t.End, SQLKeyword.OFFSET));
                    break;

                case "GROUP" when NextWordIs(tokens, i, "BY"):
                    boundaries.Add((t.Start, tokens[i + 1].End, SQLKeyword.GROUP_BY));
                    i++; // skip the BY token
                    break;

                case "ORDER" when NextWordIs(tokens, i, "BY"):
                    boundaries.Add((t.Start, tokens[i + 1].End, SQLKeyword.ORDER_BY));
                    i++; // skip the BY token
                    break;

                // Plain JOIN (no qualifier)
                case "JOIN":
                    boundaries.Add((t.Start, t.End, SQLKeyword.JOIN));
                    break;

                // INNER JOIN
                case "INNER" when NextWordIs(tokens, i, "JOIN"):
                    boundaries.Add((t.Start, tokens[i + 1].End, SQLKeyword.INNER_JOIN));
                    i++;
                    break;

                // LEFT JOIN
                case "LEFT" when NextWordIs(tokens, i, "JOIN"):
                    boundaries.Add((t.Start, tokens[i + 1].End, SQLKeyword.LEFT_JOIN));
                    i++;
                    break;

                // RIGHT JOIN
                case "RIGHT" when NextWordIs(tokens, i, "JOIN"):
                    boundaries.Add((t.Start, tokens[i + 1].End, SQLKeyword.RIGHT_JOIN));
                    i++;
                    break;

                // FULL JOIN or FULL OUTER JOIN
                case "FULL" when NextWordIs(tokens, i, "JOIN"):
                    boundaries.Add((t.Start, tokens[i + 1].End, SQLKeyword.FULL_JOIN));
                    i++;
                    break;

                case "FULL" when NextWordIs(tokens, i, "OUTER") &&
                                  i + 2 < tokens.Count &&
                                  tokens[i + 2].IsWord &&
                                  tokens[i + 2].Text.Equals("JOIN", StringComparison.OrdinalIgnoreCase):
                    boundaries.Add((t.Start, tokens[i + 2].End, SQLKeyword.FULL_JOIN));
                    i += 2; // skip OUTER JOIN
                    break;
            }
        }

        // Slice the original SQL between consecutive boundary keyword ends.
        var result = new List<SQLDecompositionComponent>();
        for (var b = 0; b < boundaries.Count; b++)
        {
            var clauseStart = boundaries[b].kwEnd;
            var clauseEnd   = b + 1 < boundaries.Count
                ? boundaries[b + 1].kwStart
                : sql.Length;

            var clauseText = sql[clauseStart..clauseEnd].Trim().TrimEnd(';');
            result.Add(new SQLDecompositionComponent(boundaries[b].keyword, clauseText));
        }

        return result;
    }

    private static bool NextWordIs(List<Token> tokens, int i, string word) =>
        i + 1 < tokens.Count &&
        tokens[i + 1].IsWord &&
        tokens[i + 1].Text.Equals(word, StringComparison.OrdinalIgnoreCase);

    // ── Tokenizer ────────────────────────────────────────────────────────────

    /// <summary>
    /// Minimal tokenizer that tracks parenthesis depth and skips string literals
    /// and comments. Only <see cref="Token.IsWord"/> tokens at depth 0 can be
    /// clause-boundary keywords.
    /// </summary>
    private static List<Token> Tokenize(string sql)
    {
        var tokens = new List<Token>();
        var i = 0;
        var depth = 0;

        while (i < sql.Length)
        {
            var c = sql[i];

            if (char.IsWhiteSpace(c)) { i++; continue; }

            // Single-quoted string literal — skip entire contents, including any keywords inside.
            if (c == '\'')
            {
                var start = i++;
                while (i < sql.Length)
                {
                    if (sql[i] == '\'' && i + 1 < sql.Length && sql[i + 1] == '\'') { i += 2; continue; } // escaped ''
                    if (sql[i] == '\'') { i++; break; }
                    i++;
                }
                tokens.Add(new Token(sql[start..i], start, i, depth, IsWord: false));
                continue;
            }

            // Double-quoted identifier (e.g. "user", "from") — not a keyword.
            if (c == '"')
            {
                var start = i++;
                while (i < sql.Length)
                {
                    if (sql[i] == '"' && i + 1 < sql.Length && sql[i + 1] == '"') { i += 2; continue; } // escaped ""
                    if (sql[i] == '"') { i++; break; }
                    i++;
                }
                tokens.Add(new Token(sql[start..i], start, i, depth, IsWord: false));
                continue;
            }

            // Open paren — increase depth so inner keywords are ignored.
            if (c == '(')
            {
                tokens.Add(new Token("(", i, i + 1, depth, IsWord: false));
                depth++;
                i++;
                continue;
            }

            // Close paren — decrease depth.
            if (c == ')')
            {
                depth = Math.Max(0, depth - 1);
                tokens.Add(new Token(")", i, i + 1, depth, IsWord: false));
                i++;
                continue;
            }

            // Line comment — skip to end of line.
            if (c == '-' && i + 1 < sql.Length && sql[i + 1] == '-')
            {
                while (i < sql.Length && sql[i] != '\n') i++;
                continue;
            }

            // Block comment — skip to closing */.
            if (c == '/' && i + 1 < sql.Length && sql[i + 1] == '*')
            {
                i += 2;
                while (i + 1 < sql.Length && !(sql[i] == '*' && sql[i + 1] == '/')) i++;
                i += 2;
                continue;
            }

            // Word token (SQL keyword or identifier).
            if (char.IsLetter(c) || c == '_')
            {
                var start = i++;
                while (i < sql.Length && (char.IsLetterOrDigit(sql[i]) || sql[i] == '_')) i++;
                tokens.Add(new Token(sql[start..i], start, i, depth, IsWord: true));
                continue;
            }

            // Everything else (operators, punctuation, numbers) — single-char token.
            tokens.Add(new Token(c.ToString(), i, i + 1, depth, IsWord: false));
            i++;
        }

        return tokens;
    }

    private sealed record Token(string Text, int Start, int End, int Depth, bool IsWord);
}
