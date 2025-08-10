using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RWF.DedicatedServer.Core;
using RWF.DedicatedServer.Services;

namespace RWF.DedicatedServer;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("RoundsWithFriends Dedicated Server");
        Console.WriteLine("===================================");

        var builder = Host.CreateApplicationBuilder(args);

        // Configuration
        builder.Configuration.AddJsonFile("appsettings.json", optional: false);
        builder.Configuration.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true);
        builder.Configuration.AddCommandLine(args);

        // Logging with enhanced console formatting
        builder.Services.AddLogging(configure => 
        {
            configure.AddConsole();
        });

        // Register configurations
        builder.Services.Configure<ServerConfiguration>(builder.Configuration.GetSection("Server"));
        builder.Services.Configure<DebugConfiguration>(builder.Configuration.GetSection("Debug"));
        builder.Services.Configure<GameConfiguration>(builder.Configuration.GetSection("GameModes"));
        builder.Services.Configure<SecurityConfiguration>(builder.Configuration.GetSection("Security"));
        builder.Services.Configure<PerformanceConfiguration>(builder.Configuration.GetSection("Performance"));

        // Register core services
        builder.Services.AddSingleton<IServerManager, ServerManager>();
        builder.Services.AddSingleton<INetworkService, NetworkService>();
        builder.Services.AddSingleton<IGameSessionManager, GameSessionManager>();
        builder.Services.AddSingleton<IPlayerManager, PlayerManager>();
        
        // Register debug and monitoring services
        builder.Services.AddSingleton<IDebugConsoleService, DebugConsoleService>();
        builder.Services.AddSingleton<IHealthMonitorService, HealthMonitorService>();
        
        // Add hosted services
        builder.Services.AddHostedService<ServerHostedService>();
        builder.Services.AddHostedService<DebugConsoleService>();
        builder.Services.AddHostedService<HealthMonitorService>();

        var host = builder.Build();

        // Handle shutdown gracefully
        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true;
            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Shutdown requested...");
            host.StopAsync().Wait();
        };

        try
        {
            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Starting RoundsWithFriends Dedicated Server...");
            logger.LogInformation("Environment: {Environment}", builder.Environment.EnvironmentName);
            
            // Check if debug mode is enabled
            var debugConfig = host.Services.GetRequiredService<IOptions<DebugConfiguration>>().Value;
            if (debugConfig.EnableDebugConsole)
            {
                logger.LogInformation("Debug console enabled - type 'help' for commands");
            }
            
            await host.RunAsync();
        }
        catch (Exception ex)
        {
            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            logger.LogCritical(ex, "Fatal error occurred");
            
            // In debug mode, show more details
            if (builder.Environment.IsDevelopment())
            {
                Console.WriteLine($"Exception Details: {ex}");
            }
            return;
        }

        Console.WriteLine("Server stopped.");
    }
}