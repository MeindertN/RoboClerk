# Smart File Provider Plugin - Usage Guide

## Overview

The `SmartFileProviderPlugin` provides intelligent path routing to different file storage backends based on URI-style path prefixes. This eliminates the complexity of managing multiple file providers and makes configuration intent crystal clear.

## Key Design Principles

### 1. **No Prefix = Local File System** (Default)
Paths without a prefix are always routed to the local file system provider.

### 2. **Explicit Prefix = Specialized Provider**  
Each specialized provider (SharePoint, Azure Blob, S3, etc.) declares its own unique prefix.

### 3. **Provider Self-Declaration**
Providers declare their own prefix via the `GetPathPrefix()` method, ensuring maximum flexibility.

## Architecture

```
???????????????????????????????????????????
?     SmartFileProviderPlugin             ?
?  (Routes based on path prefix)          ?
???????????????????????????????????????????
             ?
             ??? No prefix ? LocalFileSystemPlugin (default)
             ??? sp://     ? SharePointFileProviderPlugin
             ??? azure://  ? AzureBlobProviderPlugin (future)
             ??? s3://     ? S3ProviderPlugin (future)
```

## Implementation Details

### 1. IFileProviderPlugin Interface Enhancement

```csharp
public interface IFileProviderPlugin : IPlugin
{
    /// <summary>
    /// Gets the URI-style prefix that identifies paths handled by this provider.
    /// Return null or empty string for default (local) providers.
    /// </summary>
    string GetPathPrefix();
    
    // ... existing methods ...
}
```

### 2. Provider Implementation Examples

**Local File System (Default Provider)**
```csharp
public class LocalFileSystemPlugin : FileProviderPluginBase
{
    public override string GetPathPrefix()
    {
        return null; // No prefix = default provider
    }
    
    // ... file operations ...
}
```

**SharePoint Provider**
```csharp
public class SharePointFileProviderPlugin : FileProviderPluginBase
{
    public override string GetPathPrefix()
    {
        return "sp://"; // SharePoint uses sp:// prefix
    }
    
    // ... SharePoint operations ...
}
```

### 3. Smart Provider Setup

```csharp
// Create the smart provider with local as default
var localFileProvider = new LocalFileSystemPlugin(fileSystem);
var smartProvider = new SmartFileProviderPlugin(localFileProvider);

// Register specialized providers
smartProvider.RegisterProvider(sharePointFileProvider);

// Register ONLY the smart provider in DI
services.AddSingleton<IFileProviderPlugin>(smartProvider);
```

### 4. Adding New Providers (Future)

Adding new providers is simple - just implement `GetPathPrefix()`:

```csharp
public class AzureBlobProviderPlugin : FileProviderPluginBase
{
    public override string GetPathPrefix()
    {
        return "azure://"; // Azure Blob Storage
    }
    
    // ... Azure operations ...
}

public class S3ProviderPlugin : FileProviderPluginBase
{
    public override string GetPathPrefix()
    {
        return "s3://"; // AWS S3
    }
    
    // ... S3 operations ...
}

// Registration:
smartProvider.RegisterProvider(azureBlobProvider);
smartProvider.RegisterProvider(s3Provider);
```

## Configuration Examples

### Windows Configuration

```toml
# projectConfig.toml

ProjectName = "MyProject"

# SharePoint paths - explicit prefix
TemplateDirectory = "sp://RoboClerk_input/"
OutputDirectory = "sp://RoboClerk_output"
MediaDirectory = "sp://RoboClerk_input/media"

# Local paths - no prefix (implicit)
ProjectRoot = "C:/SourceCode/MyProject"
```

### Linux Configuration

```toml
# projectConfig.toml

ProjectName = "MyProject"

# SharePoint paths - explicit prefix
TemplateDirectory = "sp://RoboClerk_input/"
OutputDirectory = "sp://RoboClerk_output"

# Local paths - no prefix (implicit)
ProjectRoot = "/home/roboclerk/source/MyProject"
TestDirectory = "/var/roboclerk/tests"
```

### Plugin Configuration

```toml
# UnitTestFNPlugin.toml

[[TestConfigurations]]
Project = "MyProject.Core"
Language = "csharp"
# Local path - no prefix
TestDirectory = "C:/SourceCode/MyProject/Tests"
SubDirs = true
FileMasks = ["*Tests.cs"]

[[TestConfigurations]]
Project = "ArchivedTests"
Language = "csharp"
# SharePoint path - explicit prefix
TestDirectory = "sp://ArchivedTests/2024"
SubDirs = true
FileMasks = ["*Tests.cs"]

UseGit = true
```

## Usage in Plugins

Plugins no longer need to know about file provider routing. Just use `fileProvider` and the smart provider routes automatically:

```csharp
public class UnitTestFNPlugin : SourceCodeAnalysisPluginBase
{
    public UnitTestFNPlugin(IFileProviderPlugin fileSystem)
        : base(fileSystem)
    {
        // ...
    }

    public override void RefreshItems()
    {
        foreach (var testConfig in TestConfigurations)
        {
            foreach (var sourceFile in testConfig.SourceFiles)
            {
                // Smart provider routes automatically based on path prefix!
                // - "C:/source/test.cs" ? Local
                // - "sp://tests/test.cs" ? SharePoint
                var text = fileProvider.ReadAllText(sourceFile);
                ProcessFile(text, sourceFile);
            }
        }
    }
}
```

## Path Routing Examples

