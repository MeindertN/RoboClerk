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
   SP_CLIENT_SECRET=your-azure-ad-app-client-secret
   ```

### 3. Build and Run

**HTTP only (development):**
```bash
docker-compose up -d
```

**With HTTPS (production) - see [HTTPS Configuration](#https-configuration) below:**
```bash
# Edit Caddyfile with your domain first!
docker-compose -f docker-compose.https.yml up -d
```

**Or build and run manually:**
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

## HTTPS Configuration

### Option A: Public Domain with Let's Encrypt (Internet-facing)

For servers with a public domain name, use automatic Let's Encrypt certificates.

1. **Set up your domain**: Ensure your domain's DNS points to your server's public IP address.

2. **Configure credentials**:
   ```bash
   cp .env.template .env
   # Edit .env with your SP_CLIENT_ID and SP_TENANT_ID
   ```

3. **Configure your domain** in the Caddyfile:
   ```bash
   # Edit Caddyfile and replace 'roboclerk.yourdomain.com' with your actual domain
   nano Caddyfile
   ```

4. **Start the services**:
   ```bash
   docker-compose -f docker-compose.https.yml up -d
   ```

5. **Verify HTTPS**:
   ```bash
   curl https://yourdomain.com/health
   ```

### Option B: Corporate/Internal Network (No Public Domain)

For internal corporate networks without a public domain name, use self-signed certificates.

1. **Configure credentials**:
   ```bash
   cp .env.template .env
   # Edit .env with your SP_CLIENT_ID and SP_TENANT_ID
   ```

2. **Start the services**:
   ```bash
   docker-compose -f docker-compose.internal.yml up -d
   ```

3. **Access the server**:
   - By IP address: `https://192.168.1.100` (replace with your server's IP)
   - By hostname: `https://roboclerk-server` (requires DNS or hosts file entry)
   - Locally: `https://localhost`

4. **Handle certificate warnings**:
   - Accept the self-signed certificate warning in your browser, OR
   - Trust the Caddy root CA (see below)

#### Trusting the Caddy Root CA

To avoid certificate warnings for all clients, trust Caddy's internal CA:

**1. Extract the root CA from the container:**
```bash
docker cp roboclerk-caddy:/data/caddy/pki/authorities/local/root.crt ./caddy-root.crt
```

**2. Import on Windows:**
- Double-click `caddy-root.crt`
- Click "Install Certificate"
- Choose "Local Machine" → "Trusted Root Certification Authorities"

**3. Import on macOS:**
```bash
sudo security add-trusted-cert -d -r trustRoot -k /Library/Keychains/System.keychain caddy-root.crt
```

**4. Import on Linux (Ubuntu/Debian):**
```bash
sudo cp caddy-root.crt /usr/local/share/ca-certificates/
sudo update-ca-certificates
```

**5. For Word Add-in:**
The certificate must be trusted at the OS level (steps above). After trusting the CA, restart Office applications.

#### Using a Specific IP Address or Hostname

Edit `Caddyfile.internal` to bind to a specific IP or hostname:

```
# For a specific IP address
192.168.1.100 {
    tls internal
    reverse_proxy roboclerk-server:8080
}

# For a specific hostname (add to DNS or hosts file)
roboclerk.internal, roboclerk-server {
    tls internal
    reverse_proxy roboclerk-server:8080
}
```

### Option C: HTTP Only (Not Recommended)

For fully trusted internal networks where encryption is not required:

```bash
# Use the basic docker-compose without HTTPS
docker-compose up -d
```

Access at `http://localhost:8080` or `http://<server-ip>:8080`

⚠️ **Warning**: HTTP traffic is unencrypted. Only use on networks you fully control and trust.

### Files Reference

| File | Purpose |
|------|---------|
| `docker-compose.https.yml` | Public domain with Let's Encrypt |
| `docker-compose.internal.yml` | Internal network with self-signed certs |
| `docker-compose.yml` | HTTP only (development) |
| `Caddyfile` | Production config (edit domain!) |
| `Caddyfile.internal` | Internal network config |
| `Caddyfile.dev` | Local development config |

### Local Development with HTTPS

For local development with self-signed certificates:

```bash
# Use the development Caddyfile
cp Caddyfile.dev Caddyfile

# Start services
docker-compose -f docker-compose.https.yml up -d

# Access at https://localhost (accept the certificate warning)
```

### How It Works

1. **Caddy** listens on ports 80 and 443
2. **HTTP requests** are automatically redirected to HTTPS
3. **Certificates** are automatically generated (Let's Encrypt or self-signed)
4. **Requests** are proxied to the RoboClerk Server container on port 8080

### Troubleshooting HTTPS

**Certificate not obtained (Let's Encrypt):**
- Ensure ports 80 and 443 are open and accessible from the internet
- Verify DNS is correctly pointing to your server
- Check Caddy logs: `docker logs roboclerk-caddy`

**Self-signed certificate not working:**
- Ensure you're using `tls internal` in the Caddyfile
- Check that `caddy-data` volume persists between restarts
- Verify the Caddyfile is mounted correctly

**Word Add-in not trusting certificate:**
- The root CA must be trusted at the Windows OS level
- Import to "Trusted Root Certification Authorities" for Local Machine (not Current User)
- Restart Office applications after importing

**Certificate renewal:**
- Let's Encrypt: Automatic, no action needed
- Self-signed: Persistent via caddy-data volume

## Production Deployment

### Using Docker Compose for Production (with HTTPS)

```bash
# Recommended: Use HTTPS configuration
docker-compose -f docker-compose.https.yml up -d
```

### Using Docker Compose for Production (HTTP only, behind load balancer)

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
