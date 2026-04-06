using DuckDB.NET.Data;

namespace visualizer;

public class DbInitializer(IConfiguration config)
{
    public void Initialize()
    {
        var connString = config.GetConnectionString("Default");
        // var connString = "Data Source=database.db";
        using var connection = new  DuckDBConnection(connString);
        connection.Open();

        var tableCmd = connection.CreateCommand();
        tableCmd.CommandText =
            """
            drop table if exists "123";
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
              --('2025-01-07', 'WT-001', 17.312),
              --('2025-01-07', 'WT-002', 19.683),
              --('2025-01-07', 'WT-003', 20.219),
              --('2025-01-07', 'WT-004', 19.226),
              --('2025-01-07', 'WT-005', 23.944),
              --('2025-01-08', 'WT-001', 35.185),
              --('2025-01-08', 'WT-002', 34.053),
              --('2025-01-08', 'WT-003', 37.961),
              --('2025-01-08', 'WT-004', 31.963),
              --('2025-01-08', 'WT-005', 41.835),
              --('2025-01-09', 'WT-001', 26.573),
              --('2025-01-09', 'WT-002', 38.259),
              --('2025-01-09', 'WT-003', 35.509),
              --('2025-01-09', 'WT-004', 28.163),
              --('2025-01-09', 'WT-005', 25.689),
              --('2025-01-10', 'WT-001', 38.472),
              --('2025-01-10', 'WT-002', 36.988),
              --('2025-01-10', 'WT-003', 37.291),
              --('2025-01-10', 'WT-004', 37.868),
              --('2025-01-10', 'WT-005', 28.341),
              --('2025-01-11', 'WT-001', 19.256),
              --('2025-01-11', 'WT-002', 17.275),
              --('2025-01-11', 'WT-003', 23.376),
              --('2025-01-11', 'WT-004', 21.418),
              --('2025-01-11', 'WT-005', 24.031),
              --('2025-01-12', 'WT-001', 32.52),
              --('2025-01-12', 'WT-002', 42.419),
              --('2025-01-12', 'WT-003', 36.643),
              --('2025-01-12', 'WT-004', 43.017),
              --('2025-01-12', 'WT-005', 41.566),
              --('2025-01-13', 'WT-001', 42.325),
              --('2025-01-13', 'WT-002', 36.242),
              --('2025-01-13', 'WT-003', 31.072),
              --('2025-01-13', 'WT-004', 39.775),
              --('2025-01-13', 'WT-005', 40.472),
              --('2025-01-14', 'WT-001', 20.841),
              --('2025-01-14', 'WT-002', 26.875),
              --('2025-01-14', 'WT-003', 24.187),
              --('2025-01-14', 'WT-004', 24.467),
              --('2025-01-14', 'WT-005', 23.544),
              --('2025-01-15', 'WT-001', 19.15),
              --('2025-01-15', 'WT-002', 19.929),
              --('2025-01-15', 'WT-003', 19.206),
              --('2025-01-15', 'WT-004', 24.926),
              --('2025-01-15', 'WT-005', 21.881),
              --('2025-01-16', 'WT-001', 23.749),
              --('2025-01-16', 'WT-002', 27.527),
              --('2025-01-16', 'WT-003', 21.295),
              --('2025-01-16', 'WT-004', 22.82),
              --('2025-01-16', 'WT-005', 26.087),
              --('2025-01-17', 'WT-001', 23.255),
              --('2025-01-17', 'WT-002', 21.671),
              --('2025-01-17', 'WT-003', 23.891),
              --('2025-01-17', 'WT-004', 22.55),
              --('2025-01-17', 'WT-005', 30.568),
              --('2025-01-18', 'WT-001', 34.254),
              --('2025-01-18', 'WT-002', 32.122),
              --('2025-01-18', 'WT-003', 35.026),
              --('2025-01-18', 'WT-004', 34.199),
              --('2025-01-18', 'WT-005', 26.672),
              --('2025-01-19', 'WT-001', 33.136),
              --('2025-01-19', 'WT-002', 29.09),
              --('2025-01-19', 'WT-003', 32.161),
              --('2025-01-19', 'WT-004', 33.176),
              --('2025-01-19', 'WT-005', 26.554),
              --('2025-01-20', 'WT-001', 21.796),
              --('2025-01-20', 'WT-002', 23.014),
              --('2025-01-20', 'WT-003', 25.072),
              --('2025-01-20', 'WT-004', 29.11),
              --('2025-01-20', 'WT-005', 29.551),
              --('2025-01-21', 'WT-001', 25.879),
              --('2025-01-21', 'WT-002', 32.376),
              --('2025-01-21', 'WT-003', 31.172),
              --('2025-01-21', 'WT-004', 28.654),
              --('2025-01-21', 'WT-005', 27.335),
              --('2025-01-22', 'WT-001', 21.31),
              --('2025-01-22', 'WT-002', 26.827),
              --('2025-01-22', 'WT-003', 21.178),
              --('2025-01-22', 'WT-004', 22.961),
              --('2025-01-22', 'WT-005', 24.641),
              --('2025-01-23', 'WT-001', 24.433),
              --('2025-01-23', 'WT-002', 30.72),
              --('2025-01-23', 'WT-003', 30.623),
              --('2025-01-23', 'WT-004', 23.277),
              --('2025-01-23', 'WT-005', 25.814),
              --('2025-01-24', 'WT-001', 25.151),
              --('2025-01-24', 'WT-002', 24.975),
              --('2025-01-24', 'WT-003', 22.265),
              --('2025-01-24', 'WT-004', 28.525),
              --('2025-01-24', 'WT-005', 27.357),
              --('2025-01-25', 'WT-001', 23.897),
              --('2025-01-25', 'WT-002', 26.543),
              --('2025-01-25', 'WT-003', 33.877),
              --('2025-01-25', 'WT-004', 26.088),
              --('2025-01-25', 'WT-005', 24.985),
              --('2025-01-26', 'WT-001', 35.553),
              --('2025-01-26', 'WT-002', 42.639),
              --('2025-01-26', 'WT-003', 32.02),
              --('2025-01-26', 'WT-004', 38.162),
              --('2025-01-26', 'WT-005', 39.44),
              --('2025-01-27', 'WT-001', 21.475),
              --('2025-01-27', 'WT-002', 26.184),
              --('2025-01-27', 'WT-003', 22.725),
              --('2025-01-27', 'WT-004', 25.263),
              --('2025-01-27', 'WT-005', 25.275),
              --('2025-01-28', 'WT-001', 30.718),
              --('2025-01-28', 'WT-002', 25.322),
              --('2025-01-28', 'WT-003', 34.347),
              --('2025-01-28', 'WT-004', 28.114),
              --('2025-01-28', 'WT-005', 26.487);
            """;
        tableCmd.ExecuteNonQuery();
    }
    
