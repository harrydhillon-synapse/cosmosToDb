using Microsoft.Data.SqlClient;
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
                logger.LogInformation("Initializing database with connection string: {ConnectionString}", connectionString);

                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    logger.LogInformation("Database connection opened.");

                    CreateTable(connection, logger, "orders", @"
                            CREATE TABLE orders (
                                niko_order_id NVARCHAR(50) PRIMARY KEY,
                                url NVARCHAR(200),
                                status_code INT,
                                first_attempt_at DATETIME,
                                last_failed_at DATETIME,
                                completed_at DATETIME,
                                process_count INT DEFAULT 1
                            );");

                    CreateTable(connection, logger, "order_failures", @"
                            CREATE TABLE order_failures (
                                id INT IDENTITY(1,1) PRIMARY KEY,
                                niko_order_id NVARCHAR(50),
                                failure_timestamp DATETIME,
                                url NVARCHAR(200),
                                status_code INT,
                                failure_reason NTEXT,
                                FOREIGN KEY (niko_order_id) REFERENCES orders(niko_order_id)
                            );");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while initializing the database.");
            }
        }

        private static void CreateTable(SqlConnection connection, ILogger logger, string tableName, string createTableSql)
        {
            var tableExistsCommand = new SqlCommand($"SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{tableName}';", connection);
            var tableExists = tableExistsCommand.ExecuteScalar() != null;

            if (tableExists)
            {
                logger.LogInformation("{TableName} table already exists.", tableName);
            }
            else
            {
                using (var command = new SqlCommand(createTableSql, connection))
                {
                    command.ExecuteNonQuery();
                }
                logger.LogInformation("{TableName} table created.", tableName);
            }
        }
    }
}