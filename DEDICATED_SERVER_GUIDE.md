# Dedicated Server Implementation Guide

This document provides a comprehensive guide for implementing and using the dedicated server functionality in RoundsWithFriends.

## Overview

The dedicated server implementation adds the ability to host persistent game servers for RoundsWithFriends without requiring a game client. This allows for:

- **24/7 Server Hosting**: Run servers continuously without player intervention
- **Better Performance**: Dedicated resources for game hosting
- **Centralized Management**: Admin tools and monitoring capabilities
- **Scalability**: Support for multiple concurrent game sessions

## Architecture

### Components

1. **RoundsWithFriends.DedicatedServer**: Standalone server application
2. **Client Integration**: Extensions to the existing mod for server connectivity
3. **Network Protocol**: Custom TCP/UDP protocol for client-server communication
4. **Administration Tools**: Web-based and console administration interfaces

### Design Principles

- **Minimal Changes**: Preserve existing Photon-based functionality
- **Backward Compatibility**: Existing mod functionality remains unchanged
- **Flexibility**: Support both dedicated servers and P2P hosting
- **Performance**: Optimized for high player counts and low latency

## Getting Started

### For Server Administrators

#### System Requirements

- **Operating System**: Windows 10+, Linux (Ubuntu 20.04+), or macOS 11+
- **Runtime**: .NET 8.0 Runtime
- **Memory**: 4GB RAM minimum, 8GB recommended
- **CPU**: 2 cores minimum, 4 cores recommended
- **Network**: Stable internet connection with adequate upload bandwidth
- **Storage**: 1GB free space

#### Installation

1. **Download the Server Package**
   ```bash
   # From GitHub Releases
   wget https://github.com/olavim/RoundsWithFriends/releases/latest/download/rwf-server.zip
   unzip rwf-server.zip
   cd rwf-server
   ```

2. **Configure the Server**
   ```bash
   # Edit configuration file
   nano appsettings.json
   ```

   Key settings:
   ```json
   {
     "Server": {
       "Name": "Your Server Name",
       "Port": 7777,
       "MaxPlayers": 16,
       "EnableWebAdmin": true
     }
   }
   ```

3. **Start the Server**
   ```bash
   # Linux/macOS
   dotnet RoundsWithFriends.DedicatedServer.dll

   # Windows
   RoundsWithFriends.DedicatedServer.exe
   ```

#### Docker Deployment

For production deployments, Docker is recommended:

```bash
# Clone the repository
git clone https://github.com/olavim/RoundsWithFriends.git
cd RoundsWithFriends

# Build and run with Docker Compose
docker-compose -f RoundsWithFriends.DedicatedServer/docker-compose.yml up -d
```

### For Players

#### Connecting to a Dedicated Server

1. **Install RoundsWithFriends Mod** (as usual)
2. **Launch ROUNDS** with the mod enabled
3. **Access Server Browser**:
   - From main menu, select "Browse Servers"
   - Or use "Connect to Server" for direct IP connection

4. **Join a Server**:
   - Select server from browser list
   - Or enter IP address and port manually
   - Click "Connect"

#### Server Browser Features

- **Server List**: View available servers with player counts
- **Favorites**: Save frequently used servers
- **Quick Connect**: Connect to recently used servers
- **Server Info**: View detailed server information and rules

## Configuration

### Server Configuration

#### Basic Settings

```json
{
  "Server": {
    "Name": "My RWF Server",
    "Description": "A friendly server for all players",
    "Port": 7777,
    "MaxPlayers": 16,
    "MaxTeams": 16,
    "TickRate": 60
  }
}
```

#### Game Mode Configuration

```json
{
  "GameModes": {
    "Available": [
      "TeamDeathmatch",
      "Deathmatch"
    ],
    "Default": "TeamDeathmatch",
    "Settings": {
      "TeamDeathmatch": {
        "roundsToWinGame": 5,
        "pointsToWinRound": 2
      }
    }
  }
}
```

#### Security Settings

```json
{
  "Security": {
    "RequireAuthentication": false,
    "AdminPassword": "secure-admin-password",
    "BanList": [
      "192.168.1.100",
      "badplayer123"
    ],
    "Whitelist": [],
    "RateLimiting": {
      "ConnectionsPerIP": 3,
      "TimeWindow": 60
    }
  }
}
```

### Client Configuration

Players can configure their dedicated server preferences:

