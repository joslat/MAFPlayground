# Start Both AG-UI Server and React Client
# This script starts both the backend and frontend in separate windows

Write-Host "???????????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host "     AG-UI Shared State Recipe Demo - Full Stack Startup" -ForegroundColor Cyan
Write-Host "???????????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host ""

# Check for environment variables
$endpoint = $env:AZURE_OPENAI_ENDPOINT
$apiKey = $env:AZURE_OPENAI_API_KEY

if ([string]::IsNullOrWhiteSpace($endpoint) -or [string]::IsNullOrWhiteSpace($apiKey)) {
    Write-Host "? ERROR: Azure OpenAI credentials not set!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please set the environment variables:" -ForegroundColor Yellow
    Write-Host "   `$env:AZURE_OPENAI_ENDPOINT='https://your-resource.openai.azure.com/'" -ForegroundColor White
    Write-Host "   `$env:AZURE_OPENAI_API_KEY='your-api-key'" -ForegroundColor White
    Write-Host ""
    exit 1
}

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

Write-Host "? Environment variables configured" -ForegroundColor Green
Write-Host ""
Write-Host "?? Starting AG-UI Demo..." -ForegroundColor Green
Write-Host ""

# Start backend in new window
Write-Host "?? Launching Backend Server (AG-UI Server)..." -ForegroundColor Yellow
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$scriptDir'; .\start-sharedstate-server.ps1"

# Wait for backend to start
Write-Host "   ? Waiting for backend to start..." -ForegroundColor Gray
Start-Sleep -Seconds 5

# Start frontend in new window
Write-Host "?? Launching Frontend Client (React)..." -ForegroundColor Yellow
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$scriptDir'; .\start-sharedstate-client.ps1"

Write-Host ""
Write-Host "???????????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host "? AG-UI Shared State Recipe Demo Started!" -ForegroundColor Green
Write-Host "???????????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host ""
Write-Host "?? Servers:" -ForegroundColor White
Write-Host "   Backend:  http://localhost:8888 (AG-UI Server)" -ForegroundColor Cyan
Write-Host "   Frontend: http://localhost:5173 (React)" -ForegroundColor Cyan
Write-Host ""
Write-Host "?? Usage:" -ForegroundColor White
Write-Host "   1. Frontend should open automatically in your browser" -ForegroundColor Gray
Write-Host "   2. If not, navigate to: http://localhost:5173" -ForegroundColor Gray
Write-Host "   3. Chat with the Recipe Assistant!" -ForegroundColor Gray
Write-Host "   4. Watch the UI update INSTANTLY as the agent modifies state" -ForegroundColor Gray
Write-Host ""
Write-Host "?? Learn More:" -ForegroundColor White
Write-Host "   • Backend:  AGUI.Server/Agents/SharedStateCookingSimple/README.md" -ForegroundColor Gray
Write-Host "   • Docs:     https://docs.copilotkit.ai/shared-state" -ForegroundColor Gray
Write-Host ""
Write-Host "??  To stop both servers, close their PowerShell windows" -ForegroundColor Yellow
Write-Host ""
Write-Host "Press any key to exit this launcher..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
