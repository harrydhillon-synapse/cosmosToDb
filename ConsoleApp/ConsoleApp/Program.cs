using ConsoleApp.Data;
using ConsoleApp.Service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

class Program
{
    public static async Task Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();

        var logger = host.Services.GetRequiredService<ILogger<Program>>();
        var sqlServerConnectionString = host.Services.GetRequiredService<string>();

        if (string.IsNullOrEmpty(sqlServerConnectionString))
        {
            logger.LogError("SQL Server connection string is null or empty.");
            return;
        }

        logger.LogInformation("Starting database initialization.");
        DatabaseInitializer.InitializeDatabase(sqlServerConnectionString, logger);

        var dbContext = host.Services.GetRequiredService<CosmosDbContext>();
        var isConnected = await dbContext.CheckConnectionAsync();
        logger.LogInformation($"Database connection status: {(isConnected ? "Connected" : "Not Connected")}");

        if (isConnected)
        {
            var logProcessor = host.Services.GetRequiredService<LogProcessor>();
            if (logProcessor != null)
            {
                await logProcessor.ProcessLogsAsync();
            }
            else
            {
                logger.LogError("LogProcessor is null.");
            }
        }
    }

    static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            })
            .ConfigureServices((context, services) =>
            {
                var configuration = context.Configuration;
                var sqlServerConnectionString = configuration.GetSection("SqlServer").GetValue<string>("ConnectionString");

                if (string.IsNullOrEmpty(sqlServerConnectionString))
                {
                    throw new InvalidOperationException("SQL Server connection string is null or empty.");
                }

                services.AddSingleton<CosmosDbContext>();
                services.AddTransient<LogProcessor>();
                services.AddLogging(configure => configure.AddConsole());
                services.AddSingleton(sqlServerConnectionString);
            });
}
