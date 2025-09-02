# RoboClerk Server

A web API server that exposes RoboClerk functionality for integration with Word add-ins and other applications.

## Features

- **Project Management**: Load and manage RoboClerk projects
- **Document Processing**: Extract and process RoboClerk tags from DOCX documents
- **Content Generation**: Generate content using existing RoboClerk content creators
- **Configuration Access**: Retrieve project configuration settings
- **Cross-platform**: Runs on Windows, Linux, and macOS

## Getting Started

### Prerequisites

- .NET 8.0 SDK
- RoboClerk project with DOCX templates

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
- HTTP: `http://localhost:5000`
- HTTPS: `https://localhost:5001`

### API Documentation

When running in development mode, Swagger documentation is available at the root URL (e.g., `http://localhost:5000`).

## API Endpoints

### Project Management

- `GET /api/project/available` - Get list of available RoboClerk projects
- `POST /api/project/load` - Load a specific project
- `GET /api/project/{projectId}/documents` - Get documents in a loaded project
- `GET /api/project/{projectId}/configuration` - Get project configuration
- `POST /api/project/{projectId}/refresh` - Refresh project data sources
- `DELETE /api/project/{projectId}` - Unload a project

### Document Operations

- `POST /api/project/{projectId}/documents/{documentId}/load` - Load a document and extract RoboClerk tags
- `POST /api/project/{projectId}/content` - Generate content for a specific RoboClerk tag

## Usage Example

### 1. Get Available Projects

```http
GET /api/project/available
```

Response:
```json
[
  {
    "name": "MyProject",
    "path": "C:\\Projects\\MyProject"
  }
]
```

### 2. Load a Project

```http
POST /api/project/load
Content-Type: application/json

{
  "projectPath": "C:\\Projects\\MyProject"
}
```

Response:
```json
{
  "success": true,
  "projectId": "12345-67890",
  "projectName": "MyProject",
  "documents": [
    {
      "documentId": "SRS",
      "title": "Software Requirements Specification",
      "template": "SRS.docx"
    }
  ]
}
```

### 3. Load a Document

```http
POST /api/project/12345-67890/documents/SRS/load
```

Response:
```json
{
  "success": true,
  "documentId": "SRS",
  "tags": [
    {
      "tagId": "tag-1",
      "source": "SLMS",
      "contentCreatorId": "Requirements",
      "parameters": {
        "ItemID": "REQ-001"
      },
      "currentContent": ""
    }
  ]
}
```

### 4. Generate Tag Content

```http
POST /api/project/12345-67890/content
Content-Type: application/json

{
  "documentId": "SRS",
  "source": "SLMS",
  "contentCreatorId": "Requirements",
  "parameters": {
    "ItemID": "REQ-001"
  }
}
```

Response:
```json
{
  "success": true,
  "content": "REQ-001: The system shall..."
}
```

## Configuration

The server uses the same configuration system as RoboClerk:

- `appsettings.json` - ASP.NET Core configuration
- `nlog.config` - Logging configuration
- Project-specific RoboClerk configuration files

## Word Add-in Integration

This server is designed to work with Word add-ins that need to:

1. **Select Projects**: Use `/api/project/available` to list projects
2. **Load Projects**: Use `/api/project/load` to initialize a project
3. **Process Documents**: Use document endpoints to extract and populate RoboClerk tags
4. **Generate Content**: Use content endpoints to generate tag content

The API includes CORS support for browser-based add-ins.

## Deployment

### Development
```bash
dotnet run
```

### Production
```bash
dotnet publish -c Release -o ./publish
cd publish
dotnet RoboClerk.Server.dll
```

### Docker (if needed)
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY publish/ .
RUN apt-get update && apt-get install -y libgdiplus
EXPOSE 80
ENTRYPOINT ["dotnet", "RoboClerk.Server.dll"]
```

## Logging

Logs are written to:
- Console (development)
- `logs/nlog-AspNetCore-all-{date}.log` (all logs)
- `logs/nlog-AspNetCore-own-{date}.log` (application logs only)

## Troubleshooting

### Common Issues

1. **Project not found**: Ensure the project path contains `RoboClerkConfig/RoboClerk.toml` and `RoboClerkConfig/projectConfig.toml`
2. **No DOCX documents**: Only DOCX templates are supported by the server
3. **Content creator errors**: Ensure all required data source plugins are available and configured
4. **Permission errors**: Check file system permissions for project directories

### Debug Mode

Run with debug logging:
```bash
dotnet run --environment Development
```

This enables detailed logging and Swagger UI for API testing.