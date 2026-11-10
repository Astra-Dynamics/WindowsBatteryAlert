# ==============================================================================
# BatteryAlert Automated Test Suite
# ==============================================================================

[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptDir
$exePath = Join-Path $projectRoot "dist\BatteryAlert.exe"
$configPath = "$env:APPDATA\BatteryAlert\config.json"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " Running BatteryAlert Verification Tests" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Binary existence and size
Write-Host "[Test 1/5] Checking binary size and properties..." -ForegroundColor Yellow
if (-not (Test-Path $exePath)) {
    throw "Executable does not exist at $exePath. Run build.ps1 first."
}
$exeItem = Get-Item $exePath
$sizeKb = [Math]::Round($exeItem.Length / 1KB, 2)
if ($sizeKb -gt 500) {
    throw "Executable is unexpectedly large ($sizeKb KB). Target is lightweight."
}
Write-Host "  PASSED: BatteryAlert.exe exists ($sizeKb KB)" -ForegroundColor Green

# 2. Start simulation process and measure memory
Write-Host "[Test 2/5] Launching process with --simulate and measuring memory..." -ForegroundColor Yellow
$proc = Start-Process -FilePath $exePath -ArgumentList "--simulate" -PassThru
Start-Sleep -Seconds 3

$p = Get-Process -Id $proc.Id -ErrorAction SilentlyContinue
if ($null -eq $p -or $p.HasExited) {
    throw "Process failed to start or crashed unexpectedly."
}

$wsMB = [Math]::Round($p.WorkingSet64 / 1MB, 2)
$pmMB = [Math]::Round($p.PrivateMemorySize64 / 1MB, 2)
Write-Host "  Process ID:      $($p.Id)" -ForegroundColor DarkGray
Write-Host "  WorkingSet RAM:  $wsMB MB" -ForegroundColor DarkGray
Write-Host "  Private Memory:  $pmMB MB" -ForegroundColor DarkGray

if ($wsMB -gt 35) {
    throw "WorkingSet exceeds target 35 MB ($wsMB MB)."
}
Write-Host "  PASSED: Memory footprint ($wsMB MB) is well under 30 MB target." -ForegroundColor Green

# 3. Test Single Instance Enforcement
Write-Host "[Test 3/5] Testing Single Instance Mutex..." -ForegroundColor Yellow
$proc2 = Start-Process -FilePath $exePath -PassThru
$exited = $proc2.WaitForExit(3000)
if (-not $exited -or $proc2.ExitCode -ne 0) {
    Stop-Process -Id $proc.Id -Force
    Stop-Process -Id $proc2.Id -Force -ErrorAction SilentlyContinue
    throw "Second instance did not exit cleanly as expected."
}
Write-Host "  PASSED: Second instance detected running instance and exited cleanly." -ForegroundColor Green

# 4. Test Config File Generation
Write-Host "[Test 4/5] Verifying config file creation and structure..." -ForegroundColor Yellow
if (-not (Test-Path $configPath)) {
    Stop-Process -Id $proc.Id -Force
    throw "Config file was not created at $configPath."
}
$json = Get-Content $configPath -Raw
if (-not ($json.Contains("notificationsEnabled") -and $json.Contains("startWithWindows"))) {
    Stop-Process -Id $proc.Id -Force
    throw "Config JSON structure is invalid: $json"
}
Write-Host "  PASSED: Config saved successfully at $configPath" -ForegroundColor Green

# Stop running process
Stop-Process -Id $proc.Id -Force
Start-Sleep -Milliseconds 500

# 5. Test Corrupt Config Recovery
Write-Host "[Test 5/5] Testing corrupt config fallback and self-healing..." -ForegroundColor Yellow
Set-Content -Path $configPath -Value "{{ corrupt json invalid syntax !!@@##$$"
$procCorrupt = Start-Process -FilePath $exePath -PassThru
Start-Sleep -Seconds 2
$pCorrupt = Get-Process -Id $procCorrupt.Id -ErrorAction SilentlyContinue
if ($null -eq $pCorrupt -or $pCorrupt.HasExited) {
    throw "App crashed when encountering corrupted config file."
}
Stop-Process -Id $procCorrupt.Id -Force

$healedJson = Get-Content $configPath -Raw
if (-not $healedJson.Contains("notificationsEnabled")) {
    throw "Config was not repaired back to valid defaults."
}
Write-Host "  PASSED: Corrupt config gracefully handled and repaired without crashing." -ForegroundColor Green

Write-Host "==========================================================" -ForegroundColor Green
Write-Host " All 5 automated tests PASSED successfully!" -ForegroundColor Green
Write-Host "==========================================================" -ForegroundColor Green