    public void InitializePreTestDB()
    {
        var connString = config.GetConnectionString("PreTest");
        using var connection = new  DuckDBConnection(connString);
        connection.Open();

        var tableCmd = connection.CreateCommand();
        tableCmd.CommandText =
            """
            DROP TABLE IF EXISTS coffee_sales;
            DROP TABLE IF EXISTS sale;
            DROP TABLE IF EXISTS coffees;
            DROP TABLE IF EXISTS employees;
            DROP TABLE IF EXISTS departments;

            -- Departments
            CREATE TABLE departments (
                department_id INTEGER PRIMARY KEY,
                name TEXT
            );

            -- Employees (users)
            CREATE TABLE employees (
                user_id INTEGER PRIMARY KEY,
                department_id INTEGER,
                name TEXT,
                salary INTEGER,
                FOREIGN KEY (department_id) REFERENCES departments(department_id)
            );

            -- Coffees
            CREATE TABLE coffees (
                coffee_id INTEGER PRIMARY KEY,
                name TEXT,
                price DECIMAL(6,2)
            );

            -- Sales
            CREATE TABLE sale (
                sale_id INTEGER PRIMARY KEY,
                user_id INTEGER,
                date TEXT,
                FOREIGN KEY (user_id) REFERENCES employees(user_id)
            );

            -- Coffee sales (many-to-many)
            CREATE TABLE coffee_sales (
                sale_id INTEGER,
                coffee_id INTEGER,
                PRIMARY KEY (sale_id, coffee_id),
                FOREIGN KEY (sale_id) REFERENCES sale(sale_id),
                FOREIGN KEY (coffee_id) REFERENCES coffees(coffee_id)
            );

            -- Insert departments
            INSERT INTO departments VALUES
            (1, 'Barista'),
            (2, 'Management'),
            (3, 'Support');

            -- Insert employees
            INSERT INTO employees VALUES
            (1, 1, 'Alice', 28000),
            (2, 1, 'Bob', 27000),
            (3, 2, 'Charlie', 40000),
            (4, 3, 'Diana', 30000),
            (5, 1, 'Eve', 26000),
            (6, 3, 'Frank', 31000);

            -- Insert coffees
            INSERT INTO coffees VALUES
            (1, 'Espresso', 3.00),
            (2, 'Americano', 3.50),
            (3, 'Latte', 4.50),
            (4, 'Cappuccino', 4.00),
            (5, 'Mocha', 5.00);

            -- Insert sales (10 sales across different employees)
            INSERT INTO sale VALUES
            (1, 1, '2026-03-01'),
            (2, 2, '2026-03-01'),
            (3, 1, '2026-03-01'),
            (4, 3, '2026-03-02'),
            (5, 4, '2026-03-02'),
            (6, 5, '2026-03-03'),
            (7, 2, '2026-03-03'),
            (8, 6, '2026-03-03'),
            (9, 1, '2026-03-04'),
            (10, 5, '2026-03-04');

            -- Insert coffee_sales (multiple coffees per sale for richer queries)
            INSERT INTO coffee_sales VALUES
            (1, 1),
            (1, 3),
            (2, 2),
            (3, 4),
            (3, 1),
            (4, 5),
            (5, 3),
            (5, 4),
            (6, 2),
            (6, 1),
            (7, 3),
            (8, 5),
            (8, 2),
            (9, 4),
            (9, 3),
            (10, 1),
            (10, 2),
            (10, 5);
            """;
        tableCmd.ExecuteNonQuery();
    }

