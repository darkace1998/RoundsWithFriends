#!/bin/bash

# RoundsWithFriends Dedicated Server Deployment Script
# This script automates the deployment of the dedicated server

set -e

# Configuration
SERVER_NAME="${SERVER_NAME:-RoundsWithFriends-Server}"
SERVER_PORT="${SERVER_PORT:-7777}"
WEB_ADMIN_PORT="${WEB_ADMIN_PORT:-8080}"
MAX_PLAYERS="${MAX_PLAYERS:-16}"
ENVIRONMENT="${ENVIRONMENT:-Production}"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Logging functions
log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

log_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Check prerequisites
check_prerequisites() {
    log_info "Checking prerequisites..."
    
    # Check if .NET 8.0 is installed
    if ! command -v dotnet &> /dev/null; then
        log_error ".NET 8.0 is not installed. Please install it first."
        log_info "Download from: https://dotnet.microsoft.com/download/dotnet/8.0"
        exit 1
    fi
    
    # Check .NET version
    DOTNET_VERSION=$(dotnet --version | cut -d. -f1)
    if [ "$DOTNET_VERSION" -lt 8 ]; then
        log_error ".NET 8.0 or higher is required. Current version: $(dotnet --version)"
        exit 1
    fi
    
    log_success ".NET 8.0 is available"
}

# Build the server
build_server() {
    log_info "Building RoundsWithFriends Dedicated Server..."
    
    if [ ! -f "RoundsWithFriends.DedicatedServer/RoundsWithFriends.DedicatedServer.csproj" ]; then
        log_error "RoundsWithFriends.DedicatedServer project not found"
        exit 1
    fi
    
    # Build in release mode
    dotnet build RoundsWithFriends.DedicatedServer -c Release --nologo
    
    if [ $? -eq 0 ]; then
        log_success "Server built successfully"
    else
        log_error "Build failed"
        exit 1
    fi
}

