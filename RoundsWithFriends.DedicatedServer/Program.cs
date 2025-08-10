using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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

        // Logging
        builder.Services.AddLogging(configure => configure.AddConsole());

        // Register services
        builder.Services.Configure<ServerConfiguration>(builder.Configuration.GetSection("Server"));
        builder.Services.AddSingleton<IServerManager, ServerManager>();
        builder.Services.AddSingleton<INetworkService, NetworkService>();
        builder.Services.AddSingleton<IGameSessionManager, GameSessionManager>();
        builder.Services.AddSingleton<IPlayerManager, PlayerManager>();
        
        // Add hosted services
        builder.Services.AddHostedService<ServerHostedService>();

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
            await host.RunAsync();
        }
        catch (Exception ex)
        {
            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            logger.LogCritical(ex, "Fatal error occurred");
            return;
        }

        Console.WriteLine("Server stopped.");
    }
}