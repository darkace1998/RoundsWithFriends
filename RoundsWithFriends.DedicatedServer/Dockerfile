# Multi-stage Docker build for RoundsWithFriends Dedicated Server

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY ["RoundsWithFriends.DedicatedServer/RoundsWithFriends.DedicatedServer.csproj", "RoundsWithFriends.DedicatedServer/"]

# Restore dependencies
RUN dotnet restore "RoundsWithFriends.DedicatedServer/RoundsWithFriends.DedicatedServer.csproj"

# Copy source code
COPY RoundsWithFriends.DedicatedServer/ RoundsWithFriends.DedicatedServer/

# Build the application
WORKDIR "/src/RoundsWithFriends.DedicatedServer"
RUN dotnet build "RoundsWithFriends.DedicatedServer.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "RoundsWithFriends.DedicatedServer.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Create non-root user for security
RUN addgroup --system --gid 1001 rwfserver && \
    adduser --system --uid 1001 --group rwfserver

# Copy published application
COPY --from=publish /app/publish .

# Create directories and set permissions
RUN mkdir -p /app/logs /app/config && \
    chown -R rwfserver:rwfserver /app

# Switch to non-root user
USER rwfserver

# Expose ports
EXPOSE 7777/tcp
EXPOSE 8080/tcp

# Environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_RUNNING_IN_CONTAINER=true

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=30s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Entry point
ENTRYPOINT ["dotnet", "RoundsWithFriends.DedicatedServer.dll"]