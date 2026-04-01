using visualizer.Models;
using visualizer.Repositories;

namespace TestProject1;

/// <summary>
/// Tests for <see cref="DuckDbSQLDecomposer"/> covering edge cases that the original
/// regex-based <see cref="SQLDecomposer"/> cannot handle correctly.
///
/// Each test group is paired with a comment explaining why the old regex approach
/// would fail on that input.
/// </summary>
public class DuckDbSQLDecomposerTest
{
    private readonly DuckDbSQLDecomposer _decomposer = new();

    // ── Helpers ──────────────────────────────────────────────────────────────

    private List<SQLDecompositionComponent> Decompose(string sql)
    {
        var result = _decomposer.Decompose(sql);
        Assert.NotNull(result);
        return result!;
    }

    private static SQLDecompositionComponent Clause(
        List<SQLDecompositionComponent> components, SQLKeyword keyword) =>
        components.Single(c => c.Keyword == keyword);

    // ── Basic sanity: same queries the regex version already handles ─────────

    [Theory]
    [InlineData("SELECT day FROM shift")]
    [InlineData("SELECT * FROM shift")]
    [InlineData("SELECT day FROM shift WHERE day = '2025-02-11'")]
    [InlineData("SELECT productname FROM purchase GROUP BY productname")]
    [InlineData("SELECT productname, COUNT() FROM purchase GROUP BY productname HAVING COUNT() > 1")]
    [InlineData("SELECT * FROM purchase ORDER BY purchasetime")]
    [InlineData("SELECT * FROM purchase LIMIT 3")]
    [InlineData("SELECT * FROM purchase LIMIT 3 OFFSET 1")]
    public void BasicQueries_ReturnExpectedKeywords(string sql)
    {
        // These should work with both the old and new decomposer.
        var components = Decompose(sql);
        Assert.True(components.Count > 0);
        Assert.Contains(components, c => c.Keyword == SQLKeyword.SELECT);
        Assert.Contains(components, c => c.Keyword == SQLKeyword.FROM);
    }

    // ── Edge case 1: SQL keyword inside a string literal ─────────────────────
    //
    // The regex SQLDecomposer splits on " from " as a plain string.
    // A string literal like 'from home' contains that exact byte sequence,
    // so Split(" from ") produces the wrong pieces.
    //
    // DuckDbSQLDecomposer uses a tokenizer that treats single-quoted content
    // as an opaque token — keywords inside are never boundary candidates.

    [Fact]
    public void KeywordInStringLiteral_WhereClause_ParsedCorrectly()
    {
        var sql = "SELECT day FROM shift WHERE day = 'from home'";
        var components = Decompose(sql);

        Assert.Contains(components, c => c.Keyword == SQLKeyword.FROM);
        Assert.Contains(components, c => c.Keyword == SQLKeyword.WHERE);

        var where = Clause(components, SQLKeyword.WHERE);
        Assert.Contains("'from home'", where.Clause);
    }

    [Fact]
    public void KeywordInStringLiteral_SelectKeywordInLiteral_ParsedCorrectly()
    {
        // 'select *' as a literal value should not confuse the SELECT clause boundary.
        var sql = "SELECT day FROM shift WHERE day = 'select * from users'";
        var components = Decompose(sql);

        Assert.Contains(components, c => c.Keyword == SQLKeyword.SELECT);
        Assert.Contains(components, c => c.Keyword == SQLKeyword.FROM);
        Assert.Contains(components, c => c.Keyword == SQLKeyword.WHERE);

        var select = Clause(components, SQLKeyword.SELECT);
        // SELECT clause should just be "day", not anything from inside the literal
        Assert.Equal("day", select.Clause, ignoreCase: true);
    }

    [Fact]
    public void KeywordInStringLiteral_WhereInsideLiteral_OnlyOneWhereClause()
    {
        // 'where x > 1' inside a string should not produce a second WHERE clause.
        var sql = "SELECT day FROM shift WHERE day = 'where x > 1'";
        var components = Decompose(sql);

        Assert.Single(components, c => c.Keyword == SQLKeyword.WHERE);
    }

    // ── Edge case 2: Keywords inside subqueries ───────────────────────────────
    //
    // The regex SQLDecomposer works at the character level and has no concept of
    // nesting. A subquery like (SELECT id FROM orders) contains FROM at depth 1,
    // which causes the outer FROM split to break.
    //
    // DuckDbSQLDecomposer only treats keywords at depth 0 as boundaries.

