<#
.SYNOPSIS
    Publishes a .NET application in two configurations and creates per-platform ZIPs.
.DESCRIPTION
    Publishes only:
    1. Self-contained, single file, trimmed
    2. Framework-dependent, single file
    Creates separate ZIP archives for each platform.
#>

param(
    [Parameter(Mandatory=$true)]
    [string]$ProjectPath,
    
    [Parameter(Mandatory=$true)]
    [string]$OutputDirectory,
    
    [string[]]$RuntimeIdentifiers = @(
        "win-x64",
        "win-x86",
        "win-arm64",
        "linux-x64",
        "linux-arm",
        "linux-arm64",
        "osx-x64",
        "osx-arm64"
    ),
    
    [string]$Configuration = "Release",
    
    [ValidateSet("Optimal", "Fastest", "NoCompression")]
    [string]$CompressionLevel = "Optimal"
)

# Validate project path
if (-not (Test-Path $ProjectPath)) {
    Write-Error "Project file not found at $ProjectPath"
    exit 1
}

# Create output directory if it doesn't exist
if (-not (Test-Path $OutputDirectory)) {
    New-Item -ItemType Directory -Path $OutputDirectory | Out-Null
}

# Get project name for output folders
$projectName = [System.IO.Path]::GetFileNameWithoutExtension($ProjectPath)
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$publishRoot = Join-Path $OutputDirectory "$projectName-$timestamp"

# Only these two configurations
$publishConfigurations = @(
    @{ 
        # Name = "framework-dependent-singlefile";
        Name = "";
        SelfContained = $false;
        SingleFile = $true;
        Trimmed = $false
    },
    @{ 
        Name = "selfcontained";
        SelfContained = $true;
        SingleFile = $true;
        Trimmed = $true
    }
)

# Publish for each RID and configuration
foreach ($rid in $RuntimeIdentifiers) {
    foreach ($config in $publishConfigurations) {
        $outputPath = Join-Path $publishRoot "$($config.Name)\$rid"
        
        Write-Host "Publishing for $rid in $($config.Name) configuration..." -ForegroundColor Cyan
        
        $publishParams = @(
            "publish",
            $ProjectPath,
            "-c", $Configuration,
            "-r", $rid,
            "-o", $outputPath,
            "--nologo",
            "--verbosity", "quiet"
        )
        
        if ($config.SelfContained) {
            $publishParams += "--self-contained"
        } else {
            $publishParams += "--no-self-contained"
        }
        
        if ($config.SingleFile) {
            $publishParams += "-p:PublishSingleFile=true"
        }
        
        if ($config.Trimmed) {
            $publishParams += "-p:PublishTrimmed=true"
        }
        
        # Execute dotnet publish
        $process = Start-Process -FilePath "dotnet" -ArgumentList $publishParams -NoNewWindow -Wait -PassThru
        
        if ($process.ExitCode -ne 0) {
            Write-Warning "Failed to publish for $rid in $($config.Name) configuration"
            continue
        }
        
        Write-Host "Successfully published to $outputPath" -ForegroundColor Green
        
        # Create individual ZIP for this RID and configuration
        # $zipName = "$projectName-$timestamp-$($config.Name)-$rid.zip"
        if ([string]::IsNullOrEmpty($($config.Name))) {
            $zipName = "uniqly-$rid.zip"
        }else {
            $zipName = "uniqly-$rid-$($config.Name).zip"
        }
        # $zipName = "uniqly-$($config.Name)-$rid.zip"
        $zipPath = Join-Path $OutputDirectory $zipName
        
        try {
            Compress-Archive -Path "$outputPath\*" -DestinationPath $zipPath -CompressionLevel $CompressionLevel
            Write-Host "Created archive: $zipName" -ForegroundColor Green
        }
        catch {
            Write-Warning "Failed to create archive for $rid ($($config.Name)): $_"
        }
    }
}

Write-Host "Publishing and compression complete!" -ForegroundColor Green
Write-Host "Individual platform archives created in: $OutputDirectory" -ForegroundColor Yellow