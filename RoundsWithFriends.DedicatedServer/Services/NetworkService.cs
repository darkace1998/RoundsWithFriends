using Microsoft.Extensions.Logging;
using RWF.DedicatedServer.Core;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace RWF.DedicatedServer.Services;

public interface INetworkService
{
    Task StartAsync(int port, CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
    Task BroadcastToAllAsync(string message, CancellationToken cancellationToken = default);
    Task SendToPlayerAsync(int playerId, string message, CancellationToken cancellationToken = default);
    event EventHandler<PlayerConnectedEventArgs>? PlayerConnected;
    event EventHandler<PlayerDisconnectedEventArgs>? PlayerDisconnected;
    event EventHandler<MessageReceivedEventArgs>? MessageReceived;
}

public class NetworkService : INetworkService
{
    private readonly ILogger<NetworkService> _logger;
    private readonly ConcurrentDictionary<int, TcpClient> _connectedClients = new();
    private TcpListener? _listener;
    private CancellationTokenSource? _cancellationTokenSource;
    private int _nextPlayerId = 1;

    public event EventHandler<PlayerConnectedEventArgs>? PlayerConnected;
    public event EventHandler<PlayerDisconnectedEventArgs>? PlayerDisconnected;
    public event EventHandler<MessageReceivedEventArgs>? MessageReceived;

    public NetworkService(ILogger<NetworkService> logger)
    {
        _logger = logger;
    }

    public async Task StartAsync(int port, CancellationToken cancellationToken = default)
    {
        _cancellationTokenSource = new CancellationTokenSource();
        var combinedToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancellationTokenSource.Token).Token;

        _listener = new TcpListener(IPAddress.Any, port);
        _listener.Start();

        _logger.LogInformation("Network service listening on port {Port}", port);

        // Start accepting connections in background
        _ = Task.Run(async () => await AcceptConnectionsAsync(combinedToken), combinedToken);
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping network service...");

        _cancellationTokenSource?.Cancel();
        
        // Disconnect all clients
        foreach (var client in _connectedClients.Values)
        {
            try
            {
                client.Close();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error closing client connection");
            }
        }
        
        _connectedClients.Clear();
        _listener?.Stop();
        
        _logger.LogInformation("Network service stopped");
        return Task.CompletedTask;
    }

    private async Task AcceptConnectionsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && _listener != null)
        {
            try
            {
                var tcpClient = await _listener.AcceptTcpClientAsync();
                var playerId = _nextPlayerId++;
                
                _connectedClients[playerId] = tcpClient;
                
                var endpoint = tcpClient.Client.RemoteEndPoint?.ToString() ?? "Unknown";
                _logger.LogInformation("Player {PlayerId} connected from {Endpoint}", playerId, endpoint);
                
                PlayerConnected?.Invoke(this, new PlayerConnectedEventArgs
                {
                    PlayerId = playerId,
                    IpAddress = endpoint
                });

                // Handle this client in background
                _ = Task.Run(async () => await HandleClientAsync(playerId, tcpClient, cancellationToken), cancellationToken);
            }
            catch (ObjectDisposedException)
            {
                // Expected when listener is stopped
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accepting client connection");
            }
        }
    }

    private async Task HandleClientAsync(int playerId, TcpClient client, CancellationToken cancellationToken)
    {
        var stream = client.GetStream();
        var buffer = new byte[4096];

        try
        {
            while (!cancellationToken.IsCancellationRequested && client.Connected)
            {
                var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                
                if (bytesRead == 0)
                {
                    // Client disconnected
                    break;
                }

                var message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                
                MessageReceived?.Invoke(this, new MessageReceivedEventArgs
                {
                    PlayerId = playerId,
                    Message = message
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error handling client {PlayerId}", playerId);
        }
        finally
        {
            _connectedClients.TryRemove(playerId, out _);
            client.Close();
            
            PlayerDisconnected?.Invoke(this, new PlayerDisconnectedEventArgs
            {
                PlayerId = playerId
            });
            
            _logger.LogInformation("Player {PlayerId} disconnected", playerId);
        }
    }

    public async Task BroadcastToAllAsync(string message, CancellationToken cancellationToken = default)
    {
        var tasks = _connectedClients.Keys.Select(playerId => SendToPlayerAsync(playerId, message, cancellationToken));
        await Task.WhenAll(tasks);
    }

    public async Task SendToPlayerAsync(int playerId, string message, CancellationToken cancellationToken = default)
    {
        if (!_connectedClients.TryGetValue(playerId, out var client) || !client.Connected)
        {
            return;
        }

        try
        {
            var data = Encoding.UTF8.GetBytes(message);
            await client.GetStream().WriteAsync(data, 0, data.Length, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send message to player {PlayerId}", playerId);
        }
    }
}

// Event argument classes
public class PlayerConnectedEventArgs : EventArgs
{
    public int PlayerId { get; set; }
    public string IpAddress { get; set; } = string.Empty;
}

public class PlayerDisconnectedEventArgs : EventArgs
{
    public int PlayerId { get; set; }
}

public class MessageReceivedEventArgs : EventArgs
{
    public int PlayerId { get; set; }
    public string Message { get; set; } = string.Empty;
}