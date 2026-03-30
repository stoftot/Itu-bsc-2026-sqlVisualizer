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