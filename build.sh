#!/bin/bash

# RoundsWithFriends Build Script
# Builds the projects based on available dependencies

set -e

echo "======================================"
echo "RoundsWithFriends Build Script"
echo "======================================"

# Configuration
CONFIGURATION=${1:-"Debug"}
SOLUTION_DIR=$(dirname "$0")
DEDICATED_SERVER_PROJECT="$SOLUTION_DIR/RoundsWithFriends.DedicatedServer/RoundsWithFriends.DedicatedServer.csproj"
UNITY_MOD_PROJECT="$SOLUTION_DIR/RoundsWithFriends/RoundsWithFriends.csproj"

echo "Configuration: $CONFIGURATION"
echo "Solution Directory: $SOLUTION_DIR"
echo ""

# Check .NET SDK
echo "Checking .NET SDK..."
if ! command -v dotnet &> /dev/null; then
    echo "❌ .NET SDK not found. Please install .NET 8.0 SDK."
    exit 1
fi

DOTNET_VERSION=$(dotnet --version)
echo "✅ .NET SDK version: $DOTNET_VERSION"
echo ""

# Restore packages
echo "Restoring NuGet packages..."
cd "$SOLUTION_DIR"
dotnet restore
echo "✅ Package restore completed"
echo ""

# Build Dedicated Server (always buildable)
echo "Building Dedicated Server..."
dotnet build "$DEDICATED_SERVER_PROJECT" --configuration "$CONFIGURATION" --no-restore
if [ $? -eq 0 ]; then
    echo "✅ Dedicated Server build completed successfully"
else
    echo "❌ Dedicated Server build failed"
    exit 1
fi
echo ""

# Check if Unity mod can be built (requires ROUNDS game assemblies)
echo "Checking Unity Mod dependencies..."
ROUNDS_FOLDER="C:\\Program Files (x86)\\Steam\\steamapps\\common\\ROUNDS"
if [ -d "/mnt/c/Program Files (x86)/Steam/steamapps/common/ROUNDS" ] || [ -d "$HOME/.steam/steam/steamapps/common/ROUNDS" ]; then
    echo "✅ ROUNDS installation detected, attempting Unity Mod build..."
    dotnet build "$UNITY_MOD_PROJECT" --configuration "$CONFIGURATION" --no-restore
    if [ $? -eq 0 ]; then
        echo "✅ Unity Mod build completed successfully"
    else
        echo "⚠️  Unity Mod build failed (missing game assemblies)"
        echo "   This is expected in CI/development environments without ROUNDS installed"
    fi
else
    echo "⚠️  ROUNDS installation not found, skipping Unity Mod build"
    echo "   Unity Mod requires ROUNDS game to be installed with assemblies available"
fi
echo ""

# Output build results
echo "======================================"
echo "Build Summary"
echo "======================================"
echo "✅ Dedicated Server: Build completed"
if [ -d "$SOLUTION_DIR/RoundsWithFriends.DedicatedServer/bin/$CONFIGURATION" ]; then
    SERVER_OUTPUT="$SOLUTION_DIR/RoundsWithFriends.DedicatedServer/bin/$CONFIGURATION/net8.0"
    echo "   Output: $SERVER_OUTPUT"
    if [ -f "$SERVER_OUTPUT/RoundsWithFriends.DedicatedServer.dll" ]; then
        echo "   ✅ Server executable created"
    fi
fi

if [ -d "$SOLUTION_DIR/RoundsWithFriends/bin/$CONFIGURATION" ]; then
    echo "✅ Unity Mod: Build completed"
    MOD_OUTPUT="$SOLUTION_DIR/RoundsWithFriends/bin/$CONFIGURATION"
    echo "   Output: $MOD_OUTPUT"
else
    echo "⚠️  Unity Mod: Skipped (requires ROUNDS game installation)"
fi
echo ""

echo "✅ Build script completed!"