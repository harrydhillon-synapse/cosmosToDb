using ConsoleApp.Data;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using ConsoleApp.Models;
using Newtonsoft.Json;
using Microsoft.Data.SqlClient;

namespace ConsoleApp.Service
{
    /// <summary>
    /// Processes logs from Cosmos DB and inserts them into a local SQL Server database.
    /// </summary>
    public class LogProcessor
    {
        private readonly CosmosDbContext _cosmosDbContext;
        private readonly ILogger<LogProcessor> _logger;
        private readonly string _sqlServerConnectionString;

        /// <summary>
        /// Initializes a new instance of the <see cref="LogProcessor"/> class.
        /// </summary>
        /// <param name="cosmosDbContext">The Cosmos DB context.</param>
        /// <param name="logger">The logger instance.</param>
        /// <param name="sqlServerConnectionString">The SQL Server connection string.</param>
        public LogProcessor(CosmosDbContext cosmosDbContext, ILogger<LogProcessor> logger,
            string sqlServerConnectionString)
        {
            _cosmosDbContext = cosmosDbContext;
            _logger = logger;
            _sqlServerConnectionString = sqlServerConnectionString;
        }

        /// <summary>
        /// Processes logs from Cosmos DB and inserts them into the local SQL Server database.
        /// </summary>
        public async Task ProcessLogsAsync()
        {
            _logger.LogInformation("ProcessLogs method called.");

            var logs = await _cosmosDbContext.GetLogItemsAsync();
            _logger.LogInformation("Number of logs retrieved: {Count}", logs.Count);

            using (var connection = new SqlConnection(_sqlServerConnectionString))
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
        /// <param name="connection">The SQL Server connection.</param>
        /// <param name="log">The log item to insert.</param>
        private void InsertOrder(SqlConnection connection, CosmosLogItem log)
        {
            var command = new SqlCommand(@"
                            IF EXISTS (SELECT 1 FROM orders WHERE niko_order_id = @NikoOrderId)
                            BEGIN
                                UPDATE orders
                                SET status_code = @StatusCode,
                                    completed_at = IIF(@StatusCode=200,@CompletedAt,null),
                                    process_count = process_count + 1
                                WHERE niko_order_id = @NikoOrderId;
                            END
                            ELSE
                            BEGIN
                                INSERT INTO orders (niko_order_id, url, status_code, first_attempt_at, completed_at, process_count)
                                VALUES (@NikoOrderId, @Url, @StatusCode, @FirstAttemptAt, IIF(@StatusCode=200,@CompletedAt,null), 1);
                            END", connection);

            //command.Parameters.AddWithValue("@OrderId", log.Id);
            command.Parameters.AddWithValue("@NikoOrderId", log.NikoOrderId);
            command.Parameters.AddWithValue("@Url", log.Url);
            command.Parameters.AddWithValue("@StatusCode", log.StatusCode);
            command.Parameters.AddWithValue("@CompletedAt", log.DateTime);
            command.Parameters.AddWithValue("@FirstAttemptAt", log.DateTime);

            command.ExecuteNonQuery();
            _logger.LogInformation("Inserted or updated order with Id: {Id}", log.Id);
        }

        /// <summary>
        /// Inserts a failed order log into the order_failures table.
        /// </summary>
        /// <param name="connection">The SQL Server connection.</param>
        /// <param name="log">The log item to insert.</param>
        private void InsertOrderFailure(SqlConnection connection, CosmosLogItem log)
        {
            var (errorCode, message) = GetErrorDetails(log);
            var failureReason = $"{errorCode}: {message}";

            var command = new SqlCommand(@"
                            INSERT INTO order_failures (niko_order_id, failure_timestamp, url, status_code, failure_reason)
                            VALUES (@NikoOrderId, @FailureTimestamp, @Url, @StatusCode, @FailureReason);",
                connection);

            //command.Parameters.AddWithValue("@OrderId", log.Id);
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
        /// <param name="connection">The SQL Server connection.</param>
        /// <param name="log">The log item to update.</param>
        private void UpdateOrderWithFailure(SqlConnection connection, CosmosLogItem log)
        {
            var command = new SqlCommand(@"
                            UPDATE orders
                            SET last_failed_at = @LastFailedAt
                            WHERE niko_order_id = @NikoOrderId", connection);

            command.Parameters.AddWithValue("@NikoOrderId", log.NikoOrderId);
            command.Parameters.AddWithValue("@LastFailedAt", log.DateTime);

            command.ExecuteNonQuery();
            _logger.LogInformation("Updated order with failure date for Id: {Id}", log.NikoOrderId);
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