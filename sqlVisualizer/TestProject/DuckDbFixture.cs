using DuckDB.NET.Data;

namespace TestProject1;

[CollectionDefinition("DuckDb seeded")]
public class DuckDbCollection : ICollectionFixture<DuckDbFixture> { }
public sealed class DuckDbFixture : IDisposable
{
    public string DbPath { get; }
    public string ConnectionString => $"DataSource={DbPath}";

    public DuckDbFixture()
    {
        DbPath = Path.Combine(Path.GetTempPath(), $"duckdb-test-{Guid.NewGuid():N}.db");

        Seed();
    }

    private void Seed()
    {
        using var conn = CreateConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();

        cmd.CommandText =
            """
            -- SHIFT
            DROP TABLE IF EXISTS shift;
            CREATE TABLE shift (
                day TEXT,
                cashier TEXT
            );

            INSERT INTO shift VALUES
              ('2025-02-11', 'Mads'),
              ('2025-02-12', 'Anna'),
              ('2025-02-18', 'Anna'),
              ('2025-02-19', 'Anna');

            -- USER
            DROP TABLE IF EXISTS "user";
            CREATE TABLE "user" (
                username TEXT,
                email TEXT,
                password TEXT
            );

            INSERT INTO "user" VALUES
              ('Anna', 'anna', '***'),
              ('Martin', 'mhent', '***'),
              ('Omar', 'omsh', '***');

            -- PRODUCT
            DROP TABLE IF EXISTS product;
            CREATE TABLE product (
                productname TEXT,
                price INTEGER
            );

            INSERT INTO product VALUES
              ('Tea', 100),
              ('Small', 85),
              ('Large', 100),
              ('Fancy', NULL);

            -- PURCHASE
            DROP TABLE IF EXISTS purchase;
            CREATE TABLE purchase (
                purchasetime TEXT,
                productname TEXT,
                username TEXT
            );

            INSERT INTO purchase VALUES
              ('2025-02-11 09:55', 'Tea', 'Martin'),
              ('2025-02-12 10:03', 'Small', 'Martin'),
              ('2025-02-12 10:05', 'Small', 'Omar'),
              ('2025-02-12 10:06', 'Large', 'Omar'),
              ('2025-02-19 09:00', 'Small', 'Martin');
            """;
        cmd.ExecuteNonQuery();
        conn.Close();
    }

    public DuckDBConnection CreateConnection() => new DuckDBConnection(ConnectionString);

    public void Dispose()
    {
        try { File.Delete(DbPath); } catch { /* ignore cleanup errors */ }
    }
}