# RoboClerk Server Test Client

A console application that simulates the Word add-in workflow for testing the RoboClerk Server with SharePoint projects.

## Purpose

This test client simulates the complete Word add-in workflow:

1. **Health Check** - Verify server connectivity
2. **Load SharePoint Project** - Initialize project with automatic validation
3. **Load Document** - Load document from SharePoint and discover content controls
4. **Analyze Document** - Analyze content controls and their capabilities
5. **Generate Content** - Generate OpenXML content for supported content controls
6. **Refresh Data Sources** - Test data source refresh functionality
7. **Get Configuration** - Retrieve diagnostic configuration information
8. **Cleanup** - End session and cleanup resources

## Prerequisites

- .NET 8.0 SDK
- RoboClerk Server running (default: http://localhost:5000)
- SharePoint project with RoboClerk configuration and DOCX templates
- Proper SharePoint permissions configured

## Building

```bash
cd RoboClerk.Server.TestClient
dotnet build
```

## Usage

```bash
dotnet run <SharePointProjectUrl> <DocumentName> [ServerUrl]
```

### Parameters

- **SharePointProjectUrl** - URL to SharePoint project (required)
- **DocumentName** - Name of the document to test (required)  
- **ServerUrl** - RoboClerk Server URL (optional, default: http://localhost:5000)

### Examples

#### Basic Usage
```bash
dotnet run "https://mycompany.sharepoint.com/sites/projects/MyRoboClerkProject" "SRS"
```

#### With Custom Server URL
```bash
dotnet run "https://mycompany.sharepoint.com/sites/projects/MyProject" "Requirements" "https://roboclerk-server.example.com"
```

#### Local Testing
```bash
dotnet run "https://contoso.sharepoint.com/sites/engineering/RoboClerkDemo" "SoftwareRequirements" "http://localhost:5000"
```

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
SharePoint Project: https://mycompany.sharepoint.com/sites/projects/MyProject
Document Name: SRS
Server URL: http://localhost:5000

?? Starting Word Add-in Workflow Simulation
================================================

?? Step 1: Server Health Check
------------------------------
?? Checking server health...
? Server health check passed: healthy

?? Step 2: Load SharePoint Project
----------------------------------
?? Loading SharePoint project: https://mycompany.sharepoint.com/sites/projects/MyProject
? Project loaded successfully: 12345-67890 - MyProject
?? Found 3 documents

?? Step 3: Load Document from SharePoint
----------------------------------------
?? Loading document: SRS
? Document loaded successfully: 5 content controls found
  ??? SLMS:Requirements (Control: ctrl-123)
  ??? SLMS:TestCases (Control: ctrl-456)
  ??? SLMS:RiskAnalysis (Control: ctrl-789)

?? Step 4: Analyze Document Content Controls
--------------------------------------------
?? Analyzing document: SRS
? Document analysis complete: 4/5 content controls supported
  ? SLMS:Requirements (Control: ctrl-123)
     Preview: REQ-001: The system shall provide...
  ? SLMS:TestCases (Control: ctrl-456)
     Preview: TC-001: Verify user authentication...
  ? SLMS:InvalidCreator (Control: ctrl-999)
     Error: Content creator not available: Creator not found

?? Step 5: Generate Content for Content Controls
-----------------------------------------------
?? Generating content 1/4 for control: ctrl-123
?? Generating content for control: ctrl-123
? Content generated successfully: 1,234 characters of OpenXML
?? [SIMULATED] Content inserted into Word document

?? Generating content 2/4 for control: ctrl-456
?? Generating content for control: ctrl-456
? Content generated successfully: 856 characters of OpenXML
?? [SIMULATED] Content inserted into Word document

?? Step 6: Test Data Source Refresh
----------------------------------
?? Refreshing project data sources...
? Project data sources refreshed successfully
?? Re-generating content for first control to demonstrate refresh...
? Content re-generation after refresh successful

?? Step 7: Get Project Configuration (Diagnostic)
------------------------------------------------
?? Getting project configuration...
? Project configuration retrieved: 12 values
  ProjectType: SharePoint
  OutputDirectory: /output
  TemplateDirectory: /templates
  ...

?? Step 8: Cleanup and End Session
----------------------------------
?? Unloading project and cleaning up resources...
? Project unloaded successfully

?? WORKFLOW SUMMARY
==================
? Project loaded: MyProject
? Document loaded: Software Requirements Specification
? Content controls found: 5
? Supported content controls: 4
? Successful content generations: 4/4
? Session cleanup: Success
?? Overall workflow result: SUCCESS

? Word add-in workflow simulation completed successfully!
```

## Error Scenarios

The test client will handle and report various error conditions:

- Server connectivity issues
- SharePoint access problems
- Project configuration errors
- Document not found
- Content creator failures
- Network timeouts

## Logging

The test client provides detailed logging with emojis for easy visual scanning:

- ?? Information/status messages
- ? Success indicators  
- ? Error indicators
- ?? Warning indicators
- ?? Progress indicators
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
??? RoboClerk.Server.TestClient.csproj
```

### Adding New Tests

To add new test scenarios:

1. Extend `IWordAddInSimulator` with new methods
2. Implement the test logic in `WordAddInSimulator`
3. Add command line options if needed
4. Update this README

### Debugging

Run with detailed logging:
```bash
dotnet run --verbosity detailed <SharePointProjectUrl> <DocumentName>
```

## Troubleshooting

### Common Issues

1. **Server Not Running**
   ```
   ? Server health check failed - cannot continue
   ```
   Solution: Start the RoboClerk Server first

2. **SharePoint Access Denied**
   ```
   ? Project load failed: SharePoint access denied
   ```
   Solution: Check SharePoint permissions and authentication

3. **Document Not Found**
   ```
   ? Document 'MyDoc' not found in project
   ```
   Solution: Check document name matches exactly (case-sensitive)

4. **No Content Controls**
   ```
   ?? No content controls found in document
   ```
   Solution: Verify the Word document contains RoboClerk content controls

### Debug Mode

For development and troubleshooting, you can modify the logging level in `Program.cs` to see more detailed HTTP requests and responses.