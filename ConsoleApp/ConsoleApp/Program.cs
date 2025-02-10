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
        var sqliteConnectionString = host.Services.GetRequiredService<string>();

        if (string.IsNullOrEmpty(sqliteConnectionString))
        {
            logger.LogError("SQLite connection string is null or empty.");
            return;
        }

        logger.LogInformation("Starting database initialization.");
        DatabaseInitializer.InitializeDatabase(sqliteConnectionString, logger);

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
                var sqliteConnectionString = configuration.GetSection("Sqlite").GetValue<string>("ConnectionString");

                if (string.IsNullOrEmpty(sqliteConnectionString))
                {
                    throw new InvalidOperationException("SQLite connection string is null or empty.");
                }

                services.AddSingleton<CosmosDbContext>();
                services.AddTransient<LogProcessor>();
                services.AddLogging(configure => configure.AddConsole());
                services.AddSingleton(sqliteConnectionString);
            });
}