| Path | Provider | Explanation |
|------|----------|-------------|
| `C:/source/test.cs` | Local | No prefix ? local |
| `/home/user/test.cs` | Local | No prefix ? local |
| `./relative/test.cs` | Local | No prefix ? local |
| `sp://RoboClerk/template.docx` | SharePoint | `sp://` prefix ? SharePoint |
| `azure://container/file.txt` | Azure Blob | `azure://` prefix ? Azure (future) |
| `s3://bucket/file.txt` | S3 | `s3://` prefix ? S3 (future) |

## Cross-Platform Compatibility

### Windows
```
? C:/source/test.cs           ? Local (drive letter)
? D:\source\test.cs            ? Local (backslash normalized)
? \\server\share\file.txt      ? Local (UNC path)
? sp://RoboClerk/file.docx     ? SharePoint
```

### Linux
```
? /home/user/source/test.cs   ? Local (no prefix)
? /var/roboclerk/test.cs      ? Local (no prefix)
? ~/source/test.cs             ? Local (no prefix)
? ./relative/test.cs           ? Local (no prefix)
? sp://RoboClerk/file.docx     ? SharePoint
```

### Docker/Containers
```
? /mnt/source/test.cs          ? Local (mounted volume)
? /app/config.toml             ? Local (container path)
? sp://RoboClerk/file.docx     ? SharePoint
```

## Benefits

### ? **Simplified Plugin Code**
- Plugins use one file provider
- No need to choose between providers
- No dual constructor complexity

### ? **Clear Configuration Intent**
```toml
# Immediately obvious which paths use which provider:
TemplateDirectory = "sp://RoboClerk_input/"     # ? SharePoint
ProjectRoot = "C:/SourceCode/MyProject"         # ? Local
```

### ? **Cross-Platform**
- Works on Windows, Linux, macOS
- No OS-specific path detection needed
- Consistent behavior everywhere

### ? **Extensible**
- Easy to add new providers (Azure, S3, Git, etc.)
- Providers self-declare their prefix
- No changes needed to existing code

### ? **Zero Ambiguity**
- Explicit prefixes make intent clear
- No heuristics or guessing
- Follows industry standards (URI schemes)

## Migration from Dual Provider Approach

### Before (Dual Provider - Complex)
```csharp
public class UnitTestFNPlugin : SourceCodeAnalysisPluginBase
{
    public UnitTestFNPlugin(
        IFileProviderPlugin configFileProvider,
        IFileProviderPlugin sourceCodeFileProvider)  // ? Two providers!
        : base(configFileProvider, sourceCodeFileProvider)
    {
        // ...
    }

    public override void RefreshItems()
    {
        // Which provider to use? sourceCodeFileProvider? configFileProvider?
        var text = sourceCodeFileProvider.ReadAllText(sourceFile);  // ? Confusing!
    }
}
```

### After (Smart Provider - Simple)
```csharp
public class UnitTestFNPlugin : SourceCodeAnalysisPluginBase
{
    public UnitTestFNPlugin(IFileProviderPlugin fileSystem)  // ? One provider!
        : base(fileSystem)
    {
        // ...
    }

    public override void RefreshItems()
    {
        // Just use fileProvider - it routes automatically!
        var text = fileProvider.ReadAllText(sourceFile);  // ? Clear!
    }
}
```

## Best Practices

### 1. Always Use Explicit Prefixes for Non-Local Paths
```toml
# ? GOOD - Clear intent
TemplateDirectory = "sp://RoboClerk_input/"

# ? BAD - Ambiguous (is this SharePoint or local /RoboClerk_input?)
TemplateDirectory = "/RoboClerk_input/"
```

### 2. Keep Prefixes Lowercase
```toml
# ? GOOD
TemplateDirectory = "sp://RoboClerk_input/"

# ?? WORKS (case-insensitive) but not recommended
TemplateDirectory = "SP://RoboClerk_input/"
```

### 3. Document Provider Prefixes
Update configuration documentation to show supported prefixes:
```toml
# Supported path prefixes:
#   sp://    - SharePoint Online
#   (none)   - Local file system
#   
# Future:
#   azure:// - Azure Blob Storage
#   s3://    - AWS S3
```

## Troubleshooting

### Provider Not Found
```
Error: A provider for prefix 'sp://' is already registered
```
**Solution**: Each prefix can only be registered once. Check your setup code.

### Path Not Routing Correctly
```csharp
// Check if path has the correct prefix
fileProvider.ReadAllText("sp://test.txt");      // ? Routes to SharePoint
fileProvider.ReadAllText("/sp://test.txt");     // ? Routes to local (leading /)
```

### Logging
The smart provider logs routing decisions:
```
[DEBUG] Routing 'sp://RoboClerk/file.docx' to SharePointFileProviderPlugin (stripped to: 'RoboClerk/file.docx')
[DEBUG] No prefix detected, routing 'C:/source/test.cs' to local file system
```

## Future Enhancements

### Planned Provider Support
- **Azure Blob Storage**: `azure://container/file.txt`
- **AWS S3**: `s3://bucket/file.txt`
- **Git Repositories**: `git://repo/branch/file.txt`
- **HTTP/HTTPS**: `https://example.com/file.txt`

### Potential Features
- **Path Aliases**: Define shortcuts in configuration
- **Provider Fallbacks**: Try multiple providers for redundancy
- **Caching Layer**: Cache frequently accessed files
- **Async Operations**: Full async/await support throughout

## Summary

The Smart File Provider Plugin provides:
- **Simplicity**: One provider to rule them all
- **Clarity**: Explicit prefixes make intent obvious
- **Flexibility**: Easy to add new providers
- **Cross-Platform**: Works everywhere
- **Zero Ambiguity**: No guessing or heuristics

This architectural pattern follows industry best practices (URI schemes) and provides a clean, maintainable solution for multi-backend file access in RoboClerk.
