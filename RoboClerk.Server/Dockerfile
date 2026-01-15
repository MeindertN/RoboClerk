# ==============================================================================
# RoboClerk.Server Dockerfile
# Multi-stage build for optimized production image
# ==============================================================================

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution file and project files first for better layer caching
COPY ["RoboClerk.sln", "./"]
COPY ["RoboClerk.Server/RoboClerk.Server.csproj", "RoboClerk.Server/"]
COPY ["RoboClerk/RoboClerk.csproj", "RoboClerk/"]
COPY ["RoboClerk.Core/RoboClerk.Core.csproj", "RoboClerk.Core/"]
COPY ["RoboClerk.SharePointFileProviderPlugin/RoboClerk.SharePointFileProvider.csproj", "RoboClerk.SharePointFileProviderPlugin/"]

# Restore dependencies
RUN dotnet restore "RoboClerk.Server/RoboClerk.Server.csproj"

# Copy all source code
COPY . .

# Build and publish
WORKDIR "/src/RoboClerk.Server"
RUN dotnet publish -c Release -o /app/publish --no-restore

# ==============================================================================
# Runtime stage - using Alpine for smaller image size
# ==============================================================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime

# Install required dependencies
# - icu-libs: Required for globalization support
# - wget: Required for health check
RUN apk add --no-cache icu-libs wget

WORKDIR /app

# Set environment variables
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    # Ensure native libraries can be found
    LD_LIBRARY_PATH=/app/runtimes/linux-musl-x64/native

# Copy published output from build stage
COPY --from=build /app/publish .

# Create directories for configuration, logs, and plugins
RUN mkdir -p /app/logs /app/config /app/plugins /app/RoboClerk_input/RoboClerkConfig /app/RoboClerk_input/PluginConfig

# Set proper permissions
RUN chmod -R 755 /app

# Expose the HTTP port
EXPOSE 8080

# Health check - uses wget since curl is not in alpine by default
HEALTHCHECK --interval=30s --timeout=10s --start-period=10s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health/live || exit 1

# Create non-root user for security
RUN adduser -D -u 1000 roboclerk && \
    chown -R roboclerk:roboclerk /app

# Switch to non-root user
USER roboclerk

# Entry point
ENTRYPOINT ["dotnet", "RoboClerk.Server.dll"]
