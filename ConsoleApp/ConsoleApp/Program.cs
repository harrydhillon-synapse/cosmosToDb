
using ConsoleApp.Data;
using ConsoleApp.Service;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

class Program
{
    public static void Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();
        
        var logProcessor = host.Services.GetRequiredService<LogProcessor>();
        logProcessor?.ProcessLogs();
        
    }
    
    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            })
            .ConfigureServices((context, services) =>
            {
                services.AddDbContext<CosmosDbContext>(options =>
                {
                    options.UseCosmos(
                        context.Configuration["CosmosDb:Endpoint"],
                        context.Configuration["CosmosDb:PrimaryKey"],
                        context.Configuration["CosmosDb:DatabaseName"]);
                });
                services.AddTransient<LogProcessor>();
            });
}