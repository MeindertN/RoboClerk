# SharePoint File Provider Plugin

This plugin provides access to SharePoint Online document libraries using Microsoft Graph SDK with OAuth2 authentication.

## Features

- **Microsoft Graph SDK Integration**: Uses the official Microsoft Graph SDK for .NET
- **OAuth2 Authentication**: Automatic token management using Azure Identity
- **Strongly-typed Operations**: No more manual JSON parsing
- **Automatic Retry Logic**: Built-in retry mechanisms for network issues
- **Full File System Operations**: Read, write, delete, copy, move, etc.
- **Dynamic Drive ID Configuration**: Set Drive ID at runtime
- **Comprehensive Error Handling**: Proper exception handling and logging

## Configuration

The plugin requires the following configuration in `SharePointFileProvider.toml`:

```toml
# The SharePoint site URL (required)
SiteUrl = "your-sharepoint-site.sharepoint.com"

# Azure AD OAuth2 Client ID (required)
ClientId = "your-azure-ad-client-id"

# Azure AD OAuth2 Client Secret (required)
ClientSecret = "your-azure-ad-client-secret"

# Azure AD Tenant ID (required)
TenantId = "your-azure-ad-tenant-id"

# Drive ID (required)
DriveId = "your-sharepoint-drive-id"
```

## Azure AD App Registration

To use this plugin, you need to register an application in Azure AD:

1. Go to Azure Portal > Azure Active Directory > App registrations
2. Create a new registration
3. Note the Application (client) ID and Directory (tenant) ID
4. Create a client secret in Certificates & secrets
5. Grant the application the following Microsoft Graph API permissions:
   - `Sites.Read.All`
   - `Files.ReadWrite.All`

## Dependencies

- **Microsoft.Graph** (v5.44.0): Official Microsoft Graph SDK
- **Azure.Identity** (v1.10.4): Azure authentication library
- **Tomlyn**: Configuration parsing

## Key Improvements

### Before (Manual REST API)
```csharp
// Manual HTTP requests
var request = _client.CreateRequest(url, Method.Get);
var response = await _client.ExecuteAsync<dynamic>(request);
var json = JsonDocument.Parse(response.Content);
var item = json.RootElement.GetProperty("name").GetString();
```

### After (Microsoft Graph SDK)
```csharp
// Strongly-typed operations
var item = await graphClient.Drives[driveId].Root.ItemWithPath(path).GetAsync();
var content = await graphClient.Drives[driveId].Root.ItemWithPath(path).Content.GetAsync();
```

## Benefits

1. **Type Safety**: No more runtime errors from JSON parsing
2. **IntelliSense**: Full IDE support with autocomplete
3. **Automatic Authentication**: Token refresh handled automatically
4. **Better Error Handling**: Specific exception types for different errors
5. **Future-Proof**: Uses Microsoft's official SDK
6. **Reduced Code**: ~60% less code compared to manual REST implementation

## Usage Examples

### Basic File Operations
```csharp
var plugin = new SharePointFileProviderPlugin();

// Read file
string content = plugin.ReadAllText("/path/to/file.txt");

// Write file
plugin.WriteAllText("/path/to/file.txt", "Hello World");

// Check if file exists
bool exists = plugin.FileExists("/path/to/file.txt");
```

### Dynamic Drive ID
```csharp
// Set Drive ID at runtime
plugin.SetDriveId("new-drive-id");

// Get current Drive ID
string currentDriveId = plugin.GetDriveId();
```

## Error Handling

The plugin provides comprehensive error handling:

- **ServiceException**: For Graph API errors
- **FileNotFoundException**: When files don't exist
- **UnauthorizedAccessException**: For permission issues
- **NetworkException**: For connectivity issues

## Logging

The plugin uses NLog for detailed logging:

- Authentication status
- File operations
- Error conditions
- Performance metrics

## Security Notes

- Uses OAuth2 client credentials flow (server-to-server)
- No user interaction required
- Tokens are automatically refreshed
- Secure credential handling through Azure.Identity 