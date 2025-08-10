namespace RWF.DedicatedServer.Core;

/// <summary>
/// Represents a connected player on the dedicated server
/// </summary>
public class ServerPlayer
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public DateTime ConnectedAt { get; set; }
    public bool IsAuthenticated { get; set; }
    public bool IsReady { get; set; }
    public int TeamId { get; set; }
    public int ColorId { get; set; }
    public PlayerState State { get; set; } = PlayerState.Connected;
}

public enum PlayerState
{
    Connected,
    InLobby,
    InGame,
    Spectating,
    Disconnected
}

/// <summary>
/// Represents a game session on the server
/// </summary>
public class GameSession
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string GameMode { get; set; } = string.Empty;
    public List<ServerPlayer> Players { get; set; } = new();
    public GameSessionState State { get; set; } = GameSessionState.Waiting;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public Dictionary<string, object> Settings { get; set; } = new();
}

public enum GameSessionState
{
    Waiting,
    Starting,
    InProgress,
    Paused,
    Finished
}

/// <summary>
/// Server statistics and status information
/// </summary>
public class ServerStatus
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public int CurrentPlayers { get; set; }
    public int MaxPlayers { get; set; }
    public List<GameSession> ActiveSessions { get; set; } = new();
    public TimeSpan Uptime { get; set; }
    public DateTime StartTime { get; set; }
    public ServerState State { get; set; } = ServerState.Starting;
}

public enum ServerState
{
    Starting,
    Running,
    Stopping,
    Stopped,
    Error
}