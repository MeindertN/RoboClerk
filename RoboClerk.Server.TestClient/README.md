# RoboClerk Server Test Client

A console application that simulates the Word add-in workflow for testing the RoboClerk Server with SharePoint projects.

## Purpose

This test client simulates the complete Word add-in workflow:

1. **Health Check** - Verify server connectivity
2. **Load SharePoint Project** - Initialize project with proper SharePoint parameters
3. **Refresh Project** - Discover and analyze content controls in documents
4. **Generate Content** - Generate OpenXML content for supported content controls
5. **Refresh Data Sources** - Test data source refresh functionality
6. **Get Configuration** - Retrieve diagnostic configuration information
7. **Cleanup** - End session and cleanup resources

## Prerequisites

- .NET 8.0 SDK
- RoboClerk Server running (default: http://localhost:5000)
- SharePoint project with RoboClerk configuration and DOCX templates
- Proper SharePoint permissions and authentication configured
- SharePoint Drive ID (obtained via Microsoft Graph API)

## Building

```bash
cd RoboClerk.Server.TestClient
dotnet build
```

## Usage

```bash
dotnet run <ProjectPath> <SPDriveId> <ProjectRoot> <DocumentName> <SPSiteUrl> [ServerUrl]
```

### Parameters

- **ProjectPath** - SharePoint project path (e.g., `/sites/project/Shared Documents/MyRoboClerkProject`)
- **SPDriveId** - SharePoint Drive ID (obtained from Microsoft Graph API)
- **ProjectRoot** - Project root directory name (e.g., `MyRoboClerkProject`)
- **DocumentName** - Name of the document to test (e.g., `SRS`)
- **SPSiteUrl** - SharePoint site URL (e.g., `https://company.sharepoint.com/sites/project`)
- **ServerUrl** - Optional RoboClerk Server URL (default: http://localhost:5000)

### Examples

#### Basic Usage
```bash
dotnet run "/sites/projects/Shared Documents/MyProject" "b!abc123..." "MyProject" "SRS" "https://mycompany.sharepoint.com/sites/projects"
```

#### With Custom Server URL
```bash
dotnet run "/sites/projects/Shared Documents/MyProject" "b!def456..." "MyProject" "Requirements" "https://contoso.sharepoint.com/sites/projects" "https://roboclerk-server.example.com"
```

#### Local Testing
```bash
dotnet run "/sites/engineering/Shared Documents/RoboClerkDemo" "b!xyz789..." "RoboClerkDemo" "SoftwareRequirements" "https://contoso.sharepoint.com/sites/engineering" "http://localhost:5000"
```

## Getting SharePoint Drive ID

The SharePoint Drive ID must be obtained through Microsoft Graph API:

### Method 1: Using Graph Explorer
1. Go to [Graph Explorer](https://developer.microsoft.com/graph/graph-explorer)
2. Sign in with appropriate permissions
3. GET `https://graph.microsoft.com/v1.0/sites/{hostname}:/sites/{site-name}`
4. GET `https://graph.microsoft.com/v1.0/sites/{site-id}/drives`
5. Find the drive with the correct name and copy its ID

### Method 2: Using PowerShell
```powershell
# Install Microsoft Graph PowerShell module
Install-Module Microsoft.Graph

# Connect to Graph
Connect-MgGraph -Scopes "Sites.Read.All"

# Get site
$site = Get-MgSite -SiteId "{hostname}:/sites/{site-name}"

# Get drives
Get-MgSiteDrive -SiteId $site.Id
```

## Helper Script

Use the included `run-example.ps1` script for easier testing:

1. Edit the script to update your SharePoint parameters
2. Run: `.\run-example.ps1`

## Expected Project Structure

Your SharePoint project should have:

```
MyRoboClerkProject/
??? RoboClerkConfig/
?   ??? RoboClerk.toml
?   ??? projectConfig.toml
??? Templates/
?   ??? SRS.docx
?   ??? Requirements.docx
?   ??? ...
??? Media/
    ??? ...
```

## Sample Output

```
=== RoboClerk Server Test Client ===
Project Path: /sites/projects/Shared Documents/MyProject
SharePoint Drive ID: b!abc123...
Project Root: MyProject
Document Name: SRS
SharePoint Site URL: https://mycompany.sharepoint.com/sites/projects
Server URL: http://localhost:5000

?? Starting Word Add-in Workflow Simulation
================================================

?? Step 1: Server Health Check
------------------------------
?? Checking server health...
? Server health check passed: healthy

?? Step 2: Load SharePoint Project
----------------------------------
?? Loading SharePoint project: /sites/projects/Shared Documents/MyProject
? Project loaded successfully: sp-a1b2c3d4 - MyProject
?? Found 3 documents

?? Step 3: Refresh Project and Analyze Content Controls
-----------------------------------------------------
?? Refreshing project: sp-a1b2c3d4
? Project refresh complete: 4/5 content controls supported
  ? SLMS:Requirements (Control: ctrl-123)
     Preview: REQ-001: The system shall provide...
  ? SLMS:TestCases (Control: ctrl-456)
     Preview: TC-001: Verify user authentication...
  ? SLMS:InvalidCreator (Control: ctrl-999)
     Error: Content creator not available

?? Step 4: Generate Content for Content Controls
-----------------------------------------------
?? Generating content 1/4 for control: ctrl-123
   Tag: SLMS:Requirements
? Content generated successfully for ctrl-123
?? [SIMULATED] Content inserted into Word document

?? Generating content 2/4 for control: ctrl-456
   Tag: SLMS:TestCases
? Content generated successfully for ctrl-456
?? [SIMULATED] Content inserted into Word document

?? Step 5: Test Data Source Refresh
----------------------------------
?? Refreshing project data sources...
? Data sources refreshed successfully
?? Re-generating content for first control to demonstrate refresh...
? Content re-generation after refresh successful

?? Step 6: Get Project Configuration (Diagnostic)
------------------------------------------------
?? Getting project configuration...
? Project configuration retrieved: 12 values
  ProjectType: SharePoint
  OutputDirectory: /output
  TemplateDirectory: /templates
  ...

??? Step 7: Cleanup and End Session
----------------------------------
??? Unloading project and cleaning up resources...
? Project unloaded successfully

?? WORKFLOW SUMMARY
==================
? Project loaded: MyProject
? Document loaded: Software Requirements Specification
?? Content controls found: 5
? Supported content controls: 4
?? Successful content generations: 4/4
??? Session cleanup: Success
?? Overall workflow result: SUCCESS

?? Word add-in workflow simulation completed successfully!
```

## Error Scenarios

The test client will handle and report various error conditions:

- Server connectivity issues
- SharePoint access problems
- Invalid Drive ID
- Project configuration errors
- Document not found
- Content creator failures
- Network timeouts

## Logging

The test client provides detailed logging with emojis for easy visual scanning:

- ?? Information/discovery operations
- ? Success indicators  
- ? Error indicators
- ?? Warning indicators
- ?? Processing operations
- ????????? Category icons

## Development

### Project Structure

```
RoboClerk.Server.TestClient/
??? Models/
?   ??? ApiModels.cs          # Request/response models
??? Services/
?   ??? IRoboClerkServerClient.cs    # Server client interface
?   ??? RoboClerkServerClient.cs     # HTTP client implementation
?   ??? IWordAddInSimulator.cs       # Simulator interface
?   ??? WordAddInSimulator.cs        # Workflow simulator
??? Program.cs                       # Entry point and DI setup
??? run-example.ps1                  # Helper script
??? RoboClerk.Server.TestClient.csproj
```

### Adding New Tests

To add new test scenarios:

1. Extend `IWordAddInSimulator` with new methods
2. Implement the test logic in `WordAddInSimulator`
3. Add command line options if needed
4. Update this README

## Troubleshooting

### Common Issues

1. **Server Not Running**
   ```
   ? Server health check failed - cannot continue
   ```
   Solution: Start the RoboClerk Server first

2. **Invalid Drive ID**
   ```
   ? Failed to load SharePoint project - cannot continue
   Error: Invalid drive ID or access denied
   ```
   Solution: Verify the Drive ID using Microsoft Graph API

3. **SharePoint Access Denied**
   ```
   ? Project load failed: SharePoint access denied
   ```
   Solution: Check SharePoint permissions and server authentication configuration

4. **Document Not Found**
   ```
   ? Document 'MyDoc' not found in project
   ```
   Solution: Check document name matches exactly (case-sensitive)

5. **No Supported Content Controls**
   ```
   ?? No supported content controls found
   ```
   Solution: Verify the Word document contains valid RoboClerk content controls and required content creators are available

6. **Project Configuration Missing**
   ```
   ? Failed to load project configuration
   ```
   Solution: Ensure the SharePoint folder contains `RoboClerkConfig/projectConfig.toml`

### Debug Mode

For development and troubleshooting, modify the logging level in `Program.cs`:

```csharp
.ConfigureLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole();
    logging.SetMinimumLevel(LogLevel.Debug); // Changed from Information
})
```

This will show detailed HTTP requests and responses for debugging API communication.