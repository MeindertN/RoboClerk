# RoboClerk Server Docker Deployment

This document describes how to deploy the RoboClerk Server using Docker.

## Quick Start

### 1. Prerequisites

- Docker Engine 20.10+ or Docker Desktop
- Docker Compose v2+ (optional, for easier management)
- SharePoint App Registration with appropriate permissions

### 2. Configuration

1. Copy the environment template:
   ```bash
   cp .env.template .env
   ```

2. Edit `.env` and set your SharePoint credentials:
   ```
   SP_CLIENT_ID=your-azure-ad-app-client-id
   SP_TENANT_ID=your-azure-ad-tenant-id
   ```

### 3. Build and Run

Using Docker Compose (recommended):
```bash
docker-compose up -d
```

Or build and run manually:
```bash
# Build the image
docker build -t roboclerk-server -f RoboClerk.Server/Dockerfile .

# Run the container
docker run -d \
  --name roboclerk-server \
  -p 8080:8080 \
  -e SP_CLIENT_ID=your-client-id \
  -e SP_TENANT_ID=your-tenant-id \
  roboclerk-server
```

### 4. Verify

Check the health endpoint:
```bash
curl http://localhost:8080/health
```

## Configuration

### Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `SP_CLIENT_ID` | SharePoint App Client ID (required) | - |
| `SP_TENANT_ID` | Azure AD Tenant ID (required) | - |
| `ASPNETCORE_ENVIRONMENT` | Environment name | `Production` |
| `LOG_LEVEL` | Logging level | `Information` |
| `ROBOCLERK_HTTP_PORT` | HTTP port override | `8080` |
| `ROBOCLERK_HOST_ADDRESS` | Host binding address | `0.0.0.0` |
| `ROBOCLERK_LOG_LEVEL` | RoboClerk-specific log level | `INFO` |
| `ROBOCLERK_CORS_ORIGINS` | CORS allowed origins | `*` |

### Health Endpoints

| Endpoint | Purpose | Use Case |
|----------|---------|----------|
| `/health/live` | Liveness check | Kubernetes liveness probe |
| `/health/ready` | Readiness check | Kubernetes readiness probe |
| `/health/startup` | Startup check | Kubernetes startup probe |
| `/health` | Detailed health info | Monitoring dashboards |

### Volume Mounts

| Path | Purpose |
|------|---------|
| `/app/logs` | Application logs |
| `/app/config` | Configuration files (read-only) |
| `/app/plugins` | Custom plugins (read-only) |

## Production Deployment

### Using Docker Compose for Production

```bash
docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d
```

### Kubernetes Deployment

1. Create the namespace:
   ```bash
   kubectl create namespace roboclerk
   ```

2. Create the secrets:
   ```bash
   kubectl create secret generic sharepoint-credentials \
     --from-literal=client-id=YOUR_CLIENT_ID \
     --from-literal=tenant-id=YOUR_TENANT_ID \
     -n roboclerk
   ```

3. Apply the deployment:
   ```bash
   kubectl apply -f RoboClerk.Server/kubernetes/deployment.yaml -n roboclerk
   ```

## Security Considerations

1. **Never commit secrets** - Use environment variables or secrets management
2. **Restrict CORS origins** - In production, specify exact allowed domains
3. **Use HTTPS** - Configure TLS at the load balancer/reverse proxy
4. **Run as non-root** - The container runs as user `roboclerk` (UID 1000)
5. **Read-only root filesystem** - Enable if your setup supports it

## Monitoring

### Logs

View container logs:
```bash
docker logs roboclerk-server
```

Or with Docker Compose:
```bash
docker-compose logs -f roboclerk-server
```

### Metrics

The `/health` endpoint provides basic metrics including:
- Uptime
- Memory usage
- Runtime information

## Troubleshooting

### Container won't start

Check logs:
```bash
docker logs roboclerk-server
```

Common issues:
- Missing SharePoint credentials
- Invalid configuration file
- Port already in use

### Health check failing

1. Check if the application started:
   ```bash
   docker exec roboclerk-server wget -qO- http://localhost:8080/health/live
   ```

2. Check for startup errors in logs

### SharePoint connection issues

- Verify `SP_CLIENT_ID` and `SP_TENANT_ID` are correct
- Ensure the Azure AD app has required permissions
- Check if the tenant has consented to the app

## Building for Different Platforms

### ARM64 (Apple Silicon, ARM servers)
```bash
docker build --platform linux/arm64 -t roboclerk-server:arm64 -f RoboClerk.Server/Dockerfile .
```

### Multi-platform build
```bash
docker buildx build --platform linux/amd64,linux/arm64 -t roboclerk-server:latest -f RoboClerk.Server/Dockerfile .
```
