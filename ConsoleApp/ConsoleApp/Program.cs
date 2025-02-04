using ConsoleApp.Data;
using ConsoleApp.Service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

class Program
{
    public static async Task Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();

        var dbContext = host.Services.GetRequiredService<CosmosDbContext>();
        var isConnected = await dbContext.CheckConnectionAsync();
        Console.WriteLine($"Database connection status: {(isConnected ? "Connected" : "Not Connected")}");

        if (isConnected)
        {
            var logProcessor = host.Services.GetRequiredService<LogProcessor>();
            await logProcessor?.ProcessLogsAsync();
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
                services.AddSingleton<CosmosDbContext>();
                services.AddTransient<LogProcessor>();
            });
}