```json
{
  "DedicatedServer": {
    "PreferredServers": [
      {
        "Name": "My Favorite Server",
        "Address": "game.example.com",
        "Port": 7777
      }
    ],
    "AutoConnect": false,
    "ConnectionTimeout": 30000
  }
}
```

## Administration

### Web Administration Interface

Access the web admin at `http://your-server:8080` (if enabled).

#### Features:
- **Dashboard**: Server overview, player count, uptime
- **Player Management**: View, kick, ban players
- **Game Sessions**: Monitor active games, end sessions
- **Server Settings**: Live configuration updates
- **Logs**: Real-time log viewing and filtering
- **Statistics**: Performance metrics and graphs

#### Screenshots:
- [Dashboard](screenshots/admin-dashboard.png)
- [Player Management](screenshots/admin-players.png)
- [Server Logs](screenshots/admin-logs.png)

### Console Commands

While the server is running, use these commands:

#### Server Management
- `status` - Show server status
- `stop` - Gracefully stop the server
- `restart` - Restart the server
- `reload` - Reload configuration

#### Player Management
- `players` - List connected players
- `kick <playerId>` - Remove a player
- `ban <playerId> [reason]` - Ban a player
- `unban <playerId>` - Remove a ban

#### Game Management
- `sessions` - List active game sessions
- `end <sessionId>` - End a game session
- `gamemode <mode>` - Change default game mode

### Remote Administration

#### REST API

The server exposes a REST API for programmatic administration:

```bash
# Get server status
curl http://localhost:8080/api/status

# Get player list
curl http://localhost:8080/api/players

# Kick a player
curl -X POST http://localhost:8080/api/players/123/kick \
  -H "Authorization: Bearer <admin-token>"
```

#### Command Line Tools

```bash
# Remote server management
rwf-admin --server game.example.com:8080 --command "status"
rwf-admin --server game.example.com:8080 --command "players"
```

## Network Protocol

### Connection Flow

1. **TCP Handshake**: Client establishes TCP connection
2. **Authentication**: Optional player authentication
3. **Lobby Join**: Player enters server lobby
4. **Game Session**: Join or create game sessions
5. **Heartbeat**: Periodic keepalive messages

### Message Format

```json
{
  "type": "message_type",
  "timestamp": 1234567890,
  "data": {
    "key": "value"
  }
}
```

### Message Types

#### Client to Server:
- `join_lobby` - Enter server lobby
- `ready` - Signal ready for game
- `chat_message` - Send chat message
- `input` - Game input data

#### Server to Client:
- `lobby_update` - Lobby state changes
- `game_start` - Game session starting
- `game_state` - Game state updates
- `chat_broadcast` - Chat messages

## Performance Optimization

### Server Performance

#### CPU Optimization
- Use `Release` build configuration
- Enable CPU affinity for dedicated cores
- Adjust tick rate based on player count

#### Memory Optimization
- Monitor memory usage with built-in metrics
- Configure garbage collection for server workloads
- Use memory-mapped files for large data

#### Network Optimization
- Use dedicated network hardware
- Configure Quality of Service (QoS) rules
- Monitor bandwidth usage per player

### Client Performance

#### Connection Quality
- Prefer wired over wireless connections
- Use low-latency internet connections
- Enable connection compression if available

#### Game Settings
- Adjust visual settings for better performance
- Use appropriate resolution and frame rate
- Disable unnecessary visual effects

## Troubleshooting

### Common Server Issues

#### Server Won't Start
**Problem**: Server fails to start with port errors
**Solution**: 
1. Check if port is already in use: `netstat -an | grep 7777`
2. Change port in configuration
3. Check firewall settings

#### Poor Performance
**Problem**: High latency or low frame rates
**Solution**:
1. Check CPU and memory usage
2. Reduce max players or tick rate
3. Verify network bandwidth
4. Update server hardware

#### Connection Issues
**Problem**: Players can't connect
**Solution**:
1. Verify firewall allows incoming connections
2. Check router port forwarding (for home hosting)
3. Confirm server IP address is correct
4. Test with local connections first

### Common Client Issues

#### Can't Find Servers
**Problem**: Server browser shows no servers
**Solution**:
1. Check internet connection
2. Verify server discovery settings
3. Try direct IP connection
4. Check firewall blocking outbound connections

#### Connection Timeouts
**Problem**: Frequent disconnections
**Solution**:
1. Check network stability
2. Increase connection timeout setting
3. Verify server is running and reachable
4. Try different server if available

### Debugging

#### Enable Debug Logging

