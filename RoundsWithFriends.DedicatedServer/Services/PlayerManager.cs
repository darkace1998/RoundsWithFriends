using Microsoft.Extensions.Logging;
using RWF.DedicatedServer.Core;
using System.Collections.Concurrent;

namespace RWF.DedicatedServer.Services;

public interface IPlayerManager
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task<ServerPlayer> AddPlayerAsync(int id, string ipAddress);
    Task RemovePlayerAsync(int id);
    Task DisconnectAllAsync(CancellationToken cancellationToken = default);
    ServerPlayer? GetPlayer(int id);
    List<ServerPlayer> GetAllPlayers();
    int GetPlayerCount();
    Task UpdatePlayerStateAsync(int id, PlayerState state);
    Task SetPlayerReadyAsync(int id, bool ready);
    Task SetPlayerTeamAsync(int id, int teamId, int colorId);
}

public class PlayerManager : IPlayerManager
{
    private readonly ILogger<PlayerManager> _logger;
    private readonly ConcurrentDictionary<int, ServerPlayer> _players = new();

    public PlayerManager(ILogger<PlayerManager> logger)
    {
        _logger = logger;
    }

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Player manager initialized");
        return Task.CompletedTask;
    }

    public Task<ServerPlayer> AddPlayerAsync(int id, string ipAddress)
    {
        var player = new ServerPlayer
        {
            Id = id,
            Name = $"Player{id}",
            IpAddress = ipAddress,
            ConnectedAt = DateTime.UtcNow,
            State = PlayerState.Connected
        };

        _players[id] = player;
        _logger.LogInformation("Added player {PlayerId} ({IpAddress})", id, ipAddress);
        
        return Task.FromResult(player);
    }

    public Task RemovePlayerAsync(int id)
    {
        if (_players.TryRemove(id, out var player))
        {
            _logger.LogInformation("Removed player {PlayerId} ({PlayerName})", id, player.Name);
        }
        
        return Task.CompletedTask;
    }

    public async Task DisconnectAllAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Disconnecting all players...");
        
        var playerIds = _players.Keys.ToList();
        
        foreach (var id in playerIds)
        {
            await RemovePlayerAsync(id);
        }
        
        _logger.LogInformation("All players disconnected");
    }

    public ServerPlayer? GetPlayer(int id)
    {
        _players.TryGetValue(id, out var player);
        return player;
    }

    public List<ServerPlayer> GetAllPlayers()
    {
        return _players.Values.ToList();
    }

    public int GetPlayerCount()
    {
        return _players.Count;
    }

    public Task UpdatePlayerStateAsync(int id, PlayerState state)
    {
        if (_players.TryGetValue(id, out var player))
        {
            player.State = state;
            _logger.LogDebug("Player {PlayerId} state changed to {State}", id, state);
        }
        
        return Task.CompletedTask;
    }

    public Task SetPlayerReadyAsync(int id, bool ready)
    {
        if (_players.TryGetValue(id, out var player))
        {
            player.IsReady = ready;
            _logger.LogInformation("Player {PlayerId} ({PlayerName}) ready state: {IsReady}", 
                id, player.Name, ready);
        }
        
        return Task.CompletedTask;
    }

    public Task SetPlayerTeamAsync(int id, int teamId, int colorId)
    {
        if (_players.TryGetValue(id, out var player))
        {
            player.TeamId = teamId;
            player.ColorId = colorId;
            _logger.LogInformation("Player {PlayerId} ({PlayerName}) assigned to team {TeamId}, color {ColorId}", 
                id, player.Name, teamId, colorId);
        }
        
        return Task.CompletedTask;
    }
}