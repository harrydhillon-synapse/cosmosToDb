using ConsoleApp.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ConsoleApp.Data;

public class CosmosDbContext : DbContext
{
    private readonly IConfiguration _configuration;
    
    public CosmosDbContext(DbContextOptions<CosmosDbContext> options, IConfiguration configuration) 
        : base(options)
    {
        _configuration = configuration;
    }
    
    public DbSet<LogItem> LogItems { get; set; }

    protected void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var endpoint = _configuration["CosmosDb:Endpoint"];
        var key = _configuration["CosmosDb:PrimaryKey"];
        var databaseName = _configuration["CosmosDb:DatabaseName"];

        optionsBuilder.UseCosmos(endpoint, key, databaseName);
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LogItem>().ToContainer("logs"); //container name
        modelBuilder.Entity<LogItem>().HasKey(log => log.Id);
    }
    
    public async Task<bool> CheckConnectionAsync()
    {
        try
        {
            var client = new CosmosClient(_configuration["CosmosDb:Endpoint"], _configuration["CosmosDb:PrimaryKey"]);
            var database = client.GetDatabase(_configuration["CosmosDb:DatabaseName"]);
            var container = database.GetContainer("logs");
            var response = await container.ReadContainerAsync();
            return response.StatusCode == System.Net.HttpStatusCode.OK;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Connection check failed: {ex.Message}");
            return false;
        }
    }
}