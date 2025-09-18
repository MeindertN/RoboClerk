# RoboClerk Project Copilot Instructions

## Project Overview
RoboClerk is a documentation generation tool built with .NET 8 that uses a plugin architecture to extract information from various sources (requirements management systems, source code, test results) and generate technical documentation in formats like AsciiDoc.

## Coding Standards

### Unit Testing Requirements
- **All unit tests MUST include the `UnitTestAttribute`** with these required properties:
  - `Identifier`: A unique GUID for the test (use PowerShell `New-Guid` command to generate)
  - `Purpose`: Clear description of what the test validates
  - `PostCondition`: Expected outcome or behavior after test execution
- Use NUnit framework with `[Test]` attribute
- Test class names should start with "Test" prefix (e.g., `TestSourceCodeAnalysisPluginBase`)
- Use NSubstitute for mocking dependencies
- Use `System.IO.Abstractions.TestingHelpers.MockFileSystem` for file system testing
- Cross-platform file path handling with `TestingHelpers.ConvertFileName()`

**Unit Test Template:**
```csharp
[UnitTestAttribute(
    Identifier = "XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX", // Generate with PowerShell New-Guid
    Purpose = "Clear description of what this test validates",
    PostCondition = "Expected outcome after test execution")]
[Test]
public void TestMethodName()
{
    // Test implementation
}
```

### GUID Generation Policy
**IMPORTANT**: When generating GUIDs for unit tests or any other purpose:
- **NEVER generate GUIDs directly in code**
- **ALWAYS use PowerShell command**: `New-Guid`

### Plugin Development
- Plugin classes should inherit from appropriate base classes:
  - `SourceCodeAnalysisPluginBase` for source code analysis plugins
  - `SLMSPluginBase` for requirement management system plugins  
  - `DataSourcePluginBase` for general data source plugins
- Plugin names should end with "Plugin" suffix (e.g., `AnnotatedUnitTestsPlugin`)
- All plugins must implement `InitializePlugin(IConfiguration configuration)` method
- Use dependency injection - plugins receive `IFileSystem` in constructor
- Plugin configuration files should be named `{PluginName}.toml`

### Interface and Class Naming
- Interfaces must start with "I" prefix (e.g., `IConfiguration`, `IPluginLoader`)
- Plugin classes end with "Plugin" (e.g., `AnnotatedUnitTestsPlugin`) 
- Test classes start with "Test" (e.g., `TestAnnotatedUnitTestPlugin`)
- Content creator classes end with "ContentCreator" (e.g., `SOUPContentCreator`)

### Configuration Management
- Use TOML format for all configuration files
- Plugin configurations go in dedicated directories
- Support command-line overrides using `configuration.CommandLineOptionOrDefault(key, defaultValue)`
- Use Tomlyn library for TOML parsing
- Configuration classes should implement dependency injection patterns

### File System Operations
- **Always use `System.IO.Abstractions.IFileSystem`** for file operations to enable testing
- Never use `System.IO` directly - use the abstraction
- For tests, use `MockFileSystem` with `TestingHelpers.ConvertFileName()` for cross-platform paths
- Handle both Windows and Unix-style paths appropriately

### Error Handling and Logging
- Use NLog for structured logging, in many cases the base class provides a logger
- Log levels: Debug, Info, Warn, Error, Fatal
- Always log plugin initialization and major operations
- Handle exceptions gracefully, especially in plugin loading
- Provide meaningful error messages that help with troubleshooting
- Use try-catch blocks around plugin operations

### Cross-Platform Support
- Use `RuntimeInformation.IsOSPlatform(OSPlatform.Windows)` for platform-specific code
- Handle path separators correctly with `Path.DirectorySeparatorChar`
- Use `TestingHelpers.ConvertFileName()` in tests for path conversion
- Test both Windows and Unix path formats in configuration
- Assume that the code will run and build on Windows and Linux

### Dependency Injection Patterns
- Constructor injection for required dependencies
- Use `IServiceCollection` for service registration
- Plugin loading should be done through `IPluginLoader`
- Prefer interfaces over concrete classes in constructors

## Project Structure

### Core Projects
- **RoboClerk**: Main application and core functionality
- **RoboClerk.Tests**: Comprehensive unit test suite
- **Plugin Projects**: Individual projects for each plugin (e.g., `RoboClerk.AnnotatedUnitTests`)

### File Organization
- Plugin configurations in `Configuration/` subdirectories
- Template files in appropriate template directories
- Test files mirror the structure of source files
- Use consistent naming conventions across projects

## Technology Stack

### Target Framework
- .NET 8.0 for all projects
- Use latest C# language features appropriately

### Key Dependencies
- **Testing**: NUnit 4.x, NSubstitute 5.x, System.IO.Abstractions.TestingHelpers
- **Configuration**: Tomlyn for TOML parsing
- **Logging**: NLog for structured logging
- **Source Analysis**: TreeSitter.DotNet for code parsing
- **Plugin Architecture**: Dynamic loading with `EnableDynamicLoading`

## Plugin Development Guidelines

### Configuration Files
- Use TOML format exclusively
- Group related settings logically
- Provide sensible defaults
- Document configuration options with comments

### Integration Testing
- Test plugin loading and initialization
- Verify configuration parsing
- Test data retrieval and processing
- Validate output generation

## Common Anti-Patterns to Avoid
- Don't use `System.IO` directly - always use `IFileSystem`
- Don't generate GUIDs in code - use PowerShell `New-Guid`
- Don't hardcode file paths - use configuration
- Don't ignore cross-platform compatibility
- Don't skip unit test attributes with proper GUIDs
- Don't forget to call base class methods in plugin implementations

## When Generating Code, Prefer:
1. Explicit dependency injection patterns
2. Comprehensive error handling with meaningful messages
3. Cross-platform compatible file operations
4. Testable designs with interface abstractions
5. Proper logging at appropriate levels
6. Configuration-driven behavior over hard-coded values
7. Consistent naming conventions throughout
8. Proper disposal patterns for resources