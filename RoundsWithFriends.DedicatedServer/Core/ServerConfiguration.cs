namespace RWF.DedicatedServer.Core;

public class ServerConfiguration
{
    public string Name { get; set; } = "RoundsWithFriends Server";
    public string Description { get; set; } = "Dedicated server for RoundsWithFriends";
    public int Port { get; set; } = 7777;
    public int MaxPlayers { get; set; } = 16;
    public int MaxTeams { get; set; } = 16;
    public int TickRate { get; set; } = 60;
    public bool EnableConsole { get; set; } = true;
    public bool EnableWebAdmin { get; set; } = false;
    public int WebAdminPort { get; set; } = 8080;
    public bool EnableDebugEndpoints { get; set; } = false;
    public bool EnableDetailedLogging { get; set; } = false;
    public bool LogPlayerConnections { get; set; } = true;
    public bool LogNetworkMessages { get; set; } = false;
}

public class DebugConfiguration
{
    public bool EnableDebugConsole { get; set; } = false;
    public bool EnableHealthMonitoring { get; set; } = false;
    public int HealthCheckInterval { get; set; } = 30;
    public bool EnablePerformanceMetrics { get; set; } = false;
    public bool EnableStackTraces { get; set; } = false;
    public bool LogMemoryUsage { get; set; } = false;
}

public class GameConfiguration
{
    public List<string> Available { get; set; } = new();
    public string Default { get; set; } = "TeamDeathmatch";
}

public class SecurityConfiguration
{
    public bool RequireAuthentication { get; set; } = false;
    public string AdminPassword { get; set; } = string.Empty;
    public List<string> BanList { get; set; } = new();
    public List<string> Whitelist { get; set; } = new();
}

public class PerformanceConfiguration
{
    public int MaxConcurrentGames { get; set; } = 1;
    public int HeartbeatInterval { get; set; } = 30000;
    public int ConnectionTimeout { get; set; } = 60000;
}