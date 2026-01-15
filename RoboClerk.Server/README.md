# RoboClerk Server

A web API server that exposes RoboClerk functionality for integration with Word add-ins working with SharePoint documents.

## Features

- **SharePoint Project Management**: Load and manage RoboClerk projects from SharePoint
- **Content Control Processing**: Extract and process RoboClerk content controls from DOCX documents
- **OpenXML Content Generation**: Generate OpenXML content using RoboClerk content creators
- **Word Add-in Integration**: Specialized endpoints for Word add-in scenarios
- **Real-time Data Sources**: Refresh data sources for up-to-date content generation
- **Configuration Management**: View and update project configuration through the API
- **Container Ready**: Full Docker and Kubernetes deployment support
- **Health Monitoring**: Comprehensive health check endpoints for orchestration
- **Cross-platform**: Runs on Windows, Linux, and macOS

## Getting Started

### Prerequisites

- .NET 8.0 SDK
- RoboClerk project hosted on SharePoint with DOCX templates
- SharePoint App Registration in Azure AD with appropriate permissions
- Word add-in for document interaction (optional, for end-user scenarios)

### Running the Server

```bash
# Navigate to the server directory
cd RoboClerk.Server

# Run in development mode
dotnet run

# Or build and run
dotnet build
dotnet run --no-build
```

The server will start and be available at:
- HTTP: `http://localhost:8000` (default)
- HTTPS: `https://localhost:8443` (if enabled)

### Configuration

Server configuration is managed through `RoboClerk.Server.toml`. Key settings include:

```toml
[Server]
HttpPort = 8000
HostAddress = "localhost"  # Use "0.0.0.0" for containers
UseHttpsRedirection = false

[SharePoint]
ClientId = ""  # Set via SP_CLIENT_ID environment variable
TenantId = ""  # Set via SP_TENANT_ID environment variable

[CORS]
EnableCORS = true
AllowedOrigins = "*"  # Restrict in production!
```

### Environment Variables

For production deployments, use environment variables for sensitive settings:

| Variable | Description |
|----------|-------------|
| `SP_CLIENT_ID` | SharePoint App Client ID from Azure AD |
| `SP_TENANT_ID` | Azure AD Tenant ID |
| `ASPNETCORE_ENVIRONMENT` | Environment name (Development/Production) |
| `ROBOCLERK_LOG_LEVEL` | Log level (DEBUG, INFO, WARN, ERROR) |
| `ROBOCLERK_CORS_ORIGINS` | Comma-separated allowed CORS origins |

### API Documentation

When running in development mode, Swagger documentation is available at the root URL (e.g., `http://localhost:8000`).

## API Endpoints

### Health Check Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/health` | GET | Detailed health info with uptime, memory, and runtime details |
| `/health/live` | GET | Liveness probe for container orchestration |
| `/health/ready` | GET | Readiness probe with service dependency checks |
| `/health/startup` | GET | Startup probe for initialization verification |

### Word Add-in Endpoints

All Word add-in endpoints are prefixed with `/api/word-addin`.

#### Project Management

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/word-addin/project/load` | POST | Load a SharePoint project |
| `/api/word-addin/project/{projectId}/refresh` | POST | Refresh project documents and content controls |
| `/api/word-addin/project/{projectId}/refreshds` | POST | Refresh data sources only |
| `/api/word-addin/project/{projectId}` | DELETE | Unload project and cleanup resources |
| `/api/word-addin/health` | GET | Word add-in specific health check |

#### Document Operations

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/word-addin/project/{projectId}/document/{documentId}/refresh` | POST | Refresh a specific document |
| `/api/word-addin/project/{projectId}/content` | POST | Generate OpenXML content for a content control |
| `/api/word-addin/project/{projectId}/virtual-tags/stats` | GET | Get virtual tag statistics |

#### Configuration Management

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/word-addin/project/{projectId}/configuration/raw` | GET | Get raw TOML configuration |
| `/api/word-addin/project/{projectId}/configuration` | PUT | Update full configuration |
| `/api/word-addin/project/{projectId}/configuration/values` | GET | Get configuration values |
| `/api/word-addin/project/{projectId}/configuration/values` | PUT | Update configuration values |
| `/api/word-addin/project/{projectId}/configuration/validate` | POST | Validate configuration changes |

#### Template Management

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/word-addin/project/{projectId}/template-filenames` | GET | List available template files |
| `/api/word-addin/project/{projectId}/template-file` | GET | Download a template file |
| `/api/word-addin/content-creators/metadata` | GET | Get content creator metadata |

## Usage Examples

### 1. Load a SharePoint Project

**Using Document URL (recommended):**
```http
POST /api/word-addin/project/load
Content-Type: application/json

{
  "documentUrl": "https://mycompany.sharepoint.com/sites/projects/Shared%20Documents/MyProject/Templates/SRS.docx"
}
```

**Using explicit parameters:**
```http
POST /api/word-addin/project/load
Content-Type: application/json

{
  "projectPath": "sp://sites/projects/Shared Documents/MyProject",
  "spDriveId": "b!abc123...",
  "spSiteUrl": "https://mycompany.sharepoint.com/sites/projects"
}
```

Response:
```json
{
  "success": true,
  "projectId": "sp-abc123def456",
  "projectName": "MyProject"
}
```

