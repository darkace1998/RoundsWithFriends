using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RWF.DedicatedServer.Core;

namespace RWF.DedicatedServer.Services;

public interface IServerManager
{
    ServerStatus GetStatus();
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
    bool IsRunning { get; }
}

public class ServerManager : IServerManager
{
    private readonly ILogger<ServerManager> _logger;
    private readonly ServerConfiguration _config;
    private readonly INetworkService _networkService;
    private readonly IGameSessionManager _sessionManager;
    private readonly IPlayerManager _playerManager;
    
    private ServerStatus _status;
    private DateTime _startTime;

    public bool IsRunning => _status.State == ServerState.Running;

    public ServerManager(
        ILogger<ServerManager> logger,
        IOptions<ServerConfiguration> config,
        INetworkService networkService,
        IGameSessionManager sessionManager,
        IPlayerManager playerManager)
    {
        _logger = logger;
        _config = config.Value;
        _networkService = networkService;
        _sessionManager = sessionManager;
        _playerManager = playerManager;
        
        _status = new ServerStatus
        {
            Name = _config.Name,
            Version = GetType().Assembly.GetName().Version?.ToString() ?? "Unknown",
            MaxPlayers = _config.MaxPlayers,
            State = ServerState.Starting
        };
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting server '{ServerName}' on port {Port}...", _config.Name, _config.Port);
        
        try
        {
            _startTime = DateTime.UtcNow;
            _status.StartTime = _startTime;
            _status.State = ServerState.Starting;

            // Start network service
            await _networkService.StartAsync(_config.Port, cancellationToken);
            
            // Initialize session manager
            await _sessionManager.InitializeAsync(cancellationToken);
            
            // Initialize player manager
            await _playerManager.InitializeAsync(cancellationToken);

            _status.State = ServerState.Running;
            _logger.LogInformation("Server started successfully on port {Port}", _config.Port);
        }
        catch (Exception ex)
        {
            _status.State = ServerState.Error;
            _logger.LogError(ex, "Failed to start server");
            throw;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping server...");
        
        try
        {
            _status.State = ServerState.Stopping;

            // Stop accepting new connections
            await _networkService.StopAsync(cancellationToken);
            
            // End all active sessions
            await _sessionManager.StopAllSessionsAsync(cancellationToken);
            
            // Disconnect all players
            await _playerManager.DisconnectAllAsync(cancellationToken);

            _status.State = ServerState.Stopped;
            _logger.LogInformation("Server stopped successfully");
        }
        catch (Exception ex)
        {
            _status.State = ServerState.Error;
            _logger.LogError(ex, "Error occurred while stopping server");
            throw;
        }
    }

    public ServerStatus GetStatus()
    {
        _status.Uptime = DateTime.UtcNow - _startTime;
        _status.CurrentPlayers = _playerManager.GetPlayerCount();
        _status.ActiveSessions = _sessionManager.GetActiveSessions();
        
        return _status;
    }
}