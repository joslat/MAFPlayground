# Start AG-UI React Client
# This script starts the React frontend for SharedStateCookingSimple

Write-Host "???????????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host "     AG-UI React Client Startup" -ForegroundColor Cyan
Write-Host "???????????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host ""

# Check if server is running
$serverUrl = "http://localhost:8888"

Write-Host "?? Checking server availability..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "$serverUrl/health" -Method Options -TimeoutSec 2 -ErrorAction Stop
    Write-Host "? Server is running at: $serverUrl" -ForegroundColor Green
} catch {
    Write-Host "??  WARNING: Cannot connect to server at $serverUrl" -ForegroundColor Red
    Write-Host ""
    Write-Host "Make sure the AG-UI Server is running:" -ForegroundColor Yellow
    Write-Host "   1. Open another terminal" -ForegroundColor White
    Write-Host "   2. Run: .\start-sharedstate-server.ps1" -ForegroundColor White
    Write-Host ""
    Write-Host "Do you want to continue anyway? (Y/N): " -ForegroundColor Yellow -NoNewline
    $continue = Read-Host
    if ($continue -ne 'Y' -and $continue -ne 'y') {
        Write-Host "Exiting..." -ForegroundColor Gray
        exit 1
    }
}

Write-Host ""

# Navigate to client directory
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$clientPath = Join-Path $scriptDir "AGUI.Client.React"
Push-Location $clientPath

# Check if node_modules exists
if (-not (Test-Path "node_modules")) {
    Write-Host "?? Installing dependencies (first time)..." -ForegroundColor Yellow
    Write-Host ""
    npm install
    if ($LASTEXITCODE -ne 0) {
        Write-Host ""
        Write-Host "? npm install failed!" -ForegroundColor Red
        Pop-Location
        exit 1
    }
    Write-Host ""
    Write-Host "? Dependencies installed successfully" -ForegroundColor Green
    Write-Host ""
}

Write-Host "?? Starting React Development Server..." -ForegroundColor Green
Write-Host ""
Write-Host "Frontend will open at: http://localhost:5173" -ForegroundColor Cyan
Write-Host "Backend AG-UI endpoint: $serverUrl/" -ForegroundColor Cyan
Write-Host "Backend health check: $serverUrl/health" -ForegroundColor Cyan
Write-Host ""
Write-Host "Press Ctrl+C to stop the client" -ForegroundColor Gray
Write-Host "???????????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host ""

# Run the client
try {
    npm run dev
} finally {
    Pop-Location
}
