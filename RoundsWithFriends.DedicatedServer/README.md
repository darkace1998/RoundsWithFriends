# RoundsWithFriends Dedicated Server

A dedicated server implementation for the RoundsWithFriends mod, allowing you to host persistent game servers for the ROUNDS game.

## Features

- **Dedicated Server Hosting**: Run persistent game servers without requiring a client
- **Multi-Game Mode Support**: Supports Team Deathmatch, Deathmatch/Free-for-all, and future game modes
- **Player Management**: Comprehensive player connection, authentication, and session management
- **Server Administration**: Built-in admin tools and remote management capabilities
- **Performance Monitoring**: Real-time server statistics and health monitoring
- **Scalable Architecture**: Designed to handle multiple concurrent game sessions

## Quick Start

### Prerequisites

- .NET 8.0 Runtime or SDK
- Network access for client connections

### Installation

1. **Download Release**
   ```bash
   # Download the latest release from GitHub Releases
   # Extract to your desired directory
   ```

2. **Configure Server**
   ```bash
   # Edit appsettings.json to configure your server
   nano appsettings.json
   ```

3. **Start Server**
   ```bash
   dotnet RoundsWithFriends.DedicatedServer.dll
   ```

### Docker Deployment

```bash
# Build Docker image
docker build -t rwf-server .

# Run container
docker run -d -p 7777:7777 -p 8080:8080 \
  --name rwf-server \
  -v ./config:/app/config \
  rwf-server
```

## Configuration

### Server Settings (appsettings.json)

```json
{
  "Server": {
    "Name": "My RWF Server",
    "Description": "Custom RoundsWithFriends Server",
    "Port": 7777,
    "MaxPlayers": 16,
    "MaxTeams": 16,
    "TickRate": 60,
    "EnableConsole": true,
    "EnableWebAdmin": true,
    "WebAdminPort": 8080
  },
  "GameModes": {
    "Available": ["TeamDeathmatch", "Deathmatch"],
    "Default": "TeamDeathmatch"
  },
  "Security": {
    "RequireAuthentication": false,
    "AdminPassword": "your-admin-password",
    "BanList": [],
    "Whitelist": []
  }
}
```

### Command Line Options

```bash
# Specify custom port
dotnet RoundsWithFriends.DedicatedServer.dll --Server:Port=8888

# Enable debug logging
dotnet RoundsWithFriends.DedicatedServer.dll --Logging:LogLevel:Default=Debug

# Use custom config file
dotnet RoundsWithFriends.DedicatedServer.dll --configuration Production
```

## Administration

### Console Commands

While the server is running, you can use these console commands:

- `status` - Display server status and player count
- `players` - List connected players
- `kick <playerId>` - Kick a player
- `ban <playerId>` - Ban a player
- `stop` - Gracefully stop the server

### Web Administration

If enabled, access the web admin interface at `http://localhost:8080`

Features:
- Real-time server monitoring
- Player management
- Game session control
- Configuration updates
- Log viewing

## Client Connection

### For Players

Players can connect to your dedicated server by:

1. Starting RoundsWithFriends mod
2. Selecting "Connect to Server" from the main menu
3. Entering your server's IP address and port
4. Joining available game sessions

### Server Discovery

The server supports automatic discovery protocols:
- LAN broadcast for local network discovery
- Master server registration for public server listing
- Direct IP connection for private servers

## Performance Tuning

### Recommended System Requirements

- **Minimum**: 2 CPU cores, 4GB RAM, 1Mbps upload per 4 players
- **Recommended**: 4 CPU cores, 8GB RAM, 2Mbps upload per 8 players
- **High Load**: 8+ CPU cores, 16GB+ RAM, 5Mbps+ upload for 16 players

### Optimization Tips

1. **Network**: Use wired connections for better latency
2. **CPU**: Higher clock speeds benefit game simulation
3. **Memory**: More RAM allows for larger player counts
4. **Storage**: SSD recommended for faster loading

## Troubleshooting

### Common Issues

**Server won't start**
- Check if port is already in use
- Verify firewall settings
- Ensure .NET 8.0 is installed

**Players can't connect**
- Verify firewall allows incoming connections on server port
- Check router port forwarding for internet hosting
- Confirm server IP address is correct

**Poor performance**
- Reduce max players or tick rate
- Check CPU and memory usage
- Verify network bandwidth

### Log Files

Server logs are written to:
- Console output (if enabled)
- `logs/` directory (structured logging)
- Windows Event Log (on Windows)

## Development

### Building from Source

```bash
# Clone repository
git clone https://github.com/olavim/RoundsWithFriends.git
cd RoundsWithFriends

# Build dedicated server
dotnet build RoundsWithFriends.DedicatedServer

# Run in development mode
cd RoundsWithFriends.DedicatedServer
dotnet run --environment Development
```

### Project Structure

```
RoundsWithFriends.DedicatedServer/
├── Core/                    # Core server components
├── Services/                # Server services and managers
├── Program.cs              # Entry point
├── appsettings.json        # Configuration
└── README.md              # This file
```

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## License

This project is licensed under the same license as the parent RoundsWithFriends project.

## Support

- **Issues**: [GitHub Issues](https://github.com/olavim/RoundsWithFriends/issues)
- **Discord**: Join the community Discord server
- **Documentation**: [Full documentation](https://github.com/olavim/RoundsWithFriends/wiki)