using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RWF.DedicatedServer.Core;
using System.Diagnostics;

namespace RWF.DedicatedServer.Services;

public interface IHealthMonitorService
{
    HealthStatus GetHealthStatus();
    PerformanceMetrics GetPerformanceMetrics();
}

public class HealthStatus
{
    public DateTime CheckTime { get; set; }
    public bool IsHealthy { get; set; }
    public string Status { get; set; } = "Unknown";
    public Dictionary<string, object> Metrics { get; set; } = new();
    public List<string> Issues { get; set; } = new();
}

public class PerformanceMetrics
{
    public double CpuUsagePercent { get; set; }
    public long MemoryUsageMB { get; set; }
    public long GCMemoryMB { get; set; }
    public int ThreadCount { get; set; }
    public TimeSpan Uptime { get; set; }
    public int ConnectedPlayers { get; set; }
    public int ActiveSessions { get; set; }
    public Dictionary<string, long> CollectionCounts { get; set; } = new();
}

public class HealthMonitorService : BackgroundService, IHealthMonitorService
{
    private readonly ILogger<HealthMonitorService> _logger;
    private readonly DebugConfiguration _debugConfig;
    private readonly IServerManager _serverManager;
    private readonly IPlayerManager _playerManager;
    private readonly IGameSessionManager _sessionManager;
    private readonly Stopwatch _uptime;
    
    private HealthStatus _lastHealthStatus;
    private PerformanceMetrics _lastMetrics;

    public HealthMonitorService(
        ILogger<HealthMonitorService> logger,
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
        
        _lastHealthStatus = new HealthStatus { Status = "Starting" };
        _lastMetrics = new PerformanceMetrics();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_debugConfig.EnableHealthMonitoring)
        {
            _logger.LogInformation("Health monitoring is disabled");
            return;
        }

        _logger.LogInformation("Health monitoring started with {Interval}s interval", _debugConfig.HealthCheckInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PerformHealthCheck();
                
                if (_debugConfig.LogMemoryUsage)
                {
                    LogMemoryUsage();
                }

                await Task.Delay(TimeSpan.FromSeconds(_debugConfig.HealthCheckInterval), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during health check");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }

    private async Task PerformHealthCheck()
    {
        var healthStatus = new HealthStatus
        {
            CheckTime = DateTime.UtcNow,
            IsHealthy = true,
            Status = "Healthy"
        };

        var metrics = new PerformanceMetrics();
        var issues = new List<string>();

        try
        {
            // Collect performance metrics
            var process = Process.GetCurrentProcess();
            
            metrics.MemoryUsageMB = process.WorkingSet64 / (1024 * 1024);
            metrics.GCMemoryMB = GC.GetTotalMemory(false) / (1024 * 1024);
            metrics.ThreadCount = process.Threads.Count;
            metrics.Uptime = _uptime.Elapsed;
            
            // CPU usage - simplified for cross-platform compatibility
            metrics.CpuUsagePercent = 0; // Could be enhanced with platform-specific implementations

            // Server-specific metrics
            var serverStatus = _serverManager.GetStatus();
            metrics.ConnectedPlayers = serverStatus.CurrentPlayers;
            metrics.ActiveSessions = serverStatus.ActiveSessions.Count;

            // GC collection counts
            for (int i = 0; i <= GC.MaxGeneration; i++)
            {
                metrics.CollectionCounts[$"Gen{i}"] = GC.CollectionCount(i);
            }

            // Health checks
            CheckMemoryUsage(metrics, issues);
            CheckPlayerCount(metrics, issues);
            CheckServerState(serverStatus, issues);

            // Set overall health status
            healthStatus.IsHealthy = issues.Count == 0;
            healthStatus.Status = healthStatus.IsHealthy ? "Healthy" : "Warning";
            healthStatus.Issues = issues;
            
            // Add metrics to health status
            healthStatus.Metrics = new Dictionary<string, object>
            {
                ["MemoryMB"] = metrics.MemoryUsageMB,
                ["GCMemoryMB"] = metrics.GCMemoryMB,
                ["CpuPercent"] = metrics.CpuUsagePercent,
                ["ThreadCount"] = metrics.ThreadCount,
                ["UptimeHours"] = metrics.Uptime.TotalHours,
                ["ConnectedPlayers"] = metrics.ConnectedPlayers,
                ["ActiveSessions"] = metrics.ActiveSessions
            };

            _lastHealthStatus = healthStatus;
            _lastMetrics = metrics;

            // Log health status if there are issues or if performance metrics are enabled
            if (!healthStatus.IsHealthy || _debugConfig.EnablePerformanceMetrics)
            {
                _logger.LogInformation("Health Check: {Status} - Memory: {MemoryMB}MB, CPU: {CpuPercent:F1}%, Players: {Players}, Issues: {IssueCount}",
                    healthStatus.Status, metrics.MemoryUsageMB, metrics.CpuUsagePercent, metrics.ConnectedPlayers, issues.Count);

                if (issues.Any())
                {
                    foreach (var issue in issues)
                    {
                        _logger.LogWarning("Health Issue: {Issue}", issue);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting health metrics");
            healthStatus.IsHealthy = false;
            healthStatus.Status = "Error";
            healthStatus.Issues.Add($"Health check error: {ex.Message}");
            _lastHealthStatus = healthStatus;
        }

        await Task.CompletedTask;
    }

    private void CheckMemoryUsage(PerformanceMetrics metrics, List<string> issues)
    {
        // Warning if memory usage is high
        if (metrics.MemoryUsageMB > 500)
        {
            issues.Add($"High memory usage: {metrics.MemoryUsageMB}MB");
        }

        // Warning if GC memory is growing significantly
        if (metrics.GCMemoryMB > 200)
        {
            issues.Add($"High GC memory: {metrics.GCMemoryMB}MB");
        }

        // Check if Gen 2 collections are happening frequently
        if (metrics.CollectionCounts.TryGetValue("Gen2", out var gen2Count) && gen2Count > 10)
        {
            if (_lastMetrics.CollectionCounts.TryGetValue("Gen2", out var lastGen2) && 
                gen2Count - lastGen2 > 5)
            {
                issues.Add("Frequent Gen 2 garbage collections detected");
            }
        }
    }

    private void CheckPlayerCount(PerformanceMetrics metrics, List<string> issues)
    {
        var serverStatus = _serverManager.GetStatus();
        
        // Warning if server is at capacity
        if (metrics.ConnectedPlayers >= serverStatus.MaxPlayers)
        {
            issues.Add("Server at maximum player capacity");
        }

        // Warning if thread count is growing excessively
        if (metrics.ThreadCount > 50)
        {
            issues.Add($"High thread count: {metrics.ThreadCount}");
        }
    }

    private void CheckServerState(ServerStatus serverStatus, List<string> issues)
    {
        if (serverStatus.State != ServerState.Running)
        {
            issues.Add($"Server not in running state: {serverStatus.State}");
        }
    }

    private void LogMemoryUsage()
    {
        var process = Process.GetCurrentProcess();
        var gcMemory = GC.GetTotalMemory(false);
        
        _logger.LogDebug("Memory Usage: Working Set: {WorkingSetMB}MB, GC: {GCMemoryMB}MB, Threads: {ThreadCount}",
            process.WorkingSet64 / (1024 * 1024),
            gcMemory / (1024 * 1024),
            process.Threads.Count);
    }

    public HealthStatus GetHealthStatus()
    {
        return _lastHealthStatus;
    }

    public PerformanceMetrics GetPerformanceMetrics()
    {
        return _lastMetrics;
    }
}