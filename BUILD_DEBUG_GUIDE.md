# Build and Debug Guide

This guide covers the enhanced build and debug capabilities for RoundsWithFriends.

## Quick Start

### Building the Project

#### Option 1: Use Build Scripts (Recommended)
```bash
# Linux/macOS
./build.sh [Debug|Release]

# Windows
build.bat [Debug|Release]
```

#### Option 2: Manual Build
```bash
# Build dedicated server only (always works)
dotnet build RoundsWithFriends.DedicatedServer --configuration Debug

# Build full solution (requires ROUNDS game installation)
dotnet build --configuration Debug
```

### Running Debug Server

#### Quick Debug Launch
```bash
./debug-server.sh
```

#### Manual Debug Launch
```bash
cd RoundsWithFriends.DedicatedServer
ASPNETCORE_ENVIRONMENT=Development dotnet run --configuration Debug
```

## Build System

### Project Structure
- **RoundsWithFriends** - Unity mod (requires ROUNDS game assemblies)
- **RoundsWithFriends.DedicatedServer** - .NET 8.0 standalone server

### Build Scripts Features
- ✅ **Cross-platform support** (Linux, macOS, Windows)
- ✅ **Dependency detection** (automatically detects ROUNDS installation)
- ✅ **Intelligent fallback** (builds server if Unity mod can't build)
- ✅ **Clear error reporting** with colored output
- ✅ **Build verification** and output validation

### Build Requirements
- **.NET 8.0 SDK** (for dedicated server)
- **ROUNDS Game Installation** (for Unity mod)
  - Default paths checked:
    - Windows: `C:\Program Files (x86)\Steam\steamapps\common\ROUNDS`
    - Linux: `~/.steam/steam/steamapps/common/ROUNDS`

## Debug Features

### 🔧 Debug Configuration Levels

#### Production (appsettings.json)
```json
{
  "Debug": {
    "EnableDebugConsole": false,
    "EnableHealthMonitoring": true,
    "HealthCheckInterval": 60,
    "EnablePerformanceMetrics": false
  }
}
```

#### Development (appsettings.Development.json)
```json
{
  "Debug": {
    "EnableDebugConsole": true,
    "EnableHealthMonitoring": true,
    "HealthCheckInterval": 30,
    "EnablePerformanceMetrics": true,
    "EnableStackTraces": true,
    "LogMemoryUsage": true
  }
}
```

### 🖥️ Interactive Debug Console

The debug console provides real-time server administration and debugging.

#### Available Commands
- `help` - Show all available commands
- `status` - Display server status and statistics  
- `debug` - Show detailed debug information
- `players` - List connected players
- `sessions` - List active game sessions
- `memory` - Show memory usage information
- `gc` - Force garbage collection
- `config` - Show current configuration
- `logs <level>` - Change log level dynamically
- `clear` - Clear the console
- `exit/quit` - Exit debug console

#### Example Usage
```
[DEBUG] > status

=== SERVER STATUS ===
Name: RoundsWithFriends Dev Server
Version: 1.0.0.0
State: Running
Uptime: 00.00:05:42
Players: 0/16
Active Sessions: 0
Process ID: 12345
Threads: 18
```

### 📊 Health Monitoring

Automatic health monitoring tracks server performance and issues.

#### Monitored Metrics
- **Memory Usage** (Working Set, GC Memory)
- **Thread Count** 
- **Connected Players**
- **Active Game Sessions**
- **Garbage Collection Statistics**
- **Uptime**

#### Health Checks
- Memory usage warnings (>500MB)
- High GC memory alerts (>200MB)
- Frequent Gen 2 GC collections
- Thread count monitoring (>50 threads)
- Server state validation

#### Sample Health Output
```
Health Check: Healthy - Memory: 47MB, CPU: 0.0%, Players: 0, Issues: 0
Memory Usage: Working Set: 47MB, GC: 1MB, Threads: 18
```

### 📈 Performance Monitoring

#### Memory Tracking
```
[DEBUG] > memory

=== MEMORY USAGE ===
Working Set: 47 MB
Private Memory: 45 MB
GC Memory: 1 MB
Gen 0 Collections: 5
Gen 1 Collections: 2
Gen 2 Collections: 0
```

#### Server Statistics
```
[DEBUG] > status

Name: RoundsWithFriends Dev Server
Players: 0/16
Active Sessions: 0
Uptime: 00.00:15:30
Process ID: 12345
Threads: 18
```

### 🔍 Enhanced Logging

#### Log Levels by Environment
- **Production**: Information level with minimal output
- **Development**: Debug/Trace level with detailed information

#### Log Features
- ✅ **Timestamped logs** with millisecond precision
- ✅ **Structured logging** with consistent formatting
- ✅ **Memory usage logging** (in debug mode)
- ✅ **Player connection tracking**
- ✅ **Network message logging** (optional)
- ✅ **Stack traces** (in debug mode)

#### Log Configuration
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "RWF.DedicatedServer": "Trace"
    }
  }
}
```

## Debugging Workflows

### 🐛 Common Debug Scenarios

#### 1. Performance Issues
```bash
[DEBUG] > memory    # Check memory usage
[DEBUG] > gc        # Force garbage collection
[DEBUG] > memory    # Compare after GC
```

#### 2. Connection Problems
```bash
[DEBUG] > status    # Check server state
[DEBUG] > players   # List connected players
[DEBUG] > config    # Verify configuration
```

#### 3. Memory Leaks
```bash
# Monitor over time:
[DEBUG] > memory
# Wait some time...
[DEBUG] > memory
# If memory grows consistently, investigate
```

### 🔧 Development Tips

#### Build & Debug Cycle
1. Make code changes
2. Run `./build.sh Debug` to build
3. Run `./debug-server.sh` to test
4. Use debug console for real-time testing
5. Monitor health metrics for issues

#### Debug Console Integration
The debug console runs on a background thread, allowing real-time interaction without stopping the server.

#### Health Monitoring Integration
Health checks run automatically and log warnings when issues are detected, making it easy to spot problems during development.

## Error Handling

### Enhanced Error Reporting
- **Stack traces** in development mode
- **Detailed exception information** 
- **Graceful degradation** when features unavailable
- **Cross-platform compatibility** handling

### Common Issues & Solutions

#### Build Issues
- **Unity Mod Build Fails**: Ensure ROUNDS is installed with assemblies
- **.NET Version**: Requires .NET 8.0 SDK

#### Runtime Issues  
- **Port Already In Use**: Check configuration or kill existing processes
- **Permission Denied**: Ensure proper file permissions on Linux/macOS

## Integration with Development Tools

### IDE Integration
- **Debug symbols** available in Debug builds
- **Source linking** for better debugging experience
- **Conditional compilation** for debug-only features

### CI/CD Considerations
- Build scripts handle missing dependencies gracefully
- Dedicated server builds independently of Unity mod
- Clear exit codes for automation

This enhanced build and debug system provides a comprehensive development experience for the RoundsWithFriends project, supporting both experienced developers and contributors new to the project.