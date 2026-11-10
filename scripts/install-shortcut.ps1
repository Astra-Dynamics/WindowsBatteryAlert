# ==============================================================================
# BatteryAlert - Start Menu Shortcut Registrar
# Registers BatteryAlert into the Windows Start Menu so it appears in Windows Search.
# ==============================================================================

[CmdletBinding()]
param(
    [switch]$Uninstall
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptDir
$targetExe = Join-Path $projectRoot "dist\BatteryAlert.exe"
$icoPath = Join-Path $projectRoot "assets\app.ico"
$shortcutPath = Join-Path "$env:APPDATA\Microsoft\Windows\Start Menu\Programs" "BatteryAlert.lnk"

if ($Uninstall) {
    if (Test-Path $shortcutPath) {
        Remove-Item -Path $shortcutPath -Force
        Write-Host "Removed Start Menu shortcut: $shortcutPath" -ForegroundColor Yellow
    } else {
        Write-Host "Shortcut not found." -ForegroundColor DarkGray
    }
    return
}

if (-not (Test-Path $targetExe)) {
    throw "Target executable not found at $targetExe. Please run build.ps1 first."
}

$wsh = New-Object -ComObject WScript.Shell
$shortcut = $wsh.CreateShortcut($shortcutPath)
$shortcut.TargetPath = $targetExe
$shortcut.WorkingDirectory = [System.IO.Path]::GetDirectoryName($targetExe)
if (Test-Path $icoPath) {
    $shortcut.IconLocation = "$icoPath, 0"
}
$shortcut.Description = "BatteryAlert - Laptop Charging Notification Utility"
$shortcut.Save()

Write-Host "Successfully registered BatteryAlert in Windows Start Menu!" -ForegroundColor Green
Write-Host "Shortcut location: $shortcutPath" -ForegroundColor White
Write-Host "You can now press the Windows key and search for 'BatteryAlert' directly." -ForegroundColor Cyan
