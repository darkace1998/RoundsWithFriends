#!/bin/bash

# Debug Run Script for RoundsWithFriends Dedicated Server
# Builds and runs the server with enhanced debugging enabled

set -e

echo "======================================"
echo "RWF Dedicated Server - Debug Mode"
echo "======================================"

SCRIPT_DIR=$(dirname "$0")
PROJECT_DIR="$SCRIPT_DIR/RoundsWithFriends.DedicatedServer"

# Build in debug mode first
echo "Building in Debug configuration..."
cd "$SCRIPT_DIR"
dotnet build "$PROJECT_DIR" --configuration Debug --no-restore

if [ $? -ne 0 ]; then
    echo "❌ Build failed, cannot start debug server"
    exit 1
fi

echo "✅ Build completed successfully"
echo ""

# Set environment to Development for enhanced debugging
export ASPNETCORE_ENVIRONMENT=Development
export DOTNET_ENVIRONMENT=Development

echo "Environment: Development"
echo "Debug features enabled:"
echo "  ✅ Debug Console (interactive commands)"
echo "  ✅ Health Monitoring (30s intervals)"
echo "  ✅ Enhanced Logging (Trace level)"
echo "  ✅ Memory Usage Logging"
echo "  ✅ Performance Metrics"
echo "  ✅ Stack Traces on errors"
echo ""

echo "======================================"
echo "Starting Debug Server..."
echo "======================================"
echo "Available debug commands:"
echo "  help     - Show debug commands"
echo "  status   - Server status"
echo "  players  - List players"
echo "  memory   - Memory usage"
echo "  debug    - Debug info"
echo "  gc       - Force garbage collection"
echo "======================================"
echo ""

# Run the server
cd "$PROJECT_DIR"
dotnet run --configuration Debug --no-build

echo ""
echo "Debug server stopped."