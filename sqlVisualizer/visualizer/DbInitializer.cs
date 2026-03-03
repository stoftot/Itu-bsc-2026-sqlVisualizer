using DuckDB.NET.Data;

namespace visualizer;

public class DbInitializer(IConfiguration config)
{
    public void Initialize()
    {
        var connString = config.GetConnectionString("Default");
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
            )
            """;
        tableCmd.ExecuteNonQuery();
    }
}