### 2. Generate Content for a Content Control

```http
POST /api/word-addin/project/sp-abc123def456/content
Content-Type: application/json

{
  "documentId": "SRS",
  "contentControlId": "ctrl-123",
  "roboClerkTag": "@@SLMS:SWR(ItemID=REQ-001)@@"
}
```

Response:
```json
{
  "success": true,
  "content": "<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\">...</w:p>"
}
```

### 3. Get Configuration Values

```http
GET /api/word-addin/project/sp-abc123def456/configuration/values
```

Response:
```json
{
  "CompanyName": "Acme Inc.",
  "SoftwareName": "MyProduct",
  "SoftwareVersion": "1.0.0",
  "ProjectIdentifier": "PRJ-001"
}
```

### 4. Health Check

```http
GET /health
```

Response:
```json
{
  "status": "healthy",
  "timestamp": "2024-01-15T10:30:00Z",
  "version": "2.0.0.0",
  "uptime": {
    "totalSeconds": 3600,
    "formatted": "0d 1h 0m 0s"
  },
  "environment": "Production",
  "runtime": {
    "framework": ".NET 8.0.0",
    "osDescription": "Linux 5.15.0",
    "processArchitecture": "X64"
  },
  "memory": {
    "workingSetMB": 150.5,
    "gcTotalMemoryMB": 45.2
  }
}
```

## Docker Deployment

### Quick Start with Docker Compose

1. Copy the environment template:
   ```bash
   cp .env.template .env
   ```

2. Edit `.env` with your SharePoint credentials:
   ```
   SP_CLIENT_ID=your-azure-ad-app-client-id
   SP_TENANT_ID=your-azure-ad-tenant-id
   ```

3. Start the server:
   ```bash
   docker-compose up -d
   ```

### Build Docker Image

```bash
docker build -t roboclerk-server -f RoboClerk.Server/Dockerfile .
```

### Run Container

```bash
docker run -d \
  --name roboclerk-server \
  -p 8080:8080 \
  -e SP_CLIENT_ID=your-client-id \
  -e SP_TENANT_ID=your-tenant-id \
  roboclerk-server
```

For detailed Docker deployment instructions, see [DOCKER.md](DOCKER.md).

## Kubernetes Deployment

Kubernetes manifests are provided in the `kubernetes/` directory:

```bash
# Create namespace and secrets
kubectl create namespace roboclerk
kubectl create secret generic sharepoint-credentials \
  --from-literal=client-id=YOUR_CLIENT_ID \
  --from-literal=tenant-id=YOUR_TENANT_ID \
  -n roboclerk

# Deploy
kubectl apply -f RoboClerk.Server/kubernetes/deployment.yaml -n roboclerk
```

## Logging

The server uses NLog for structured logging. Logs are written to:
- Console (always, optimized for container environments)
- `logs/roboclerk-server-{date}.log` (file output)
- `logs/roboclerk-server-errors-{date}.log` (errors only)

Configure log level via:
- `RoboClerk.Server.toml`: `[Logging] ServerLogLevel = "INFO"`
- Environment variable: `ROBOCLERK_LOG_LEVEL=DEBUG`

## Troubleshooting

### Common Issues

1. **SharePoint Access Denied**
   - Verify `SP_CLIENT_ID` and `SP_TENANT_ID` are correct
   - Ensure Azure AD app has Sites.Read.All or Sites.ReadWrite.All permissions
   - Check if admin consent has been granted

2. **Project Not Found**
   - Verify the SharePoint path is accessible
   - Check that `RoboClerkConfig/projectConfig.toml` exists in the project root
   - Ensure the project path uses `sp://` prefix

3. **Content Generation Fails**
   - Verify data source plugins are configured correctly
   - Check that the content creator tag syntax is valid
   - Review logs for detailed error messages

4. **Container Health Check Failing**
   - Check container logs: `docker logs roboclerk-server`
   - Verify port 8080 is exposed correctly
   - Ensure environment variables are set

### Debug Mode

Enable detailed logging:
```bash
# Environment variable
ROBOCLERK_LOG_LEVEL=DEBUG dotnet run

# Or in configuration
[Logging]
ServerLogLevel = "DEBUG"
```

## Security Considerations

1. **Secrets Management**: Never commit `SP_CLIENT_ID` or `SP_TENANT_ID` to source control. Use environment variables or secrets management.

2. **CORS Configuration**: In production, restrict `AllowedOrigins` to specific domains instead of `*`.

3. **HTTPS**: Configure TLS at the load balancer or reverse proxy for production deployments.

4. **Non-root Container**: The Docker image runs as a non-root user (`roboclerk`, UID 1000) for security.

## Architecture

```
┌─────────────────┐     ┌──────────────────┐     ┌─────────────────┐
│   Word Add-in   │────▶│  RoboClerk.Server │────▶│   SharePoint    │
│   (Browser)     │◀────│   (ASP.NET Core)  │◀────│   (Files/Docs)  │
└─────────────────┘     └──────────────────┘     └─────────────────┘
                               │
                               ▼
                        ┌──────────────────┐
                        │   Data Sources   │
                        │ (Redmine, Azure  │
                        │  DevOps, etc.)   │
                        └──────────────────┘
```

## License

See the main RoboClerk repository for license information.