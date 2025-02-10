using ConsoleApp.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ConsoleApp.Data;

public class CosmosDbContext
{
    private readonly IConfiguration _configuration;
    private readonly CosmosClient _cosmosClient;
    private readonly Container _container;
    private readonly ILogger<CosmosDbContext> _logger;

    public CosmosDbContext(IConfiguration configuration, ILogger<CosmosDbContext> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _cosmosClient = new CosmosClient(_configuration["CosmosDb:Endpoint"], _configuration["CosmosDb:PrimaryKey"]);
        var database = _cosmosClient.GetDatabase(_configuration["CosmosDb:DatabaseName"]);
        _container = database.GetContainer("logs");
    }

    public async Task<List<CosmosLogItem>> GetLogItemsAsync()
    {
        var query = new QueryDefinition(
            "SELECT * FROM c WHERE c.nikoOrderId != null ORDER BY c.DateTime");

        var iterator = _container.GetItemQueryIterator<CosmosLogItem>(query);
        var results = new List<CosmosLogItem>();

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
            _logger.LogError(ex, "Connection check failed");
            return false;
        }
    }
}