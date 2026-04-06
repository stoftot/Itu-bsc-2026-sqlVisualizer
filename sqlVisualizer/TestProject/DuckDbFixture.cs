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
            -- Table with coffee types
            DROP TABLE IF EXISTS coffee_sales;
            DROP TABLE IF EXISTS coffee_types;
            CREATE TABLE coffee_types (
                coffee_id INTEGER PRIMARY KEY,
                coffee_name TEXT
            );
            
            -- Table with sales transactions
            
            CREATE TABLE coffee_sales (
                sale_id INTEGER PRIMARY KEY,
                coffee_id INTEGER,
                quantity INTEGER,
                price_per_unit DOUBLE,
                sale_date TEXT,
                FOREIGN KEY (coffee_id) REFERENCES coffee_types(coffee_id)
            );
            
            -- Insert coffee types
            INSERT INTO coffee_types VALUES
            (1, 'Espresso'),
            (2, 'Latte'),
            (3, 'Cappuccino'),
            (4, 'Americano'),
            (5, 'Mocha');
            
            -- Insert sales data
            INSERT INTO coffee_sales VALUES
            (1, 1, 2, 3.00, '2026-01-01'),
            (2, 2, 1, 4.50, '2026-01-01'),
            (3, 1, 1, 3.00, '2026-01-02'),
            (4, 3, 3, 4.00, '2026-01-02'),
            (5, 2, 2, 4.50, '2026-01-03'),
            (6, 4, 1, 2.50, '2026-01-03'),
            (7, 5, 1, 3.5, '2026-01-03'),
            (8, 5, 1, 3.5, '2026-01-04');
            
            -- EMPLOYEES
            DROP tABLE IF EXISTS employees;
            CREATE TABLE employees (
            emp_id INTEGER PRIMARY KEY,
            department TEXT NOT NULL,
            salary INTEGER NOT NULL
            );
                
            INSERT INTO employees VALUES
              (1, 'IT', 5000),
              (2, 'HR', 2400),
              (3, 'HR', 3500),
              (4, 'Sales', 5500),
              (5, 'Engineering', 6000);
            
            
            DROP TABLE IF EXISTS sale;
            CREATE TABLE sale (
                order_id INTEGER PRIMARY KEY,
                user_id INTEGER NOT NULL,
                region TEXT NOT NULL,
                amount INTEGER NOT NULL
            );
            
            INSERT INTO sale VALUES 
             (1, 1, 'North', 100),
             (2, 2, 'South', 150),
             (3, 3, 'East', 200),
             (4, 4, 'West', 250),
             (5, 5, 'North', 300);  
            
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
                user_id INTEGER,
                username TEXT,
                email TEXT,
                password TEXT
            );
            
            INSERT INTO "user" VALUES
              (1, 'Anna', 'anna', '***'),
              (2, 'Martin', 'mhent', '***'),
              (3, 'Omar', 'omsh', '***'),
              (4, 'Alice', 'alice', '***'),
              (5, 'Bob', 'bob', '***'),
              (6, 'Charlie', 'charlie', '***'),
              (7, 'David', 'david', '***'),
              (8, 'Eve', 'eve', '***');
            
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
              
            create or replace view sales as
            select pu.*, price
            from purchase pu join product pr
            on pu.productname = pr.productname;
            
            -- WIND TURBINE PRODUCTION
            DROP TABLE IF EXISTS wind_turbine_production;
            CREATE TABLE wind_turbine_production (
                production_date DATE            NOT NULL,
                turbine_id      VARCHAR(10)     NOT NULL,
                power_output    DOUBLE          NOT NULL
            );
             
            -- Insert data
            INSERT INTO wind_turbine_production VALUES
              ('2025-01-01', 'WT-001', 28.507),
              ('2025-01-01', 'WT-002', 22.503),
              ('2025-01-01', 'WT-003', 28.673),
              ('2025-01-01', 'WT-004', 23.868),
              ('2025-01-01', 'WT-005', 22.708),
              ('2025-01-02', 'WT-001', 46.019),
              ('2025-01-02', 'WT-002', 46.281),
              ('2025-01-02', 'WT-003', 43.827),
              ('2025-01-02', 'WT-004', 35.965),
              ('2025-01-02', 'WT-005', 32.736),
              ('2025-01-03', 'WT-001', 37.193),
              ('2025-01-03', 'WT-002', 39.811),
              ('2025-01-03', 'WT-003', 29.403),
              ('2025-01-03', 'WT-004', 34.573),
              ('2025-01-03', 'WT-005', 28.188),
              ('2025-01-04', 'WT-001', 37.208),
              ('2025-01-04', 'WT-002', 28.888),
              ('2025-01-04', 'WT-003', 34.052),
              ('2025-01-04', 'WT-004', 29.565),
              ('2025-01-04', 'WT-005', 32.23),
              ('2025-01-05', 'WT-001', 23.552),
              ('2025-01-05', 'WT-002', 20.206),
              ('2025-01-05', 'WT-003', 27.463),
              ('2025-01-05', 'WT-004', 25.665),
              ('2025-01-05', 'WT-005', 27.185),
              ('2025-01-06', 'WT-001', 26.771),
              ('2025-01-06', 'WT-002', 24.025),
              ('2025-01-06', 'WT-003', 27.021),
              ('2025-01-06', 'WT-004', 19.314),
              ('2025-01-06', 'WT-005', 20.308);

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