using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RWF.DedicatedServer.Core;
using System.Diagnostics;

namespace RWF.DedicatedServer.Services;

public interface IDebugConsoleService
{
    Task ProcessCommandAsync(string command);
    void PrintDebugInfo();
    void PrintServerStats();
    void PrintPlayerList();
    void PrintMemoryUsage();
}

public class DebugConsoleService : BackgroundService, IDebugConsoleService
{
    private readonly ILogger<DebugConsoleService> _logger;
    private readonly DebugConfiguration _debugConfig;
    private readonly IServerManager _serverManager;
    private readonly IPlayerManager _playerManager;
    private readonly IGameSessionManager _sessionManager;
    private readonly Stopwatch _uptime;

    public DebugConsoleService(
        ILogger<DebugConsoleService> logger,
        IOptions<DebugConfiguration> debugConfig,
        IServerManager serverManager,
        IPlayerManager playerManager,
        IGameSessionManager sessionManager)
    {
        _logger = logger;
        _debugConfig = debugConfig.Value;
        _serverManager = serverManager;
        _playerManager = playerManager;
        _sessionManager = sessionManager;
        _uptime = Stopwatch.StartNew();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_debugConfig.EnableDebugConsole)
        {
            _logger.LogInformation("Debug console is disabled");
            return;
        }

        _logger.LogInformation("Debug console started. Type 'help' for available commands.");
        _logger.LogInformation("Debug Console Commands: help, status, players, memory, sessions, gc, exit");

