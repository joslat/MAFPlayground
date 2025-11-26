# AG-UI Projects

This directory contains three projects that demonstrate the **AG-UI (Agent Gateway User Interface) protocol** with Microsoft Agent Framework:

## Projects

### 1. **Shared** - Common Configuration Library
A shared class library containing configuration code used by both server and client projects.

**Key Files:**
- `AIConfig.cs` - Shared Azure OpenAI configuration supporting both API Key and DefaultAzureCredential authentication

### 2. **AGUI.Server** - ASP.NET Core Web Server
Hosts an AI agent and exposes it via the AG-UI protocol.

### 3. **AGUI.Client** - Console Client Application
Connects to the AG-UI server and provides an interactive chat interface.

## Quick Start

### Prerequisites

1. .NET 9.0 SDK or later
2. Azure OpenAI Service with a deployed model
3. Environment variables configured

### Step 1: Set Environment Variables

**Windows (PowerShell):**
```powershell
$env:AZURE_OPENAI_ENDPOINT="https://your-resource.openai.azure.com/"
$env:AZURE_OPENAI_API_KEY="your-api-key"
$env:AZURE_OPENAI_DEPLOYMENT_NAME="gpt-4o-mini"
```

### Step 2: Start the Server

```bash
cd AGUI.Server
dotnet run
```

### Step 3: Run the Client (in a new terminal)

```bash
cd AGUI.Client
dotnet run
```

## Architecture

```
AGUI.Client (Console)
    ? HTTP POST + SSE
AGUI.Server (ASP.NET Core)
    ?
Azure OpenAI Service
```

For detailed documentation, see the individual project READMEs.
