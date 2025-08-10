@echo off
REM RoundsWithFriends Build Script for Windows
REM Builds the projects based on available dependencies

setlocal enabledelayedexpansion

echo ======================================
echo RoundsWithFriends Build Script
echo ======================================

REM Configuration
set CONFIGURATION=%1
if "%CONFIGURATION%"=="" set CONFIGURATION=Debug

set SOLUTION_DIR=%~dp0
set DEDICATED_SERVER_PROJECT=%SOLUTION_DIR%RoundsWithFriends.DedicatedServer\RoundsWithFriends.DedicatedServer.csproj
set UNITY_MOD_PROJECT=%SOLUTION_DIR%RoundsWithFriends\RoundsWithFriends.csproj

echo Configuration: %CONFIGURATION%
echo Solution Directory: %SOLUTION_DIR%
echo.

REM Check .NET SDK
echo Checking .NET SDK...
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo ❌ .NET SDK not found. Please install .NET 8.0 SDK.
    exit /b 1
)

for /f "delims=" %%i in ('dotnet --version') do set DOTNET_VERSION=%%i
echo ✅ .NET SDK version: %DOTNET_VERSION%
echo.

REM Restore packages
echo Restoring NuGet packages...
cd /d "%SOLUTION_DIR%"
dotnet restore
if errorlevel 1 (
    echo ❌ Package restore failed
    exit /b 1
)
echo ✅ Package restore completed
echo.

REM Build Dedicated Server (always buildable)
echo Building Dedicated Server...
dotnet build "%DEDICATED_SERVER_PROJECT%" --configuration "%CONFIGURATION%" --no-restore
if errorlevel 1 (
    echo ❌ Dedicated Server build failed
    exit /b 1
)
echo ✅ Dedicated Server build completed successfully
echo.

REM Check if Unity mod can be built (requires ROUNDS game assemblies)
echo Checking Unity Mod dependencies...
set ROUNDS_FOLDER=C:\Program Files (x86)\Steam\steamapps\common\ROUNDS
if exist "%ROUNDS_FOLDER%" (
    echo ✅ ROUNDS installation detected, attempting Unity Mod build...
    dotnet build "%UNITY_MOD_PROJECT%" --configuration "%CONFIGURATION%" --no-restore
    if errorlevel 1 (
        echo ⚠️  Unity Mod build failed ^(missing game assemblies^)
        echo    This is expected in CI/development environments without ROUNDS installed
    ) else (
        echo ✅ Unity Mod build completed successfully
    )
) else (
    echo ⚠️  ROUNDS installation not found, skipping Unity Mod build
    echo    Unity Mod requires ROUNDS game to be installed with assemblies available
)
echo.

REM Output build results
echo ======================================
echo Build Summary
echo ======================================
echo ✅ Dedicated Server: Build completed
if exist "%SOLUTION_DIR%RoundsWithFriends.DedicatedServer\bin\%CONFIGURATION%" (
    set SERVER_OUTPUT=%SOLUTION_DIR%RoundsWithFriends.DedicatedServer\bin\%CONFIGURATION%\net8.0
    echo    Output: !SERVER_OUTPUT!
    if exist "!SERVER_OUTPUT!\RoundsWithFriends.DedicatedServer.exe" (
        echo    ✅ Server executable created
    )
)

if exist "%SOLUTION_DIR%RoundsWithFriends\bin\%CONFIGURATION%" (
    echo ✅ Unity Mod: Build completed
    set MOD_OUTPUT=%SOLUTION_DIR%RoundsWithFriends\bin\%CONFIGURATION%
    echo    Output: !MOD_OUTPUT!
) else (
    echo ⚠️  Unity Mod: Skipped ^(requires ROUNDS game installation^)
)
echo.

echo ✅ Build script completed!
pause