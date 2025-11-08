# Enhanced PowerShell script to run RoboClerk Server + Test Client in Visual Studio Debug Mode
# Update these values with your actual SharePoint information

param(
    [switch]$DebugMode,
    [switch]$SetupOnly,
    [int]$StartupDelay = 5000  # Default 5 second delay for debug mode
)

# SharePoint parameters (update these with your actual values)
$ProjectPath = "/sites/yoursite/Shared Documents/YourRoboClerkProject"
$SPDriveId = "b!YOUR_DRIVE_ID_HERE"  # Obtain from Microsoft Graph API
$ProjectRoot = "YourRoboClerkProject"
$DocumentName = "SRS"  # or whatever document you want to test
$SPSiteUrl = "https://yourcompany.sharepoint.com/sites/yoursite"
$ServerUrl = "http://localhost:5000"  # Optional, defaults to localhost:5000

# Solution and project paths
$SolutionPath = "..\RoboClerk.sln"
$ServerProjectPath = "..\RoboClerk.Server\RoboClerk.Server.csproj"
$TestClientProjectPath = "RoboClerk.Server.TestClient.csproj"

Write-Host "=== RoboClerk Debug Setup Script ===" -ForegroundColor Green
Write-Host "Project Path: $ProjectPath" -ForegroundColor Cyan
Write-Host "Drive ID: $SPDriveId" -ForegroundColor Cyan
Write-Host "Project Root: $ProjectRoot" -ForegroundColor Cyan
Write-Host "Document: $DocumentName" -ForegroundColor Cyan
Write-Host "Site URL: $SPSiteUrl" -ForegroundColor Cyan
Write-Host "Server URL: $ServerUrl" -ForegroundColor Cyan
Write-Host "Startup Delay: $StartupDelay ms" -ForegroundColor Cyan
Write-Host ""

# Function to check if Visual Studio is installed and get path
function Get-VisualStudioPath {
    # Check for VS 2022
    $vsPaths = @(
        "${env:ProgramFiles}\Microsoft Visual Studio\2022\Enterprise\Common7\IDE\devenv.exe",
        "${env:ProgramFiles}\Microsoft Visual Studio\2022\Professional\Common7\IDE\devenv.exe",
        "${env:ProgramFiles}\Microsoft Visual Studio\2022\Community\Common7\IDE\devenv.exe",
        "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2019\Enterprise\Common7\IDE\devenv.exe",
        "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2019\Professional\Common7\IDE\devenv.exe",
        "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2019\Community\Common7\IDE\devenv.exe"
    )
    
    foreach ($path in $vsPaths) {
        if (Test-Path $path) {
            return $path
        }
    }
    return $null
}

# Function to create launch settings for test client
function Set-TestClientLaunchSettings {
    $launchSettingsPath = "Properties\launchSettings.json"
    $launchSettingsDir = "Properties"
    
    if (-not (Test-Path $launchSettingsDir)) {
        New-Item -ItemType Directory -Path $launchSettingsDir -Force | Out-Null
    }
    
    $commandLineArgs = "`"$ProjectPath`" `"$SPDriveId`" `"$ProjectRoot`" `"$DocumentName`" `"$SPSiteUrl`" `"$ServerUrl`" `"$StartupDelay`""
    
    $launchSettings = @{
        profiles = @{
            "RoboClerk.Server.TestClient" = @{
                commandName = "Project"
                commandLineArgs = $commandLineArgs
                environmentVariables = @{
                    "DOTNET_ENVIRONMENT" = "Development"
                    "ASPNETCORE_LOGGING__LOGLEVEL__DEFAULT" = "Debug"
                }
            }
            "Debug with Server" = @{
                commandName = "Project"
                commandLineArgs = $commandLineArgs
                environmentVariables = @{
                    "DOTNET_ENVIRONMENT" = "Development"
                    "ASPNETCORE_LOGGING__LOGLEVEL__DEFAULT" = "Debug"
                }
            }
            "Quick Start (No Delay)" = @{
                commandName = "Project"
                commandLineArgs = "`"$ProjectPath`" `"$SPDriveId`" `"$ProjectRoot`" `"$DocumentName`" `"$SPSiteUrl`" `"$ServerUrl`" `"0`""
                environmentVariables = @{
                    "DOTNET_ENVIRONMENT" = "Development"
                }
            }
        }
    } | ConvertTo-Json -Depth 4
    
    Set-Content -Path $launchSettingsPath -Value $launchSettings -Encoding UTF8
    Write-Host "? Created launch settings: $launchSettingsPath" -ForegroundColor Green
    Write-Host "   - Default profile with ${StartupDelay}ms delay" -ForegroundColor White
    Write-Host "   - Quick Start profile with no delay" -ForegroundColor White
}

