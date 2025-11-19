# RoboClerk Server Configuration

This document explains the new server configuration system for RoboClerk.Server.

## Overview

RoboClerk.Server now supports a separate configuration file (`RoboClerk.Server.toml`) that allows you to customize server-specific settings without affecting the core RoboClerk configuration.

## Default Behavior

When you build RoboClerk.Server, the configuration file is automatically copied to the output directory alongside the server executable. The server will automatically load `RoboClerk.Server.toml` from its own directory on startup.

**No additional setup required** - just run the server and it will use the default configuration.

## Configuration Files

### RoboClerk.Server.toml

This file contains all server-specific settings organized into logical sections:

- **Server**: Basic server settings (ports, host address, HTTPS)
- **API**: API-related settings (base path, Swagger, request limits)
- **CORS**: Cross-origin resource sharing settings
- **Logging**: Server-specific logging configuration
- **Session**: Project session management
- **Performance**: Caching and performance settings
- **Security**: Authentication and rate limiting
- **FileSystem**: File access and validation settings
- **SharePoint**: SharePoint integration settings
- **HealthCheck**: Health monitoring endpoints
- **Monitoring**: Application monitoring and diagnostics

## Command Line Usage

You can specify a custom server configuration file using the command line:

```bash
RoboClerk.Server.exe --serverConfigurationFile "path/to/custom-server.toml"
```

## Configuration Override

You can also override specific configuration values via command line using dot notation:

```bash
RoboClerk.Server.exe --options "Server.HttpPort=8080" "CORS.AllowedOrigins=https://example.com"
```

## Quick Start Examples

### Change the Port

To run the server on a different port, simply edit the `RoboClerk.Server.toml` file in the server directory:

```toml
[Server]
HttpPort = 8080
HttpsPort = 8443
```

Or override via command line:
```bash
RoboClerk.Server.exe --options "Server.HttpPort=8080"
```

### Enable Production Swagger

```toml
[API]
EnableSwaggerInProduction = true
```

### Configure CORS for Specific Domain

```toml
[CORS]
AllowedOrigins = "https://yourdomain.com,https://app.yourdomain.com"
```

## Common Configuration Scenarios

### Development Environment

For development, you might want:
- Different ports to avoid conflicts
- Enable Swagger in production for API testing
- More detailed logging
- Relaxed CORS settings

```toml
[Server]
HttpPort = 8080
HttpsPort = 8443
Environment = "Development"

[API]
EnableSwaggerInProduction = true

[Logging]
ServerLogLevel = "DEBUG"
LogApiRequests = true

[CORS]
AllowedOrigins = "*"
```

### Production Environment

For production, you should configure:
- Secure CORS settings
- Performance optimizations
- Security features
- Monitoring

```toml
[Server]
HttpPort = 80
HttpsPort = 443
HostAddress = "0.0.0.0"
Environment = "Production"

[CORS]
AllowedOrigins = "https://yourdomain.com"
AllowCredentials = false

[Security]
EnableRateLimiting = true
RateLimitRequestsPerMinute = 60

[Monitoring]
EnableApplicationInsights = true
MonitorMemoryUsage = true
```

### SharePoint Integration

For SharePoint-based projects:

```toml
[SharePoint]
OperationTimeoutSeconds = 180
RetryCount = 5
RetryDelayMilliseconds = 2000
CacheAuthTokens = true

[Session]
ProjectSessionTimeoutMinutes = 120
MaxConcurrentProjects = 5

[Performance]
MaxProjectMemoryMB = 1000
```

## Settings Reference

### Server Section

| Setting | Default | Description |
|---------|---------|-------------|
| `HttpPort` | 51046 | HTTP port number |
| `HttpsPort` | 51045 | HTTPS port number |
| `HostAddress` | "localhost" | Host address to bind to |
| `UseHttpsRedirection` | true | Enable HTTPS redirection |
| `Environment` | "Development" | Application environment |

### API Section

| Setting | Default | Description |
|---------|---------|-------------|
| `BasePath` | "" | API base path prefix |
| `EnableSwaggerInProduction` | false | Enable Swagger in production |
| `SwaggerRoutePrefix` | "" | Swagger UI route prefix |
| `MaxRequestBodySize` | 31457280 | Max request body size (bytes) |
| `RequestTimeoutSeconds` | 300 | Request timeout in seconds |

### CORS Section

| Setting | Default | Description |
|---------|---------|-------------|
| `EnableCORS` | true | Enable CORS support |
| `AllowedOrigins` | "*" | Allowed origins (comma-separated) |
| `AllowedMethods` | "*" | Allowed HTTP methods |
| `AllowedHeaders` | "*" | Allowed headers |
| `AllowCredentials` | false | Allow credentials in requests |

### Session Section

| Setting | Default | Description |
|---------|---------|-------------|
| `ProjectSessionTimeoutMinutes` | 60 | Project session timeout |
| `MaxConcurrentProjects` | 10 | Max concurrent loaded projects |
| `SessionCleanupIntervalMinutes` | 15 | Session cleanup interval |

### Security Section

| Setting | Default | Description |
|---------|---------|-------------|
| `EnableApiKeyAuth` | false | Enable API key authentication |
| `ApiKeyHeaderName` | "X-API-Key" | API key header name |
| `EnableRateLimiting` | false | Enable rate limiting |
| `RateLimitRequestsPerMinute` | 100 | Rate limit per minute per IP |

## File Location and Deployment

### Default Location
By default, the server looks for `RoboClerk.Server.toml` in the same directory as the server executable.

### Automatic Deployment
The build process automatically copies the configuration file from the `Configuration` folder to the output directory, so it's always available alongside the server executable.

### Custom Location
You can override the location using the `--serverConfigurationFile` command line parameter:

```bash
RoboClerk.Server.exe --serverConfigurationFile "C:\MyConfigs\custom-server.toml"
```

## Loading Order

1. Default configuration values are loaded from the ServerConfiguration class
2. Values from `RoboClerk.Server.toml` (in the server directory) override defaults
3. Command line options override file configuration
4. The final configuration is validated for consistency

## Error Handling

If the configuration file is not found or contains errors:
- A warning is logged
- Default values are used
- The server continues to start normally

This ensures the server can always start, even with configuration issues.

## Integration with RoboClerk Core

The server configuration is separate from and complementary to the core RoboClerk configuration:
- `RoboClerk.toml` - Core RoboClerk settings (plugins, output format, etc.)
- `RoboClerk.Server.toml` - Server-specific settings (ports, CORS, sessions, etc.)

Both configuration files are loaded independently and serve different purposes.