Server:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug"
    }
  }
}
```

Client:
```csharp
// In mod configuration
RWFMod.SetLogLevel(LogLevel.Debug);
```

#### Log Analysis

Key log patterns to look for:
- Connection establishment: `Player X connected from Y`
- Game state changes: `Session X state changed to Y`
- Errors: `ERROR` or `Exception` entries
- Performance: High CPU/memory usage warnings

## Security Considerations

### Server Security

#### Network Security
- Use firewall to restrict access to admin ports
- Consider VPN for administrative access
- Enable DDoS protection if available
- Monitor for suspicious connection patterns

#### Authentication
- Enable player authentication for competitive servers
- Use strong admin passwords
- Implement IP-based access controls
- Regular security updates

#### Data Protection
- Limit data collection to necessary information
- Secure storage of player data
- Regular backups of server configuration
- Compliance with privacy regulations

### Client Security

#### Connection Security
- Verify server certificates (if implemented)
- Avoid connecting to unknown servers
- Be cautious with personal information
- Report suspicious server behavior

## Development

### Building from Source

#### Prerequisites
- .NET 8.0 SDK
- Git
- Visual Studio or VS Code (optional)

#### Build Steps
```bash
# Clone repository
git clone https://github.com/olavim/RoundsWithFriends.git
cd RoundsWithFriends

# Restore dependencies
dotnet restore

# Build dedicated server
dotnet build RoundsWithFriends.DedicatedServer -c Release

# Run tests
dotnet test

# Create release package
dotnet publish RoundsWithFriends.DedicatedServer -c Release -o publish/
```

### Custom Modifications

#### Adding Game Modes
1. Implement `IGameModeHandler` interface
2. Register with `GameModeManager`
3. Update server configuration schema
4. Test with multiple clients

#### Custom Network Protocols
1. Implement `INetworkService` interface
2. Handle connection lifecycle events
3. Define message serialization format
4. Update client integration code

#### Administration Extensions
1. Create custom admin controllers
2. Implement REST API endpoints
3. Add web UI components
4. Register with dependency injection

### Testing

#### Unit Tests
```bash
# Run all tests
dotnet test

# Run specific test category
dotnet test --filter Category=Integration
```

#### Integration Tests
```bash
# Start test server
dotnet run --project RoundsWithFriends.DedicatedServer -- --environment Testing

# Run client tests against server
dotnet test RoundsWithFriends.Tests --filter Integration
```

#### Load Testing
```bash
# Simulate multiple clients
./scripts/load-test.sh --server localhost:7777 --clients 16 --duration 60s
```

## Community and Support

### Getting Help

- **Documentation**: [GitHub Wiki](https://github.com/olavim/RoundsWithFriends/wiki)
- **Issues**: [GitHub Issues](https://github.com/olavim/RoundsWithFriends/issues)
- **Discord**: Community Discord server
- **Forums**: Steam Community discussions

### Contributing

#### Bug Reports
1. Check existing issues first
2. Provide detailed reproduction steps
3. Include log files and system information
4. Use appropriate issue templates

#### Feature Requests
1. Discuss in community channels first
2. Create detailed specification
3. Consider implementation complexity
4. Submit formal feature request

#### Code Contributions
1. Fork the repository
2. Create feature branch
3. Follow coding standards
4. Add tests for new features
5. Submit pull request

### Roadmap

#### Version 3.0 (Current)
- [x] Basic dedicated server implementation
- [x] Web administration interface
- [x] Docker deployment support
- [ ] Advanced game mode support
- [ ] Enhanced security features

#### Version 3.1 (Planned)
- [ ] Server clusters and load balancing
- [ ] Advanced statistics and analytics
- [ ] Plugin system for custom modifications
- [ ] Improved client UI and UX

#### Future Versions
- [ ] Cloud hosting integration
- [ ] Mobile administration app
- [ ] Advanced anti-cheat systems
- [ ] Professional hosting tools

## License and Legal

This dedicated server implementation is provided under the same license as the main RoundsWithFriends project. Please review the LICENSE file for full terms and conditions.

### Third-Party Components
- .NET 8.0 Runtime (Microsoft)
- Various NuGet packages (see project files for details)

### Disclaimer
This software is provided "as is" without warranty of any kind. Use at your own risk. The developers are not responsible for any damages or issues resulting from the use of this software.

---

For more information, visit the [official repository](https://github.com/olavim/RoundsWithFriends) or join our community Discord server.