# Function to create solution user options file for multiple startup projects
function Set-MultipleStartupProjects {
    # Create .suo alternative - Visual Studio will read this
    $suoContent = @"
<?xml version="1.0" encoding="utf-8"?>
<StartupProject>
  <MultipleStartupProjects>
    <StartupProject>RoboClerk.Server</StartupProject>
    <StartupProject>RoboClerk.Server.TestClient</StartupProject>
  </MultipleStartupProjects>
</StartupProject>
"@
    
    # Note: Visual Studio doesn't easily accept external .suo modifications
    # Instead, we'll provide instructions and try to open with specific parameters
    Write-Host "?? Multiple startup projects need to be configured manually in Visual Studio" -ForegroundColor Yellow
    Write-Host "   1. Right-click solution ? 'Set Startup Projects...'" -ForegroundColor White
    Write-Host "   2. Select 'Multiple startup projects'" -ForegroundColor White
    Write-Host "   3. Set both RoboClerk.Server and RoboClerk.Server.TestClient to 'Start'" -ForegroundColor White
    Write-Host "   4. Ensure RoboClerk.Server is listed FIRST" -ForegroundColor White
}

# Build projects
function Build-Projects {
    Write-Host "Building projects in Debug configuration..." -ForegroundColor Yellow
    
    # Build server project
    Write-Host "Building RoboClerk.Server..." -ForegroundColor Cyan
    dotnet build $ServerProjectPath -c Debug --nologo
    if ($LASTEXITCODE -ne 0) {
        Write-Host "? Server build failed" -ForegroundColor Red
        exit 1
    }
    Write-Host "? Server build successful" -ForegroundColor Green
    
    # Build test client project
    Write-Host "Building RoboClerk.Server.TestClient..." -ForegroundColor Cyan
    dotnet build $TestClientProjectPath -c Debug --nologo
    if ($LASTEXITCODE -ne 0) {
        Write-Host "? Test Client build failed" -ForegroundColor Red
        exit 1
    }
    Write-Host "? Test Client build successful" -ForegroundColor Green
}

# Main execution
try {
    # Validate parameters
    if ($SPDriveId -eq "b!YOUR_DRIVE_ID_HERE") {
        Write-Host "?? WARNING: You need to update the SPDriveId with your actual SharePoint Drive ID" -ForegroundColor Yellow
        Write-Host "   Get it from Microsoft Graph API: https://graph.microsoft.com/v1.0/sites/{site-id}/drives" -ForegroundColor White
        if (-not $SetupOnly) {
            $continue = Read-Host "Continue anyway? (y/N)"
            if ($continue -ne "y" -and $continue -ne "Y") {
                exit 0
            }
        }
    }
    
    # Build projects
    Build-Projects
    
    # Set up launch settings
    Set-TestClientLaunchSettings
    
    if ($DebugMode -or $SetupOnly) {
        # Find Visual Studio
        $vsPath = Get-VisualStudioPath
        if (-not $vsPath) {
            Write-Host "? Visual Studio not found. Please install Visual Studio 2019 or 2022." -ForegroundColor Red
            exit 1
        }
        Write-Host "? Found Visual Studio: $vsPath" -ForegroundColor Green
        
        # Configure multiple startup projects info
        Set-MultipleStartupProjects
        
        if (-not $SetupOnly) {
            # Launch Visual Studio with the solution
            Write-Host ""
            Write-Host "?? Launching Visual Studio in Debug Mode..." -ForegroundColor Green
            Write-Host "   Opening solution: $SolutionPath" -ForegroundColor Cyan
            
            # Start Visual Studio with the solution
            Start-Process -FilePath $vsPath -ArgumentList "`"$SolutionPath`"" -NoNewWindow
            
            Write-Host ""
            Write-Host "?? VISUAL STUDIO DEBUG SETUP COMPLETE!" -ForegroundColor Green
            Write-Host "========================================" -ForegroundColor Green
            Write-Host "Next steps in Visual Studio:" -ForegroundColor White
            Write-Host "1. Wait for solution to load completely" -ForegroundColor Yellow
            Write-Host "2. Right-click solution ? 'Set Startup Projects...'" -ForegroundColor Yellow
            Write-Host "3. Select 'Multiple startup projects'" -ForegroundColor Yellow
            Write-Host "4. Set both projects to 'Start' (Server first, then TestClient)" -ForegroundColor Yellow
            Write-Host "5. Press F5 to start debugging both projects!" -ForegroundColor Yellow
            Write-Host ""
            Write-Host "Launch profiles configured:" -ForegroundColor Green
            Write-Host "• Default: ${StartupDelay}ms startup delay ?" -ForegroundColor White
            Write-Host "• Quick Start: No startup delay ?" -ForegroundColor White
        } else {
            Write-Host ""
            Write-Host "?? SETUP COMPLETE - Manual steps required:" -ForegroundColor Green
            Write-Host "1. Open Visual Studio and load the solution" -ForegroundColor White
            Write-Host "2. Configure multiple startup projects as shown above" -ForegroundColor White
            Write-Host "3. Command line arguments are already set for TestClient with ${StartupDelay}ms delay" -ForegroundColor White
        }
    } else {
        # Run normally (non-debug mode)
        Write-Host "Running in normal mode (not debug)..." -ForegroundColor Yellow
        Write-Host "Use -DebugMode to set up Visual Studio debugging" -ForegroundColor Cyan
        Write-Host "Use -StartupDelay to customize the delay (current: ${StartupDelay}ms)" -ForegroundColor Cyan
        Write-Host ""
        
        # Run the test client normally
        dotnet run -- `
            "$ProjectPath" `
            "$SPDriveId" `
            "$ProjectRoot" `
            "$DocumentName" `
            "$SPSiteUrl" `
            "$ServerUrl" `
            "$StartupDelay"
    }
} catch {
    Write-Host "? Script failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}