using Microsoft.Extensions.Logging;
using RWF.DedicatedServer.Core;
using System.Collections.Concurrent;

namespace RWF.DedicatedServer.Services;

public interface IGameSessionManager
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task<GameSession> CreateSessionAsync(string gameMode, List<int> playerIds);
    Task<bool> StartSessionAsync(string sessionId);
    Task EndSessionAsync(string sessionId);
    Task StopAllSessionsAsync(CancellationToken cancellationToken = default);
    GameSession? GetSession(string sessionId);
    List<GameSession> GetActiveSessions();
    Task AddPlayerToSessionAsync(string sessionId, int playerId);
    Task RemovePlayerFromSessionAsync(string sessionId, int playerId);
}

public class GameSessionManager : IGameSessionManager
{
    private readonly ILogger<GameSessionManager> _logger;
    private readonly ConcurrentDictionary<string, GameSession> _sessions = new();

    public GameSessionManager(ILogger<GameSessionManager> logger)
    {
        _logger = logger;
    }

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Game session manager initialized");
        return Task.CompletedTask;
    }

    public Task<GameSession> CreateSessionAsync(string gameMode, List<int> playerIds)
    {
        var session = new GameSession
        {
            Id = Guid.NewGuid().ToString(),
            GameMode = gameMode,
            State = GameSessionState.Waiting,
            CreatedAt = DateTime.UtcNow
        };

        // Add placeholder players (will be populated by PlayerManager)
        foreach (var playerId in playerIds)
        {
            session.Players.Add(new ServerPlayer { Id = playerId });
        }

        _sessions[session.Id] = session;
        
        _logger.LogInformation("Created game session {SessionId} for game mode {GameMode} with {PlayerCount} players",
            session.Id, gameMode, playerIds.Count);
        
        return Task.FromResult(session);
    }

    public Task<bool> StartSessionAsync(string sessionId)
    {
        if (!_sessions.TryGetValue(sessionId, out var session))
        {
            _logger.LogWarning("Attempted to start non-existent session {SessionId}", sessionId);
            return Task.FromResult(false);
        }

        if (session.State != GameSessionState.Waiting)
        {
            _logger.LogWarning("Attempted to start session {SessionId} in state {State}", sessionId, session.State);
            return Task.FromResult(false);
        }

        session.State = GameSessionState.Starting;
        session.StartedAt = DateTime.UtcNow;
        
        _logger.LogInformation("Starting game session {SessionId}", sessionId);
        
        // Transition to in progress after a brief delay
        _ = Task.Run(async () =>
        {
            await Task.Delay(1000); // Brief delay to simulate startup
            if (_sessions.TryGetValue(sessionId, out var currentSession))
            {
                currentSession.State = GameSessionState.InProgress;
                _logger.LogInformation("Game session {SessionId} is now in progress", sessionId);
            }
        });

        return Task.FromResult(true);
    }

    public Task EndSessionAsync(string sessionId)
    {
        if (!_sessions.TryGetValue(sessionId, out var session))
        {
            _logger.LogWarning("Attempted to end non-existent session {SessionId}", sessionId);
            return Task.CompletedTask;
        }

        session.State = GameSessionState.Finished;
        session.EndedAt = DateTime.UtcNow;
        
        _logger.LogInformation("Ended game session {SessionId}", sessionId);
        
        // Remove session after a delay to allow for cleanup
        _ = Task.Run(async () =>
        {
            await Task.Delay(30000); // Keep session for 30 seconds
            _sessions.TryRemove(sessionId, out _);
            _logger.LogDebug("Removed finished session {SessionId}", sessionId);
        });

        return Task.CompletedTask;
    }

    public async Task StopAllSessionsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping all active sessions...");
        
        var activeSessions = _sessions.Values.Where(s => s.State == GameSessionState.InProgress || s.State == GameSessionState.Starting).ToList();
        
        foreach (var session in activeSessions)
        {
            await EndSessionAsync(session.Id);
        }
        
        _logger.LogInformation("Stopped {SessionCount} active sessions", activeSessions.Count);
    }

    public GameSession? GetSession(string sessionId)
    {
        _sessions.TryGetValue(sessionId, out var session);
        return session;
    }

    public List<GameSession> GetActiveSessions()
    {
        return _sessions.Values
            .Where(s => s.State == GameSessionState.Waiting || 
                       s.State == GameSessionState.Starting || 
                       s.State == GameSessionState.InProgress)
            .ToList();
    }

    public Task AddPlayerToSessionAsync(string sessionId, int playerId)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            if (!session.Players.Any(p => p.Id == playerId))
            {
                session.Players.Add(new ServerPlayer { Id = playerId });
                _logger.LogInformation("Added player {PlayerId} to session {SessionId}", playerId, sessionId);
            }
        }
        
        return Task.CompletedTask;
    }

    public Task RemovePlayerFromSessionAsync(string sessionId, int playerId)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            var player = session.Players.FirstOrDefault(p => p.Id == playerId);
            if (player != null)
            {
                session.Players.Remove(player);
                _logger.LogInformation("Removed player {PlayerId} from session {SessionId}", playerId, sessionId);
                
                // End session if no players remain
                if (session.Players.Count == 0)
                {
                    _ = EndSessionAsync(sessionId);
                }
            }
        }
        
        return Task.CompletedTask;
    }
}