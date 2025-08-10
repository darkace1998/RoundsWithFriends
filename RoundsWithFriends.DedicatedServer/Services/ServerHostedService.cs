using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RWF.DedicatedServer.Services;

namespace RWF.DedicatedServer.Services;

public class ServerHostedService : BackgroundService
{
    private readonly ILogger<ServerHostedService> _logger;
    private readonly IServerManager _serverManager;
    private readonly INetworkService _networkService;
    private readonly IPlayerManager _playerManager;
    private readonly IGameSessionManager _sessionManager;

    public ServerHostedService(
        ILogger<ServerHostedService> logger,
        IServerManager serverManager,
        INetworkService networkService,
        IPlayerManager playerManager,
        IGameSessionManager sessionManager)
    {
        _logger = logger;
        _serverManager = serverManager;
        _networkService = networkService;
        _playerManager = playerManager;
        _sessionManager = sessionManager;

        // Wire up network events
        _networkService.PlayerConnected += OnPlayerConnected;
        _networkService.PlayerDisconnected += OnPlayerDisconnected;
        _networkService.MessageReceived += OnMessageReceived;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("Starting dedicated server...");
            
            await _serverManager.StartAsync(stoppingToken);
            
            // Keep the service running
            while (!stoppingToken.IsCancellationRequested && _serverManager.IsRunning)
            {
                await Task.Delay(1000, stoppingToken);
                
                // Periodic tasks can be added here
                // Example: health checks, statistics updates, etc.
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Server shutdown requested");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Critical error in server execution");
            throw;
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping dedicated server...");
        
        try
        {
            await _serverManager.StopAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping server");
        }
        
        await base.StopAsync(cancellationToken);
    }

    private async void OnPlayerConnected(object? sender, PlayerConnectedEventArgs e)
    {
        try
        {
            _logger.LogInformation("Player {PlayerId} connected from {IpAddress}", e.PlayerId, e.IpAddress);
            
            var player = await _playerManager.AddPlayerAsync(e.PlayerId, e.IpAddress);
            
            // Send welcome message
            var welcomeMessage = $"Welcome to {_serverManager.GetStatus().Name}!";
            await _networkService.SendToPlayerAsync(e.PlayerId, welcomeMessage);
            
            // Broadcast player joined to others
            var joinMessage = $"Player {player.Name} joined the server";
            await BroadcastToOthersAsync(e.PlayerId, joinMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling player connection for {PlayerId}", e.PlayerId);
        }
    }

    private async void OnPlayerDisconnected(object? sender, PlayerDisconnectedEventArgs e)
    {
        try
        {
            _logger.LogInformation("Player {PlayerId} disconnected", e.PlayerId);
            
            var player = _playerManager.GetPlayer(e.PlayerId);
            var playerName = player?.Name ?? $"Player{e.PlayerId}";
            
            await _playerManager.RemovePlayerAsync(e.PlayerId);
            
            // Remove from any active sessions
            var activeSessions = _sessionManager.GetActiveSessions();
            foreach (var session in activeSessions)
            {
                await _sessionManager.RemovePlayerFromSessionAsync(session.Id, e.PlayerId);
            }
            
            // Broadcast player left to others
            var leaveMessage = $"Player {playerName} left the server";
            await BroadcastToOthersAsync(e.PlayerId, leaveMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling player disconnection for {PlayerId}", e.PlayerId);
        }
    }

    private async void OnMessageReceived(object? sender, MessageReceivedEventArgs e)
    {
        try
        {
            _logger.LogDebug("Received message from player {PlayerId}: {Message}", e.PlayerId, e.Message);
            
            // Handle different message types
            await ProcessPlayerMessage(e.PlayerId, e.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message from player {PlayerId}", e.PlayerId);
        }
    }

    private async Task ProcessPlayerMessage(int playerId, string message)
    {
        // Simple command processing - could be expanded with a proper command system
        var parts = message.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return;

        var command = parts[0].ToLowerInvariant();
        
        switch (command)
        {
            case "ready":
                await _playerManager.SetPlayerReadyAsync(playerId, true);
                await _networkService.SendToPlayerAsync(playerId, "You are now ready!");
                break;
                
            case "unready":
                await _playerManager.SetPlayerReadyAsync(playerId, false);
                await _networkService.SendToPlayerAsync(playerId, "You are no longer ready");
                break;
                
            case "status":
                var status = _serverManager.GetStatus();
                var statusMessage = $"Server: {status.Name}, Players: {status.CurrentPlayers}/{status.MaxPlayers}, Sessions: {status.ActiveSessions.Count}";
                await _networkService.SendToPlayerAsync(playerId, statusMessage);
                break;
                
            case "chat":
                if (parts.Length > 1)
                {
                    var player = _playerManager.GetPlayer(playerId);
                    var chatMessage = string.Join(" ", parts.Skip(1));
                    var broadcastMessage = $"{player?.Name ?? $"Player{playerId}"}: {chatMessage}";
                    await _networkService.BroadcastToAllAsync(broadcastMessage);
                }
                break;
                
            default:
                await _networkService.SendToPlayerAsync(playerId, "Unknown command. Available: ready, unready, status, chat <message>");
                break;
        }
    }

    private async Task BroadcastToOthersAsync(int excludePlayerId, string message)
    {
        var allPlayers = _playerManager.GetAllPlayers();
        var tasks = allPlayers
            .Where(p => p.Id != excludePlayerId)
            .Select(p => _networkService.SendToPlayerAsync(p.Id, message));
        
        await Task.WhenAll(tasks);
    }
}