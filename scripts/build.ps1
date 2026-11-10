# ==============================================================================
# BatteryAlert Build Script
# Compiles a standalone, single-file Windows executable with zero runtime dependencies.
# ==============================================================================

[CmdletBinding()]
param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptDir
$srcDir = Join-Path $projectRoot "src"
$distDir = Join-Path $projectRoot "dist"
$assetsDir = Join-Path $projectRoot "assets"
$icoPath = Join-Path $assetsDir "app.ico"
$outputExe = Join-Path $distDir "BatteryAlert.exe"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " Building BatteryAlert ($Configuration)" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Ensure assets/app.ico exists
if (-not (Test-Path $icoPath)) {
    Write-Host "[1/3] Generating application icon..." -ForegroundColor Yellow
    $genScript = Join-Path $scriptDir "generate-icon.ps1"
    & powershell -ExecutionPolicy Bypass -File $genScript
} else {
    Write-Host "[1/3] Application icon found: $icoPath" -ForegroundColor Green
}

# 2. Ensure dist directory exists
if (-not (Test-Path $distDir)) {
    New-Item -ItemType Directory -Path $distDir -Force | Out-Null
}

# 3. Locate MSBuild or csc
$msBuildCandidates = @(
    "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe",
    "C:\Windows\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe"
)
$msBuildPath = $msBuildCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1

$cscCandidates = @(
    "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe",
    "C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe"
)
$cscPath = $cscCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1

Write-Host "[2/3] Compiling source code..." -ForegroundColor Yellow

if ($msBuildPath) {
    Write-Host "      Using MSBuild: $msBuildPath" -ForegroundColor DarkGray
    $csproj = Join-Path $srcDir "BatteryAlert.csproj"
    & $msBuildPath $csproj "/p:Configuration=$Configuration" "/p:OutputPath=..\dist\" "/verbosity:minimal" "/nologo"
    if ($LASTEXITCODE -ne 0) {
        throw "MSBuild failed with exit code $LASTEXITCODE"
    }
} elseif ($cscPath) {
    Write-Host "      Using csc.exe: $cscPath" -ForegroundColor DarkGray
    $csFiles = Get-ChildItem -Path $srcDir -Filter "*.cs" -Recurse | Select-Object -ExpandProperty FullName
    $manifestPath = Join-Path $srcDir "app.manifest"
    $cscArgs = @(
        "/target:winexe",
        "/optimize+",
        "/nologo",
        "/out:$outputExe",
        "/win32icon:$icoPath",
        "/win32manifest:$manifestPath",
        "/r:System.dll",
        "/r:System.Core.dll",
        "/r:System.Drawing.dll",
        "/r:System.Windows.Forms.dll",
        "/r:System.Web.Extensions.dll"
    ) + $csFiles
    & $cscPath $cscArgs
    if ($LASTEXITCODE -ne 0) {
        throw "csc.exe failed with exit code $LASTEXITCODE"
    }
} else {
    throw "No C# compiler or MSBuild found on this system."
}

# Clean any secondary pdb or xml files from dist if Release
Get-ChildItem -Path $distDir -Exclude "BatteryAlert.exe" | Remove-Item -Force -ErrorAction SilentlyContinue

if (Test-Path $outputExe) {
    $fileInfo = Get-Item $outputExe
    $sizeKb = [Math]::Round($fileInfo.Length / 1KB, 2)
    Write-Host "[3/3] Build Succeeded!" -ForegroundColor Green
    Write-Host "==========================================================" -ForegroundColor Green
    Write-Host " Output Executable: $outputExe" -ForegroundColor White
    Write-Host " File Size:         $sizeKb KB (Single standalone binary)" -ForegroundColor White
    Write-Host "==========================================================" -ForegroundColor Green
} else {
    throw "Build failed: $outputExe was not produced."
}