# Publish the server
publish_server() {
    log_info "Publishing server for deployment..."
    
    local output_dir="deploy/${SERVER_NAME}"
    
    # Clean previous deployment
    if [ -d "$output_dir" ]; then
        rm -rf "$output_dir"
    fi
    
    mkdir -p "$output_dir"
    
    # Publish the application
    dotnet publish RoundsWithFriends.DedicatedServer \
        -c Release \
        -o "$output_dir" \
        --self-contained false \
        --nologo
    
    if [ $? -eq 0 ]; then
        log_success "Server published to $output_dir"
    else
        log_error "Publish failed"
        exit 1
    fi
    
    # Create configuration file
    create_config "$output_dir"
    
    # Create startup scripts
    create_startup_scripts "$output_dir"
    
    # Set permissions
    chmod +x "$output_dir"/*.sh 2>/dev/null || true
}

# Create configuration file
create_config() {
    local output_dir="$1"
    local config_file="$output_dir/appsettings.production.json"
    
    log_info "Creating production configuration..."
    
    cat > "$config_file" << EOF
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "Server": {
    "Name": "$SERVER_NAME",
    "Description": "RoundsWithFriends Dedicated Server",
    "Port": $SERVER_PORT,
    "MaxPlayers": $MAX_PLAYERS,
    "MaxTeams": $MAX_PLAYERS,
    "TickRate": 60,
    "EnableConsole": true,
    "EnableWebAdmin": true,
    "WebAdminPort": $WEB_ADMIN_PORT
  },
  "GameModes": {
    "Available": [
      "TeamDeathmatch",
      "Deathmatch"
    ],
    "Default": "TeamDeathmatch"
  },
  "Security": {
    "RequireAuthentication": false,
    "AdminPassword": "",
    "BanList": [],
    "Whitelist": []
  },
  "Performance": {
    "MaxConcurrentGames": 1,
    "HeartbeatInterval": 30000,
    "ConnectionTimeout": 60000
  }
}
EOF
    
    log_success "Configuration created: $config_file"
}

# Create startup scripts
create_startup_scripts() {
    local output_dir="$1"
    
    # Linux/macOS startup script
    cat > "$output_dir/start-server.sh" << 'EOF'
#!/bin/bash

# RoundsWithFriends Dedicated Server Startup Script

cd "$(dirname "$0")"

SERVER_PID_FILE="server.pid"
LOG_FILE="server.log"

start_server() {
    if [ -f "$SERVER_PID_FILE" ]; then
        PID=$(cat "$SERVER_PID_FILE")
        if ps -p "$PID" > /dev/null 2>&1; then
            echo "Server is already running (PID: $PID)"
            return 1
        else
            rm -f "$SERVER_PID_FILE"
        fi
    fi
    
    echo "Starting RoundsWithFriends Dedicated Server..."
    
    # Start server in background
    nohup dotnet RoundsWithFriends.DedicatedServer.dll --environment Production > "$LOG_FILE" 2>&1 &
    
    echo $! > "$SERVER_PID_FILE"
    echo "Server started with PID: $!"
    echo "Logs: $LOG_FILE"
    echo "Admin interface: http://localhost:8080"
}

stop_server() {
    if [ -f "$SERVER_PID_FILE" ]; then
        PID=$(cat "$SERVER_PID_FILE")
        if ps -p "$PID" > /dev/null 2>&1; then
            echo "Stopping server (PID: $PID)..."
            kill "$PID"
            
            # Wait for graceful shutdown
            for i in {1..10}; do
                if ! ps -p "$PID" > /dev/null 2>&1; then
                    break
                fi
                sleep 1
            done
            
            # Force kill if still running
            if ps -p "$PID" > /dev/null 2>&1; then
                echo "Force killing server..."
                kill -9 "$PID"
            fi
            
            rm -f "$SERVER_PID_FILE"
            echo "Server stopped"
        else
            echo "Server is not running"
            rm -f "$SERVER_PID_FILE"
        fi
    else
        echo "Server is not running"
    fi
}

status_server() {
    if [ -f "$SERVER_PID_FILE" ]; then
        PID=$(cat "$SERVER_PID_FILE")
        if ps -p "$PID" > /dev/null 2>&1; then
            echo "Server is running (PID: $PID)"
            return 0
        else
            echo "Server is not running (stale PID file)"
            rm -f "$SERVER_PID_FILE"
            return 1
        fi
    else
        echo "Server is not running"
        return 1
    fi
}

case "$1" in
    start)
        start_server
        ;;
    stop)
        stop_server
        ;;
    restart)
        stop_server
        sleep 2
        start_server
        ;;
    status)
        status_server
        ;;
    logs)
        if [ -f "$LOG_FILE" ]; then
            tail -f "$LOG_FILE"
        else
            echo "Log file not found: $LOG_FILE"
        fi
        ;;
    *)
        echo "Usage: $0 {start|stop|restart|status|logs}"
        exit 1
        ;;
esac
EOF

    # Windows startup script
    cat > "$output_dir/start-server.bat" << 'EOF'
@echo off
REM RoundsWithFriends Dedicated Server Startup Script for Windows

cd /d "%~dp0"

set SERVER_PID_FILE=server.pid
set LOG_FILE=server.log

if "%1"=="start" goto start
if "%1"=="stop" goto stop
if "%1"=="restart" goto restart
if "%1"=="status" goto status
if "%1"=="logs" goto logs

echo Usage: %0 {start^|stop^|restart^|status^|logs}
exit /b 1

:start
echo Starting RoundsWithFriends Dedicated Server...
start "RWF Server" /min dotnet RoundsWithFriends.DedicatedServer.dll --environment Production
echo Server started
echo Admin interface: http://localhost:8080
goto end

:stop
echo Stopping server...
taskkill /f /im "dotnet.exe" 2>nul
echo Server stopped
goto end

:restart
call :stop
timeout /t 2 /nobreak >nul
call :start
goto end

:status
tasklist /fi "imagename eq dotnet.exe" | find "dotnet.exe" >nul
if %errorlevel%==0 (
    echo Server is running
) else (
    echo Server is not running
)
goto end

:logs
if exist "%LOG_FILE%" (
    type "%LOG_FILE%"
) else (
    echo Log file not found: %LOG_FILE%
)
goto end

:end
EOF
    
    log_success "Startup scripts created"
}

# Create systemd service (Linux)
create_systemd_service() {
    local output_dir="$1"
    local service_file="/etc/systemd/system/rwf-server.service"
    
    if [ "$EUID" -ne 0 ]; then
        log_warning "Root privileges required to create systemd service"
        log_info "To install systemd service manually:"
        log_info "1. Copy the service file to /etc/systemd/system/"
        log_info "2. Run: sudo systemctl daemon-reload"
        log_info "3. Run: sudo systemctl enable rwf-server"
        
        # Create service file in deployment directory
        service_file="$output_dir/rwf-server.service"
    fi
    
    cat > "$service_file" << EOF
[Unit]
Description=RoundsWithFriends Dedicated Server
After=network.target

[Service]
Type=notify
User=rwf-server
Group=rwf-server
WorkingDirectory=$output_dir
ExecStart=/usr/bin/dotnet $output_dir/RoundsWithFriends.DedicatedServer.dll --environment Production
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=rwf-server
Environment=DOTNET_ENVIRONMENT=Production

[Install]
WantedBy=multi-user.target
EOF
    
    if [ "$service_file" != "$output_dir/rwf-server.service" ]; then
        systemctl daemon-reload
        log_success "Systemd service installed: $service_file"
        log_info "Enable with: sudo systemctl enable rwf-server"
        log_info "Start with: sudo systemctl start rwf-server"
    else
        log_success "Systemd service file created: $service_file"
    fi
}

# Setup firewall rules
setup_firewall() {
    log_info "Setting up firewall rules..."
    
    # Detect firewall system
    if command -v ufw &> /dev/null; then
        # Ubuntu/Debian UFW
        log_info "Configuring UFW firewall..."
        sudo ufw allow $SERVER_PORT/tcp comment "RWF Server"
        sudo ufw allow $WEB_ADMIN_PORT/tcp comment "RWF Admin"
        log_success "UFW rules added"
    elif command -v firewall-cmd &> /dev/null; then
        # CentOS/RHEL/Fedora firewalld
        log_info "Configuring firewalld..."
        sudo firewall-cmd --permanent --add-port=$SERVER_PORT/tcp
        sudo firewall-cmd --permanent --add-port=$WEB_ADMIN_PORT/tcp
        sudo firewall-cmd --reload
        log_success "Firewalld rules added"
    elif command -v iptables &> /dev/null; then
        # Generic iptables
        log_info "Configuring iptables..."
        sudo iptables -A INPUT -p tcp --dport $SERVER_PORT -j ACCEPT
        sudo iptables -A INPUT -p tcp --dport $WEB_ADMIN_PORT -j ACCEPT
        log_warning "iptables rules added but not persisted. Consider using iptables-persistent"
    else
        log_warning "No recognized firewall system found"
        log_info "Please manually open ports $SERVER_PORT and $WEB_ADMIN_PORT"
    fi
}

# Create Docker files
create_docker_files() {
    local output_dir="$1"
    
    log_info "Creating Docker deployment files..."
    
    # Copy existing Docker files if they exist
    if [ -f "RoundsWithFriends.DedicatedServer/Dockerfile" ]; then
        cp "RoundsWithFriends.DedicatedServer/Dockerfile" "$output_dir/"
    fi
    
    if [ -f "RoundsWithFriends.DedicatedServer/docker-compose.yml" ]; then
        cp "RoundsWithFriends.DedicatedServer/docker-compose.yml" "$output_dir/"
    fi
    
    # Create simple Docker run script
    cat > "$output_dir/docker-run.sh" << EOF
#!/bin/bash

# Simple Docker deployment script

docker build -t rwf-server .

docker run -d \\
    --name rwf-server \\
    -p $SERVER_PORT:7777 \\
    -p $WEB_ADMIN_PORT:8080 \\
    -v \$(pwd)/config:/app/config \\
    -v \$(pwd)/logs:/app/logs \\
    --restart unless-stopped \\
    rwf-server

echo "Server started in Docker container"
echo "Game port: $SERVER_PORT"
echo "Admin interface: http://localhost:$WEB_ADMIN_PORT"
EOF
    
    chmod +x "$output_dir/docker-run.sh"
    log_success "Docker files created"
}

# Main deployment function
deploy() {
    log_info "Starting RoundsWithFriends Dedicated Server deployment..."
    log_info "Configuration:"
    log_info "  Server Name: $SERVER_NAME"
    log_info "  Game Port: $SERVER_PORT"
    log_info "  Admin Port: $WEB_ADMIN_PORT"
    log_info "  Max Players: $MAX_PLAYERS"
    log_info "  Environment: $ENVIRONMENT"
    echo
    
    check_prerequisites
    build_server
    publish_server
    
    local output_dir="deploy/${SERVER_NAME}"
    
    # Create additional deployment files
    create_docker_files "$output_dir"
    
    # Linux-specific setup
    if [[ "$OSTYPE" == "linux-gnu"* ]]; then
        create_systemd_service "$output_dir"
        
        # Offer to setup firewall
        read -p "Setup firewall rules? (y/N): " setup_fw
        if [[ $setup_fw =~ ^[Yy]$ ]]; then
            setup_firewall
        fi
    fi
    
    echo
    log_success "Deployment completed successfully!"
    log_info "Deployment location: $output_dir"
    echo
    log_info "To start the server:"
    log_info "  Linux/macOS: cd $output_dir && ./start-server.sh start"
    log_info "  Windows: cd $output_dir && start-server.bat start"
    log_info "  Docker: cd $output_dir && ./docker-run.sh"
    echo
    log_info "Access admin interface at: http://localhost:$WEB_ADMIN_PORT"
    echo
}

# Handle command line arguments
case "${1:-deploy}" in
    deploy)
        deploy
        ;;
    build)
        check_prerequisites
        build_server
        ;;
    clean)
        log_info "Cleaning deployment directory..."
        rm -rf deploy/
        log_success "Clean completed"
        ;;
    help|--help|-h)
        echo "RoundsWithFriends Dedicated Server Deployment Script"
        echo
        echo "Usage: $0 [command]"
        echo
        echo "Commands:"
        echo "  deploy  - Build and deploy the server (default)"
        echo "  build   - Build the server only"
        echo "  clean   - Clean deployment directory"
        echo "  help    - Show this help"
        echo
        echo "Environment Variables:"
        echo "  SERVER_NAME      - Server name (default: RoundsWithFriends-Server)"
        echo "  SERVER_PORT      - Game server port (default: 7777)"
        echo "  WEB_ADMIN_PORT   - Web admin port (default: 8080)"
        echo "  MAX_PLAYERS      - Maximum players (default: 16)"
        echo "  ENVIRONMENT      - Environment (default: Production)"
        echo
        echo "Example:"
        echo "  SERVER_NAME='My Server' SERVER_PORT=7778 $0 deploy"
        ;;
    *)
        log_error "Unknown command: $1"
        log_info "Use '$0 help' for usage information"
        exit 1
        ;;
esac