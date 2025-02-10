using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace ConsoleApp.Service
{
    public static class DatabaseInitializer
    {
        public static void InitializeDatabase(string connectionString, ILogger logger)
        {
            try
            {
                logger.LogInformation("Initializing database with connection string: {ConnectionString}",
                    connectionString);

                var connectionStringBuilder = new SqliteConnectionStringBuilder(connectionString);
                var databasePath = Path.GetFullPath(connectionStringBuilder.DataSource);

                logger.LogInformation("Database file will be created at: {DatabasePath}", databasePath);

                using (var connection = new SqliteConnection(connectionString))
                {
                    connection.Open();
                    logger.LogInformation("Database connection opened.");

                    CreateTable(connection, logger, "orders", @"
                                            CREATE TABLE IF NOT EXISTS orders (
                                                order_id TEXT PRIMARY KEY,
                                                niko_order_id TEXT,
                                                url TEXT,
                                                status_code INTEGER,
                                                last_failed_at TEXT,
                                                completed_at TEXT,
                                                process_count INTEGER DEFAULT 1
                                            );");

                    CreateTable(connection, logger, "order_failures", @"
                                            CREATE TABLE IF NOT EXISTS order_failures (
                                                id INTEGER PRIMARY KEY AUTOINCREMENT,
                                                order_id TEXT,
                                                niko_order_id TEXT,
                                                failure_timestamp TEXT,
                                                url TEXT,
                                                status_code INTEGER,
                                                failure_reason TEXT,
                                                FOREIGN KEY (order_id) REFERENCES orders(order_id)
                                            );");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while initializing the database.");
            }
        }

        private static void CreateTable(SqliteConnection connection, ILogger logger, string tableName,
            string createTableSql)
        {
            var tableExistsCommand =
                new SqliteCommand($"SELECT name FROM sqlite_master WHERE type='table' AND name='{tableName}';",
                    connection);
            var tableExists = tableExistsCommand.ExecuteScalar() != null;

            using (var command = new SqliteCommand(createTableSql, connection))
            {
                command.ExecuteNonQuery();
            }

            if (tableExists)
            {
                logger.LogInformation("{TableName} table already exists.", tableName);
            }
            else
            {
                logger.LogInformation("{TableName} table created.", tableName);
            }
        }
    }
}