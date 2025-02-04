using ConsoleApp.Data;

namespace ConsoleApp.Service;

public class LogProcessor
{
    private readonly CosmosDbContext _cosmosDbContext;

    public LogProcessor(CosmosDbContext cosmosDbContext)
    {
        _cosmosDbContext = cosmosDbContext;
    }

    public async Task ProcessLogsAsync()
    {
        Console.WriteLine("ProcessLogs method called.");

        var logs = await _cosmosDbContext.GetLogItemsAsync();
        Console.WriteLine($"Number of logs retrieved: {logs.Count}");

        foreach (var log in logs)
        {
            Console.WriteLine($"Id: {log.Id}, DateTime: {log.DateTime}, StatusCode: {log.StatusCode}, " +
                              $"ErrorMessage: {log.ErrorMessage}");
        }
    }
}