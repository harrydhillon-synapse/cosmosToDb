using ConsoleApp.Data;

namespace ConsoleApp.Service;

public class LogProcessor
{
    private readonly CosmosDbContext _cosmosDbContext;
    
    public LogProcessor(CosmosDbContext cosmosDbContext)
    {
        _cosmosDbContext = cosmosDbContext;
    }

    public void ProcessLogs()
    {
        Console.WriteLine("ProcessLogs method called.");

        var logs = _cosmosDbContext.LogItems.Take(5).ToList();
        Console.WriteLine($"Number of logs retrieved: {logs.Count}");

        foreach (var log in logs)
        {
            Console.WriteLine($"Id: {log.Id}, DateTime: {log.DateTime}, StatusCode: {log.statusCode}, " +
                              $"ErrorMessage: {log.ErrorMessage}");
        }
    }
    
}