    [Fact]
    public void SubqueryInFrom_InnerFromIgnored()
    {
        // The inner FROM (inside the subquery) must not be treated as a clause boundary.
        var sql = "SELECT sub.day FROM (SELECT day FROM shift) sub";
        var components = Decompose(sql);

        // There should be exactly one FROM clause
        Assert.Single(components, c => c.Keyword == SQLKeyword.FROM);

        var from = Clause(components, SQLKeyword.FROM);
        // The entire subquery should be in the FROM clause text
        Assert.Contains("SELECT day FROM shift", from.Clause, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SubqueryInFrom_OuterWhereStillDetected()
    {
        var sql = "SELECT sub.day FROM (SELECT day FROM shift) sub WHERE sub.day > '2025-01-01'";
        var components = Decompose(sql);

        Assert.Contains(components, c => c.Keyword == SQLKeyword.WHERE);
        // And still only one FROM
        Assert.Single(components, c => c.Keyword == SQLKeyword.FROM);
    }

    [Fact]
    public void SubqueryInWhere_InnerKeywordsIgnored()
    {
        // WHERE clause containing a subquery — its SELECT, FROM, WHERE are all at depth 1.
        var sql = "SELECT day FROM shift WHERE day IN (SELECT day FROM shift WHERE cashier = 'Anna')";
        var components = Decompose(sql);

        // Exactly one of each top-level clause
        Assert.Single(components, c => c.Keyword == SQLKeyword.SELECT);
        Assert.Single(components, c => c.Keyword == SQLKeyword.FROM);
        Assert.Single(components, c => c.Keyword == SQLKeyword.WHERE);
    }

    // ── Edge case 3: SQL comments ─────────────────────────────────────────────
    //
    // Commented-out keywords should never be treated as clause boundaries.

    [Fact]
    public void LineComment_CommentedKeywordIgnored()
    {
        var sql = """
                  SELECT day
                  FROM shift
                  -- WHERE day = '2025-02-11'
                  """;
        var components = Decompose(sql);

        // The commented WHERE should not appear as a clause
        Assert.DoesNotContain(components, c => c.Keyword == SQLKeyword.WHERE);
        Assert.Contains(components, c => c.Keyword == SQLKeyword.FROM);
    }

    [Fact]
    public void BlockComment_CommentedKeywordIgnored()
    {
        var sql = "SELECT day FROM shift /* WHERE day = '2025-02-11' */";
        var components = Decompose(sql);

        Assert.DoesNotContain(components, c => c.Keyword == SQLKeyword.WHERE);
    }

    // ── Edge case 4: parse errors give clear messages ─────────────────────────
    //
    // The old decomposer would silently produce garbage or throw an unrelated
    // exception for invalid SQL. DuckDbSQLDecomposer throws with DuckDB's own
    // error message immediately.

    [Fact]
    public void InvalidSQL_ThrowsWithMessage()
    {
        var ex = Assert.Throws<Exception>(() => _decomposer.Decompose("SELECT FROM FROM FROM"));
        Assert.Contains("parse error", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    // ── Edge case 5: JOIN type detection ─────────────────────────────────────

    [Fact]
    public void LeftJoin_DetectedCorrectly()
    {
        var sql = "SELECT * FROM shift LEFT JOIN \"user\" ON shift.cashier = \"user\".username";
        var components = Decompose(sql);

        Assert.Contains(components, c => c.Keyword == SQLKeyword.LEFT_JOIN);
    }

    [Fact]
    public void InnerJoin_DetectedCorrectly()
    {
        var sql = "SELECT * FROM shift INNER JOIN \"user\" ON shift.cashier = \"user\".username";
        var components = Decompose(sql);

        Assert.Contains(components, c => c.Keyword == SQLKeyword.INNER_JOIN);
    }

    // ── Execution order ───────────────────────────────────────────────────────

    [Fact]
    public void Components_OrderedByExecutionPrecedence()
    {
        var sql = "SELECT productname, COUNT() FROM purchase GROUP BY productname HAVING COUNT() > 1 ORDER BY productname";
        var components = Decompose(sql);

        var precedences = components.Select(c => c.Keyword.ExecutionPrecedence()).ToList();
        Assert.Equal(precedences.OrderBy(x => x).ToList(), precedences);
    }

    // ── WITH clause (CTEs) ────────────────────────────────────────────────────
    //
    // WITH clauses define named subqueries (CTEs) referenced in FROM.
    // The keywords inside the CTE body are at depth > 0 and must not be
    // treated as outer clause boundaries.

    [Fact]
    public void WithClause_DetectedAsWithComponent()
    {
        var sql = """
                  WITH recent AS (SELECT day FROM shift WHERE day > '2025-01-01')
                  SELECT day FROM recent
                  """;
        var components = Decompose(sql);

        Assert.Contains(components, c => c.Keyword == SQLKeyword.WITH);
    }

    [Fact]
    public void WithClause_InnerKeywordsNotTreatedAsBoundaries()
    {
        var sql = """
                  WITH recent AS (SELECT day FROM shift WHERE day > '2025-01-01')
                  SELECT day FROM recent WHERE day > '2025-06-01'
                  """;
        var components = Decompose(sql);

        // Exactly one FROM — the inner one inside the CTE is at depth 1
        Assert.Single(components, c => c.Keyword == SQLKeyword.FROM);
        // Exactly one WHERE at the outer level
        Assert.Single(components, c => c.Keyword == SQLKeyword.WHERE);
    }

    [Fact]
    public void WithClause_ClauseTextContainsCTEDefinition()
    {
        var sql = """
                  WITH recent AS (SELECT day FROM shift)
                  SELECT day FROM recent
                  """;
        var components = Decompose(sql);

        var withComponent = components.Single(c => c.Keyword == SQLKeyword.WITH);
        Assert.Contains("recent", withComponent.Clause, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("AS", withComponent.Clause, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void WithClause_WithComesBeforeFromInExecutionOrder()
    {
        var sql = """
                  WITH recent AS (SELECT day FROM shift)
                  SELECT day FROM recent
                  """;
        var components = Decompose(sql);

        var withPrecedence = components.Single(c => c.Keyword == SQLKeyword.WITH).Keyword.ExecutionPrecedence();
        var fromPrecedence = components.Single(c => c.Keyword == SQLKeyword.FROM).Keyword.ExecutionPrecedence();

        Assert.True(withPrecedence < fromPrecedence);
    }
}