        await Task.Run(async () =>
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    Console.Write("[DEBUG] > ");
                    var input = Console.ReadLine();
                    
                    if (!string.IsNullOrWhiteSpace(input))
                    {
                        await ProcessCommandAsync(input.Trim());
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing debug command");
                }
            }
        }, stoppingToken);
    }

    public async Task ProcessCommandAsync(string command)
    {
        var parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return;

        var cmd = parts[0].ToLowerInvariant();

        try
        {
            switch (cmd)
            {
                case "help":
                    PrintHelp();
                    break;

                case "status":
                    PrintServerStats();
                    break;

                case "debug":
                    PrintDebugInfo();
                    break;

                case "players":
                    PrintPlayerList();
                    break;

                case "sessions":
                    PrintSessions();
                    break;

                case "memory":
                    PrintMemoryUsage();
                    break;

                case "gc":
                    ForceGarbageCollection();
                    break;

                case "config":
                    PrintConfiguration();
                    break;

                case "logs":
                    if (parts.Length > 1)
                    {
                        await ChangeLogLevel(parts[1]);
                    }
                    else
                    {
                        Console.WriteLine("Usage: logs <level> (Trace|Debug|Information|Warning|Error|Critical)");
                    }
                    break;

                case "clear":
                    Console.Clear();
                    break;

                case "exit":
                case "quit":
                    Console.WriteLine("Debug console command received, but server continues running...");
                    break;

                default:
                    Console.WriteLine($"Unknown command: {cmd}. Type 'help' for available commands.");
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error executing command '{cmd}': {ex.Message}");
            if (_debugConfig.EnableStackTraces)
            {
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }

    private void PrintHelp()
    {
        Console.WriteLine();
        Console.WriteLine("=== DEBUG CONSOLE COMMANDS ===");
        Console.WriteLine("help          - Show this help message");
        Console.WriteLine("status        - Show server status and statistics");
        Console.WriteLine("debug         - Show detailed debug information");
        Console.WriteLine("players       - List connected players");
        Console.WriteLine("sessions      - List active game sessions");
        Console.WriteLine("memory        - Show memory usage information");
        Console.WriteLine("gc            - Force garbage collection");
        Console.WriteLine("config        - Show current configuration");
        Console.WriteLine("logs <level>  - Change log level (Trace|Debug|Information|Warning|Error|Critical)");
        Console.WriteLine("clear         - Clear the console");
        Console.WriteLine("exit/quit     - Exit debug console (server continues)");
        Console.WriteLine();
    }

    public void PrintServerStats()
    {
        var status = _serverManager.GetStatus();
        var uptime = _uptime.Elapsed;

        Console.WriteLine();
        Console.WriteLine("=== SERVER STATUS ===");
        Console.WriteLine($"Name: {status.Name}");
        Console.WriteLine($"Version: {status.Version}");
        Console.WriteLine($"State: {status.State}");
        Console.WriteLine($"Uptime: {uptime:dd\\.hh\\:mm\\:ss}");
        Console.WriteLine($"Players: {status.CurrentPlayers}/{status.MaxPlayers}");
        Console.WriteLine($"Active Sessions: {status.ActiveSessions.Count}");
        Console.WriteLine($"Process ID: {Process.GetCurrentProcess().Id}");
        Console.WriteLine($"Threads: {Process.GetCurrentProcess().Threads.Count}");
        Console.WriteLine();
    }

    public void PrintDebugInfo()
    {
        Console.WriteLine();
        Console.WriteLine("=== DEBUG INFORMATION ===");
        Console.WriteLine($"Debug Console: {_debugConfig.EnableDebugConsole}");
        Console.WriteLine($"Health Monitoring: {_debugConfig.EnableHealthMonitoring}");
        Console.WriteLine($"Performance Metrics: {_debugConfig.EnablePerformanceMetrics}");
        Console.WriteLine($"Stack Traces: {_debugConfig.EnableStackTraces}");
        Console.WriteLine($"Memory Logging: {_debugConfig.LogMemoryUsage}");
        Console.WriteLine($"CLR Version: {Environment.Version}");
        Console.WriteLine($"OS Version: {Environment.OSVersion}");
        Console.WriteLine($"Machine Name: {Environment.MachineName}");
        Console.WriteLine($"User: {Environment.UserName}");
        Console.WriteLine($"Working Directory: {Environment.CurrentDirectory}");
        Console.WriteLine();
    }

    public void PrintPlayerList()
    {
        var players = _playerManager.GetAllPlayers();
        
        Console.WriteLine();
        Console.WriteLine("=== CONNECTED PLAYERS ===");
        if (players.Any())
        {
            Console.WriteLine($"{"ID",-5} {"Name",-20} {"IP Address",-15} {"Ready",-8} {"Connected",-20}");
            Console.WriteLine(new string('-', 70));
            foreach (var player in players)
            {
                Console.WriteLine($"{player.Id,-5} {player.Name,-20} {player.IpAddress,-15} {player.IsReady,-8} {player.ConnectedAt:yyyy-MM-dd HH:mm:ss}");
            }
        }
        else
        {
            Console.WriteLine("No players connected");
        }
        Console.WriteLine();
    }

    private void PrintSessions()
    {
        var sessions = _sessionManager.GetActiveSessions();
        
        Console.WriteLine();
        Console.WriteLine("=== ACTIVE SESSIONS ===");
        if (sessions.Any())
        {
            foreach (var session in sessions)
            {
                Console.WriteLine($"Session {session.Id}: {session.GameMode} - {session.Players.Count} players");
                Console.WriteLine($"  State: {session.State}");
                Console.WriteLine($"  Created: {session.CreatedAt:yyyy-MM-dd HH:mm:ss}");
                if (session.Players.Any())
                {
                    Console.WriteLine($"  Players: {string.Join(", ", session.Players.Select(p => p.Name))}");
                }
                Console.WriteLine();
            }
        }
        else
        {
            Console.WriteLine("No active sessions");
        }
        Console.WriteLine();
    }

    public void PrintMemoryUsage()
    {
        var process = Process.GetCurrentProcess();
        var gc = GC.GetTotalMemory(false);
        
        Console.WriteLine();
        Console.WriteLine("=== MEMORY USAGE ===");
        Console.WriteLine($"Working Set: {process.WorkingSet64 / (1024 * 1024):N0} MB");
        Console.WriteLine($"Private Memory: {process.PrivateMemorySize64 / (1024 * 1024):N0} MB");
        Console.WriteLine($"GC Memory: {gc / (1024 * 1024):N0} MB");
        Console.WriteLine($"Gen 0 Collections: {GC.CollectionCount(0)}");
        Console.WriteLine($"Gen 1 Collections: {GC.CollectionCount(1)}");
        Console.WriteLine($"Gen 2 Collections: {GC.CollectionCount(2)}");
        
        for (int i = 0; i <= GC.MaxGeneration; i++)
        {
            Console.WriteLine($"Gen {i} Memory: {GC.GetTotalMemory(false) / (1024 * 1024):N0} MB");
        }
        Console.WriteLine();
    }

    private void ForceGarbageCollection()
    {
        var beforeGC = GC.GetTotalMemory(false);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var afterGC = GC.GetTotalMemory(false);
        
        Console.WriteLine();
        Console.WriteLine("=== GARBAGE COLLECTION ===");
        Console.WriteLine($"Memory before GC: {beforeGC / (1024 * 1024):N0} MB");
        Console.WriteLine($"Memory after GC: {afterGC / (1024 * 1024):N0} MB");
        Console.WriteLine($"Memory freed: {(beforeGC - afterGC) / (1024 * 1024):N0} MB");
        Console.WriteLine();
    }

    private void PrintConfiguration()
    {
        Console.WriteLine();
        Console.WriteLine("=== CONFIGURATION ===");
        Console.WriteLine($"Debug Console: {_debugConfig.EnableDebugConsole}");
        Console.WriteLine($"Health Monitoring: {_debugConfig.EnableHealthMonitoring}");
        Console.WriteLine($"Health Check Interval: {_debugConfig.HealthCheckInterval}s");
        Console.WriteLine($"Performance Metrics: {_debugConfig.EnablePerformanceMetrics}");
        Console.WriteLine($"Stack Traces: {_debugConfig.EnableStackTraces}");
        Console.WriteLine($"Memory Logging: {_debugConfig.LogMemoryUsage}");
        Console.WriteLine();
    }

    private async Task ChangeLogLevel(string level)
    {
        Console.WriteLine($"Log level change requested to: {level}");
        Console.WriteLine("Note: Dynamic log level changes require additional configuration.");
        Console.WriteLine("Consider restarting the server with updated appsettings.json");
        await Task.CompletedTask;
    }
}