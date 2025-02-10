using ConsoleApp.Data;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using ConsoleApp.Models;
using Newtonsoft.Json;

namespace ConsoleApp.Service
{
    /// <summary>
    /// Processes logs from Cosmos DB and inserts them into a local SQLite database.
    /// </summary>
    public class LogProcessor
    {
        private readonly CosmosDbContext _cosmosDbContext;
        private readonly ILogger<LogProcessor> _logger;
        private readonly string _sqliteConnectionString;

        /// <summary>
        /// Initializes a new instance of the <see cref="LogProcessor"/> class.
        /// </summary>
        /// <param name="cosmosDbContext">The Cosmos DB context.</param>
        /// <param name="logger">The logger instance.</param>
        /// <param name="sqliteConnectionString">The SQLite connection string.</param>
        public LogProcessor(CosmosDbContext cosmosDbContext, ILogger<LogProcessor> logger,
            string sqliteConnectionString)
        {
            _cosmosDbContext = cosmosDbContext;
            _logger = logger;
            _sqliteConnectionString = sqliteConnectionString;
        }

        /// <summary>
        /// Processes logs from Cosmos DB and inserts them into the local SQLite database.
        /// </summary>
        public async Task ProcessLogsAsync()
        {
            _logger.LogInformation("ProcessLogs method called.");

            var logs = await _cosmosDbContext.GetLogItemsAsync();
            _logger.LogInformation("Number of logs retrieved: {Count}", logs.Count);

            using (var connection = new SqliteConnection(_sqliteConnectionString))
            {
                connection.Open();
                foreach (var log in logs)
                {
                    InsertOrder(connection, log);
                    if (log.StatusCode != 200)
                    {
                        InsertOrderFailure(connection, log);
                        UpdateOrderWithFailure(connection, log);
                    }
                }
            }
        }

        /// <summary>
        /// Inserts an order log into the orders table.
        /// </summary>
        /// <param name="connection">The SQLite connection.</param>
        /// <param name="log">The log item to insert.</param>
        private void InsertOrder(SqliteConnection connection, CosmosLogItem log)
        {
            var command = new SqliteCommand(@"
                        INSERT INTO orders (order_id, niko_order_id, url, status_code, completed_at, process_count)
                        VALUES (@OrderId, @NikoOrderId, @Url, @StatusCode, @CompletedAt, 1)
                        ON CONFLICT(order_id) DO UPDATE SET
                            niko_order_id = excluded.niko_order_id,
                            url = excluded.url,
                            status_code = excluded.status_code,
                            completed_at = excluded.completed_at,
                            process_count = orders.process_count + 1;", connection);

            command.Parameters.AddWithValue("@OrderId", log.Id);
            command.Parameters.AddWithValue("@NikoOrderId", log.NikoOrderId);
            command.Parameters.AddWithValue("@Url", log.Url);
            command.Parameters.AddWithValue("@StatusCode", log.StatusCode);
            command.Parameters.AddWithValue("@CompletedAt", log.DateTime);

            command.ExecuteNonQuery();
            _logger.LogInformation("Inserted or updated order with Id: {Id}", log.Id);
        }

        /// <summary>
        /// Inserts a failed order log into the order_failures table.
        /// </summary>
        /// <param name="connection">The SQLite connection.</param>
        /// <param name="log">The log item to insert.</param>
        private void InsertOrderFailure(SqliteConnection connection, CosmosLogItem log)
        {
            var (errorCode, message) = GetErrorDetails(log);
            var failureReason = $"{errorCode}: {message}";

            var command = new SqliteCommand(@"
                        INSERT INTO order_failures (order_id, niko_order_id, failure_timestamp, url, status_code, failure_reason)
                        VALUES (@OrderId, @NikoOrderId, @FailureTimestamp, @Url, @StatusCode, @FailureReason);",
                connection);

            command.Parameters.AddWithValue("@OrderId", log.Id);
            command.Parameters.AddWithValue("@NikoOrderId", log.NikoOrderId);
            command.Parameters.AddWithValue("@FailureTimestamp", log.DateTime);
            command.Parameters.AddWithValue("@Url", log.Url);
            command.Parameters.AddWithValue("@StatusCode", log.StatusCode);
            command.Parameters.AddWithValue("@FailureReason", failureReason);

            command.ExecuteNonQuery();
            _logger.LogInformation("Inserted order failure with Id: {Id}", log.Id);
        }

        /// <summary>
        /// Updates the orders table with the failure date for a given order.
        /// </summary>
        /// <param name="connection">The SQLite connection.</param>
        /// <param name="log">The log item to update.</param>
        private void UpdateOrderWithFailure(SqliteConnection connection, CosmosLogItem log)
        {
            var command = new SqliteCommand(@"
                        UPDATE orders
                        SET last_failed_at = @LastFailedAt
                        WHERE order_id = @OrderId", connection);

            command.Parameters.AddWithValue("@OrderId", log.Id);
            command.Parameters.AddWithValue("@LastFailedAt", log.DateTime);

            command.ExecuteNonQuery();
            _logger.LogInformation("Updated order with failure date for Id: {Id}", log.Id);
        }

        /// <summary>
        /// Extracts error details from the log item.
        /// </summary>
        /// <param name="logItem">The log item to extract error details from.</param>
        /// <returns>A tuple containing the error code and message.</returns>
        private (string ErrorCode, string Message) GetErrorDetails(CosmosLogItem logItem)
        {
            if (logItem.StatusCode != 200 && !string.IsNullOrEmpty(logItem.Response?.Payload))
            {
                dynamic payload = JsonConvert.DeserializeObject<dynamic>(logItem.Response?.Payload);
                var errorCode = (string?)payload?.issue[0]?.details?.coding[0]?.code ?? "Unknown";
                var message = (string?)payload?.issue[0]?.details?.coding[0]?.display ?? "Unknown";
                return (errorCode, message);
            }

            return ("Unknown", "Unknown");
        }
    }
}