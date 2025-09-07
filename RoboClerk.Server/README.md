# RoboClerk Server

A web API server that exposes RoboClerk functionality for integration with Word add-ins working with SharePoint documents.

## Features

- **SharePoint Project Management**: Load and manage RoboClerk projects from SharePoint
- **Content Control Processing**: Extract and process RoboClerk content controls from DOCX documents
- **OpenXML Content Generation**: Generate OpenXML content using RoboClerk content creators
- **Word Add-in Integration**: Specialized endpoints for Word add-in scenarios
- **Real-time Data Sources**: Refresh data sources for up-to-date content generation
- **Cross-platform**: Runs on Windows, Linux, and macOS

## Features

- **SharePoint Integration**: Designed specifically for SharePoint-hosted RoboClerk projects
- **Content Control Focus**: Works exclusively with Word content controls (RoboClerkDocxTag)
- **OpenXML Generation**: Returns raw OpenXML for direct insertion into Word documents
- **Session Management**: Supports multiple concurrent Word add-in sessions
- **Real-time Updates**: Refresh SharePoint data sources on demand

## Getting Started

### Prerequisites

- .NET 8.0 SDK
- RoboClerk project hosted on SharePoint with DOCX templates
- SharePoint file provider plugin configured
- Word add-in for document interaction

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

### Word Add-in Endpoints

- `POST /api/word-addin/project/load` - Load a SharePoint project with automatic validation
- `POST /api/word-addin/project/{projectId}/document/{documentId}/load` - Load document and discover content controls
- `GET /api/word-addin/project/{projectId}/document/{documentId}/analyze` - Analyze content controls and capabilities
- `POST /api/word-addin/project/{projectId}/content` - Generate OpenXML for specific content control
- `POST /api/word-addin/project/{projectId}/refresh` - Refresh SharePoint data sources
- `GET /api/word-addin/project/{projectId}/config` - Get project configuration (diagnostic)
- `DELETE /api/word-addin/project/{projectId}` - End session and cleanup resources
- `GET /api/word-addin/health` - Health check endpoint

## Usage Example

### 1. Load a SharePoint Project

```http
POST /api/word-addin/project/load
Content-Type: application/json

{
  "projectPath": "https://mycompany.sharepoint.com/sites/projects/MyRoboClerkProject"
}
```

Response:
```json
{
  "success": true,
  "projectId": "12345-67890",
  "projectName": "MyRoboClerkProject",
  "documents": [
    {
      "documentId": "SRS",
      "title": "Software Requirements Specification",
      "template": "SRS.docx"
    }
  ]
}
```

### 2. Load a Document

```http
POST /api/word-addin/project/12345-67890/document/SRS/load
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
      "contentControlId": "ctrl-123",
      "parameters": {
        "ItemID": "REQ-001"
      },
      "currentContent": ""
    }
  ]
}
```

### 3. Analyze Document Content Controls

```http
GET /api/word-addin/project/12345-67890/document/SRS/analyze
```

Response:
```json
{
  "success": true,
  "documentId": "SRS",
  "totalTagCount": 5,
  "supportedTagCount": 4,
  "availableTags": [
    {
      "tagId": "tag-1",
      "source": "SLMS",
      "contentCreatorId": "Requirements",
      "contentControlId": "ctrl-123",
      "parameters": {
        "ItemID": "REQ-001"
      },
      "isSupported": true,
      "contentPreview": "REQ-001: The system shall provide..."
    }
  ]
}
```

### 4. Generate Content for Content Control

```http
POST /api/word-addin/project/12345-67890/content
Content-Type: application/json

{
  "documentId": "SRS",
  "contentControlId": "ctrl-123"
}
```

Response:
```json
{
  "success": true,
  "content": "<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:t>REQ-001: The system shall provide user authentication...</w:t></w:r></w:p>"
}
```

### 5. Health Check

```http
GET /api/word-addin/health
```

Response:
```json
{
  "status": "healthy",
  "service": "RoboClerk Word Add-in API",
  "timestamp": "2024-01-15T10:30:00Z",
  "version": "2.0.0"
}
```

## Configuration

The server uses the same configuration system as RoboClerk:

- `appsettings.json` - ASP.NET Core configuration
- `nlog.config` - Logging configuration
- SharePoint project configuration files (`RoboClerk.toml`, `projectConfig.toml`)

### SharePoint Configuration

Ensure your SharePoint project has:
- `RoboClerkConfig/RoboClerk.toml` - Main RoboClerk configuration
- `RoboClerkConfig/projectConfig.toml` - Project-specific configuration
- `Templates/` - Directory containing DOCX templates with content controls
- Proper SharePoint permissions for the service account

## Word Add-in Integration

This server is specifically designed for Word add-ins that:

1. **Load SharePoint Projects**: Projects must be hosted on SharePoint
2. **Work with Content Controls**: Only processes RoboClerk content controls in DOCX files
3. **Handle OpenXML**: Receives raw OpenXML for insertion into Word documents
4. **Manage Sessions**: Each add-in session gets isolated project contexts

### Typical Workflow

1. Word add-in loads SharePoint project
2. User opens a Word document with RoboClerk content controls
3. Add-in analyzes document to discover available content controls
4. User triggers content generation for specific controls
5. Add-in receives OpenXML and inserts it into the document
6. Session cleanup when user closes document/add-in

## Multi-User Support

The server supports:
- ? **Multiple users with different projects** - Full isolation
- ? **Multiple users with same project, different documents** - Shared project context
- ?? **Multiple users with same document** - Potential race conditions on content controls

For high-concurrency scenarios, consider implementing session-based project isolation.

## Deployment

### Development
```bash
dotnet run --environment Development
```

### Production
```bash
dotnet publish -c Release -o ./publish
cd publish
dotnet RoboClerk.Server.dll
```

### Docker
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

Key log events:
- SharePoint project loading/validation
- Content control discovery and analysis
- OpenXML content generation
- Error conditions and troubleshooting info

## Troubleshooting

### Common Issues

1. **SharePoint Access Denied**: 
   - Verify SharePoint permissions for service account
   - Check SharePoint file provider plugin configuration
   - Ensure project URL is accessible

2. **Project Configuration Missing**: 
   - Verify `RoboClerkConfig/RoboClerk.toml` exists in SharePoint
   - Check `RoboClerkConfig/projectConfig.toml` is present
   - Validate TOML file syntax

3. **No Content Controls Found**: 
   - Only RoboClerk content controls (RoboClerkDocxTag) are supported
   - Verify Word document contains properly configured content controls
   - Check content control tags match expected format

4. **Content Creator Errors**: 
   - Ensure required data source plugins are installed
   - Verify data source configuration and connectivity
   - Check plugin directories and permissions

5. **OpenXML Generation Fails**:
   - Review content creator output format
   - Check for HTML/text content that needs conversion
   - Verify RoboClerkDocxTag.GeneratedOpenXml property

### Debug Mode

Enable detailed logging:
```bash
dotnet run --environment Development --verbosity detailed
```

This provides:
- Detailed SharePoint interaction logs
- Content control parsing information
- OpenXML generation details
- Performance timing information

### Health Monitoring

Use the health endpoint to monitor service status:
```bash
curl http://localhost:5000/api/word-addin/health
```

Monitor for:
- SharePoint connectivity issues
- Memory usage with multiple loaded projects
- Document cache growth
- Session cleanup effectiveness