    public void InitializePostTestDB()
    {
        var connString = config.GetConnectionString("PostTest");
        using var connection = new  DuckDBConnection(connString);
        connection.Open();

        var tableCmd = connection.CreateCommand();
        tableCmd.CommandText =
            """
            -- Drop tables if they already exist
            DROP TABLE IF EXISTS sales_products;
            DROP TABLE IF EXISTS sales;
            DROP TABLE IF EXISTS products;
            DROP TABLE IF EXISTS users;
            DROP TABLE IF EXISTS cashiers;
            
            -- Users table
            CREATE TABLE users (
                user_id INTEGER PRIMARY KEY,
                name TEXT,
                email TEXT
            );
            
            -- Cashiers table
            CREATE TABLE cashiers (
                cashier_id INTEGER PRIMARY KEY,
                name TEXT
            );
            
            -- Products table
            CREATE TABLE products (
                product_id INTEGER PRIMARY KEY,
                name TEXT,
                price DECIMAL(10,2)
            );
            
            -- Sales table
            CREATE TABLE sales (
                sale_id INTEGER PRIMARY KEY,
                user_id INTEGER,
                cashier_id INTEGER,
                sale_date TIMESTAMP,
                FOREIGN KEY (user_id) REFERENCES users(user_id),
                FOREIGN KEY (cashier_id) REFERENCES cashiers(cashier_id)
            );
            
            -- Sales_Products table (many-to-many: sales ↔ products)
            CREATE TABLE sales_products (
                sale_id INTEGER,
                product_id INTEGER,
                quantity INTEGER,
                PRIMARY KEY (sale_id, product_id),
                FOREIGN KEY (sale_id) REFERENCES sales(sale_id),
                FOREIGN KEY (product_id) REFERENCES products(product_id)
            );
            
            -- Insert users (5 users)
            INSERT INTO users VALUES
            (1, 'Alice', 'alice@example.com'),
            (2, 'Bob', 'bob@example.com'),
            (3, 'Charlie', 'charlie@example.com'),
            (4, 'Diana', 'diana@example.com'),
            (5, 'Eve', 'eve@example.com');
            
            -- Insert cashiers (3 cashiers)
            INSERT INTO cashiers VALUES
            (1, 'Martina'),
            (2, 'Abraham'),
            (3, 'Thresa');
            
            -- Insert products (5 products)
            INSERT INTO products VALUES
            (1, 'Laptop', 1200.00),
            (2, 'Phone', 800.00),
            (3, 'Headphones', 150.00),
            (4, 'Keyboard', 100.00),
            (5, 'Mouse', 50.00);
            
            -- Insert sales (10 sales referencing users & cashiers)
            INSERT INTO sales VALUES
            (1, 1, 1, '2026-01-01 10:00:00'),
            (2, 2, 2, '2026-01-02 11:00:00'),
            (3, 3, 3, '2026-01-03 12:00:00'),
            (4, 4, 1, '2026-01-04 13:00:00'),
            (5, 5, 2, '2026-01-05 14:00:00'),
            (6, 1, 3, '2026-01-06 15:00:00'),
            (7, 2, 1, '2026-01-07 16:00:00'),
            (8, 3, 2, '2026-01-08 17:00:00'),
            (9, 4, 3, '2026-01-09 18:00:00'),
            (10, 5, 1, '2026-01-10 19:00:00');
            
            -- Insert sales_products (linking products to sales)
            INSERT INTO sales_products VALUES
            (1, 1, 1),
            (1, 5, 2),
            (2, 2, 1),
            (2, 3, 1),
            (3, 3, 2),
            (3, 4, 1),
            (4, 1, 1),
            (4, 2, 1),
            (5, 5, 3),
            (6, 4, 2),
            (6, 5, 1),
            (7, 2, 2),
            (8, 1, 1),
            (8, 3, 1),
            (9, 4, 1),
            (9, 5, 2),
            (10, 1, 1),
            (10, 2, 1),
            (10, 3, 1);
            """;
        tableCmd.ExecuteNonQuery();
    }
    
    
    public void InitializeMetrics()
    {
        var connString = config.GetConnectionString("Metrics");
        using var connection = new  DuckDBConnection(connString);
        connection.Open();

        var tableCmd = connection.CreateCommand();
        tableCmd.CommandText =
            """
            
            CREATE SEQUENCE IF NOT EXISTS queries_written_id_seq START 1;
            CREATE SEQUENCE IF NOT EXISTS time_spent_id_seq START 1;
            CREATE SEQUENCE IF NOT EXISTS action_keyword_metrics_id_seq START 1;
            -- ACTIONS
            CREATE TABLE IF NOT EXISTS button_action_counts (
                session_id      VARCHAR NOT NULL,
                action_name     VARCHAR NOT NULL,
                action_count    BIGINT  NOT NULL DEFAULT 0,
                
                PRIMARY KEY (session_id, action_name)
            );

            -- QUERIES
            CREATE TABLE IF NOT EXISTS queries_written (
                id              BIGINT DEFAULT nextval('queries_written_id_seq'),
                session_id      VARCHAR NOT NULL,
                query_string    VARCHAR NOT NULL,
                event_ts        TIMESTAMP NOT NULL,
                                                       
                PRIMARY KEY (id, session_id)
            );

            -- TIME
            CREATE TABLE IF NOT EXISTS time_spent (
                id              BIGINT DEFAULT nextval('time_spent_id_seq'),
                session_id      VARCHAR NOT NULL,
                step            VARCHAR NOT NULL,
                time_spent_ms   BIGINT NOT NULL,
                animation_ms    BIGINT NOT NULL,
                event_ts        TIMESTAMP NOT NULL,
                                                  
                PRIMARY KEY (id, session_id)
            );

            CREATE TABLE IF NOT EXISTS action_keyword_metrics (
                id BIGINT DEFAULT nextval('action_keyword_metrics_id_seq'),
                session_id VARCHAR NOT NULL,
                action_type VARCHAR NOT NULL,
                sql_keyword VARCHAR NOT NULL,
                count    BIGINT  NOT NULL DEFAULT 0,
                UNIQUE (session_id, action_type, sql_keyword)
            );

            CREATE TABLE IF NOT EXISTS keyword_animation_view_percentage (
                sql_keyword VARCHAR NOT NULL,
                total_percentage_sum DOUBLE NOT NULL DEFAULT 0,
                view_count BIGINT NOT NULL DEFAULT 0,
                
                PRIMARY KEY (sql_keyword)
            );
            """;
        tableCmd.ExecuteNonQuery();
    }
    
    public void InitializeUser()
    {
        var connString = config.GetConnectionString("User");
        using var connection = new  DuckDBConnection(connString);
        connection.Open();
        
        var tableCmd = connection.CreateCommand();
        tableCmd.CommandText = 
            """
            CREATE TABLE IF NOT EXISTS user_queries (
                session_id TEXT PRIMARY KEY, query TEXT
            );
            
            CREATE TABLE IF NOT EXISTS user_databases (
                session_id string, 
                database_path string,
                PRIMARY KEY (session_id, database_path)
            );
            """;
        tableCmd.ExecuteNonQuery();
    }
}