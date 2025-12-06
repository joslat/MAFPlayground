# Start AG-UI Server with Shared State Recipe Agent
# This script starts the AG-UI backend with the SharedStateCookingSimple agent

Write-Host "???????????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host "     AG-UI Server - Shared State Recipe Agent Startup" -ForegroundColor Cyan
Write-Host "???????????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host ""

# Check for environment variables
$endpoint = $env:AZURE_OPENAI_ENDPOINT
$apiKey = $env:AZURE_OPENAI_API_KEY
$deployment = $env:AZURE_OPENAI_DEPLOYMENT_NAME

if ([string]::IsNullOrWhiteSpace($endpoint)) {
    Write-Host "? ERROR: AZURE_OPENAI_ENDPOINT is not set!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please set the environment variable:" -ForegroundColor Yellow
    Write-Host "   `$env:AZURE_OPENAI_ENDPOINT='https://your-resource.openai.azure.com/'" -ForegroundColor White
    Write-Host ""
    exit 1
}

if ([string]::IsNullOrWhiteSpace($apiKey)) {
    Write-Host "? ERROR: AZURE_OPENAI_API_KEY is not set!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please set the environment variable:" -ForegroundColor Yellow
    Write-Host "   `$env:AZURE_OPENAI_API_KEY='your-api-key'" -ForegroundColor White
    Write-Host ""
    exit 1
}

Write-Host "? Environment Check:" -ForegroundColor Green
Write-Host "   Endpoint: $endpoint" -ForegroundColor White
Write-Host "   Deployment: $(if ($deployment) { $deployment } else { 'gpt-4o-mini (default)' })" -ForegroundColor White
Write-Host "   Auth: API Key" -ForegroundColor White
Write-Host ""

# Set agent type to shared state
$env:AGUI_AGENT_TYPE = 'sharedstate'

# Navigate to server directory
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Push-Location (Join-Path $scriptDir "AGUI.Server")

Write-Host "?? Starting AG-UI Server with Shared State Recipe Agent..." -ForegroundColor Green
Write-Host ""
Write-Host "Press Ctrl+C to stop the server" -ForegroundColor Gray
Write-Host "???????????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host ""

# Run the server
try {
    dotnet run
} finally {
    Pop-Location
}
