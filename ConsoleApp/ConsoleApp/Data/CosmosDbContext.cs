using ConsoleApp.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;

namespace ConsoleApp.Data;

public class CosmosDbContext
{
    private readonly IConfiguration _configuration;
    private readonly CosmosClient _cosmosClient;
    private readonly Container _container;

    public CosmosDbContext(IConfiguration configuration)
    {
        _configuration = configuration;
        _cosmosClient = new CosmosClient(_configuration["CosmosDb:Endpoint"], _configuration["CosmosDb:PrimaryKey"]);
        var database = _cosmosClient.GetDatabase(_configuration["CosmosDb:DatabaseName"]);
        _container = database.GetContainer("logs");
    }

    public async Task<List<LogItem>> GetLogItemsAsync(int take = 5)
    {
        var query = new QueryDefinition("SELECT * FROM c ORDER BY c.DateTime OFFSET 0 LIMIT @take")
            .WithParameter("@take", take);

        var iterator = _container.GetItemQueryIterator<LogItem>(query);
        var results = new List<LogItem>();

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            results.AddRange(response);
        }

        return results;
    }

    public async Task<bool> CheckConnectionAsync()
    {
        try
        {
            var response = await _container.ReadContainerAsync();
            return response.StatusCode == System.Net.HttpStatusCode.OK;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Connection check failed: {ex.Message}");
            return false;
        }
    }
}