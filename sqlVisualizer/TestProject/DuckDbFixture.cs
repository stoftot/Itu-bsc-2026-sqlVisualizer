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
            
            -- WIND TURBINE PRODUCTION
            DROP TABLE IF EXISTS wind_turbine_production;
            CREATE TABLE wind_turbine_production (
                id              INTEGER         PRIMARY KEY,
                production_date DATE            NOT NULL,
                turbine_id      VARCHAR(10)     NOT NULL,
                power_output    DOUBLE          NOT NULL
            );

            INSERT INTO wind_turbine_production VALUES
              (1,  '2025-01-01', 'WT-001', 28.507),
              (2,  '2025-01-01', 'WT-002', 22.503),
              (3,  '2025-01-01', 'WT-003', 28.673),
              (4,  '2025-01-02', 'WT-001', 46.019),
              (5,  '2025-01-02', 'WT-002', 46.281),
              (6,  '2025-01-02', 'WT-003', 43.827),
              (7,  '2025-01-03', 'WT-001', 37.193),
              (8,  '2025-01-03', 'WT-002', 39.811),
              (9,  '2025-01-03', 'WT-003', 29.403),
              (10, '2025-01-04', 'WT-001', 37.208),
              (11, '2025-01-04', 'WT-002', 28.888),
              (12, '2025-01-04', 'WT-003', 34.052),
              (13, '2025-01-05', 'WT-001', 23.552),
              (14, '2025-01-05', 'WT-002', 20.206),
              (15, '2025-01-05', 'WT-003', 27.463);

            -- Retardo table
            DROP TABLE IF EXISTS "123";
            CREATE TABLE "123" (
                "*" INTEGER,
                "123" INTEGER,
                "user" TEXT,
            );
            
            INSERT INTO "123" VALUES
                (1, 2, '1_1'),
                (3, 4, '2_1'),
                (5, 6, '3_1');
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