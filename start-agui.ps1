# AG-UI Multi-Project Launcher
# This script starts AGUI.Server and AGUI.Client in the correct order

param(
    [Parameter(Position=0)]
    [ValidateSet("basic", "tools", "with-tools", "inspectable", "frontend", "frontend-tools")]
    [string]$AgentType = "basic"
)

Write-Host "Starting AG-UI Server and Client..." -ForegroundColor Green
Write-Host ""

# Check if we're in the solution root
if (!(Test-Path "AGUI.Server") -or !(Test-Path "AGUI.Client")) {
    Write-Host "Error: Please run this script from the solution root directory" -ForegroundColor Red
    Write-Host "Expected to find AGUI.Server and AGUI.Client directories" -ForegroundColor Red
    exit 1
}

# Display agent type
Write-Host "Agent Type: " -NoNewline
if ($AgentType -eq "tools" -or $AgentType -eq "with-tools") {
    Write-Host "$AgentType (Travel Assistant with backend tools)" -ForegroundColor Green
    Write-Host "  Backend Tools: Weather, Restaurants, Time" -ForegroundColor DarkGreen
} elseif ($AgentType -eq "inspectable" -or $AgentType -eq "frontend" -or $AgentType -eq "frontend-tools") {
    Write-Host "$AgentType (Inspectable agent with frontend tool support)" -ForegroundColor Magenta
    Write-Host "  Frontend Tools: Client registers and executes tools locally" -ForegroundColor DarkMagenta
    Write-Host "  Middleware: Server inspects tools sent by client" -ForegroundColor DarkMagenta
} else {
    Write-Host "$AgentType (Basic conversational assistant)" -ForegroundColor Cyan
}
Write-Host ""

# Check for Azure OpenAI configuration
if ($env:AZURE_OPENAI_ENDPOINT) {
    Write-Host "Found Azure OpenAI endpoint: $($env:AZURE_OPENAI_ENDPOINT)" -ForegroundColor Green
    if ($env:AZURE_OPENAI_DEPLOYMENT_NAME) {
        Write-Host "Using deployment: $($env:AZURE_OPENAI_DEPLOYMENT_NAME)" -ForegroundColor Green
    }
} else {
    Write-Host "Warning: AZURE_OPENAI_ENDPOINT not found!" -ForegroundColor Yellow
    Write-Host "Please set the AZURE_OPENAI_ENDPOINT environment variable" -ForegroundColor Yellow
    Write-Host "Example: `$env:AZURE_OPENAI_ENDPOINT='https://your-resource.openai.azure.com/'" -ForegroundColor Yellow
    Write-Host ""
}

# Build both projects
Write-Host ""
Write-Host "Building AGUI.Server..." -ForegroundColor Cyan
Push-Location AGUI.Server
$buildResult = dotnet build --verbosity quiet
if ($LASTEXITCODE -ne 0) {
    Write-Host "Failed to build AGUI.Server" -ForegroundColor Red
    Pop-Location
    exit 1
}
Pop-Location
Write-Host "? AGUI.Server build completed" -ForegroundColor Green

Write-Host ""
Write-Host "Building AGUI.Client..." -ForegroundColor Cyan
Push-Location AGUI.Client
$buildResult = dotnet build --verbosity quiet
if ($LASTEXITCODE -ne 0) {
    Write-Host "Failed to build AGUI.Client" -ForegroundColor Red
    Pop-Location
    exit 1
}
Pop-Location
Write-Host "? AGUI.Client build completed" -ForegroundColor Green

Write-Host ""
Write-Host (new string('=', 80))
Write-Host "Starting AGUI.Server in background..." -ForegroundColor Cyan
Write-Host (new string('=', 80))
Write-Host ""

# Set agent type environment variable for the server
$env:AGUI_AGENT_TYPE = $AgentType

# Start AGUI.Server in a new window with agent type argument
$serverProcess = Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$PWD\AGUI.Server'; `$env:AGUI_AGENT_TYPE='$AgentType'; Write-Host '=== AGUI.Server ===' -ForegroundColor Cyan; dotnet run --no-build $AgentType" -PassThru -WindowStyle Normal

# Wait for server to be ready by checking if port 8888 is listening
Write-Host "Waiting for server to start (checking port 8888)..." -ForegroundColor Gray
Start-Sleep -Seconds 3

$maxRetries = 15
$retryCount = 0
$serverReady = $false

while ($retryCount -lt $maxRetries -and !$serverReady) {
    try {
        $tcpConnection = Test-NetConnection -ComputerName "localhost" -Port 8888 -InformationLevel Quiet -WarningAction SilentlyContinue -ErrorAction SilentlyContinue
        if ($tcpConnection) {
            $serverReady = $true
            Write-Host ""
            Write-Host "? Server is ready! (took $retryCount seconds)" -ForegroundColor Green
        } else {
            Write-Host "." -NoNewline -ForegroundColor Gray
            Start-Sleep -Seconds 1
            $retryCount++
        }
    } catch {
        Write-Host "." -NoNewline -ForegroundColor Gray
        Start-Sleep -Seconds 1
        $retryCount++
    }
}

if (!$serverReady) {
    Write-Host ""
    Write-Host "??  Server port 8888 not responding after $maxRetries seconds" -ForegroundColor Yellow
    Write-Host "   Continuing anyway - server might still be starting..." -ForegroundColor Yellow
}

Write-Host ""
Write-Host (new string('=', 80))
Write-Host "Starting AGUI.Client..." -ForegroundColor Cyan
if ($AgentType -eq "inspectable" -or $AgentType -eq "frontend" -or $AgentType -eq "frontend-tools") {
    Write-Host "Client will register frontend tools (Location, Sensors, SystemInfo)" -ForegroundColor Magenta
}
Write-Host (new string('=', 80))
Write-Host ""

# Start AGUI.Client in the current window
Push-Location AGUI.Client
dotnet run --no-build
Pop-Location

Write-Host ""
Write-Host "Demo completed!" -ForegroundColor Green
Write-Host ""
Write-Host "??  Note: AGUI.Server is still running in a separate window." -ForegroundColor Yellow
Write-Host "   Please close the server window manually when done." -ForegroundColor Gray
Write-Host ""
