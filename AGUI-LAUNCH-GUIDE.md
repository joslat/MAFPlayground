# AG-UI Multi-Project Launch Configuration

This document explains how to configure and run both **AGUI.Server** and **AGUI.Client** together, including support for **backend tool rendering**.

## Prerequisites

- ? .NET 9 SDK installed
- ? Azure OpenAI endpoint configured:
  ```powershell
  $env:AZURE_OPENAI_ENDPOINT = "https://your-resource.openai.azure.com/"
  $env:AZURE_OPENAI_API_KEY = "your-api-key"
  $env:AZURE_OPENAI_DEPLOYMENT_NAME = "gpt-4o-mini"  # or your deployment name
  ```

---

## Agent Types

The server supports two agent configurations:

### 1. **Basic Agent** (Default)
- Simple conversational assistant
- No function tools
- Good for basic Q&A

### 2. **Agent with Tools** (Travel Assistant)
- Weather information tool
- Restaurant search tool
- Current time/timezone tool
- Demonstrates backend tool rendering

---

## Option 1: Visual Studio Multiple Startup Projects ? (Recommended)

This is the easiest way to run both projects from Visual Studio.

### Steps:

1. **Right-click on the Solution** in Solution Explorer
2. Select **"Configure Startup Projects..."** or **"Set Startup Projects..."**
3. Select **"Multiple startup projects"**
4. Configure the startup order:
   - ? **AGUI.Server** ? Action: **Start** (must be first)
   - ? **AGUI.Client** ? Action: **Start**
5. Use the **Move Up/Down** buttons to ensure **AGUI.Server is ABOVE AGUI.Client**
6. Click **OK**

### Running with Tools:

Before running, set the agent type:

```powershell
# In Visual Studio Developer PowerShell or terminal
$env:AGUI_AGENT_TYPE = "tools"
```

### Running:

- Press **F5** or click **Start Debugging**
- OR press **Ctrl+F5** for **Start Without Debugging**

---

## Option 2: PowerShell Launch Script ?? (Easiest)

### Running the Script:

```powershell
# From the solution root directory

# Basic agent (no tools)
.\start-agui.ps1

# Agent with tools
.\start-agui.ps1 tools
```

### Execution Policy Fix:

```powershell
Unblock-File -Path .\start-agui.ps1
.\start-agui.ps1 tools
```

---

## Option 3: Manual Terminal Launch

### Terminal 1 - Start Server:

```powershell
cd AGUI.Server

# Basic agent
dotnet run

# Agent with tools
dotnet run tools
```

### Terminal 2 - Start Client:
```powershell
cd AGUI.Client
dotnet run
```

---

## Backend Tool Rendering Features

### Available Tools (Agent with Tools)

1. **GetWeather** - Get current weather for a location
2. **SearchRestaurants** - Find restaurants by location and cuisine
3. **GetCurrentTime** - Get current time for a location/timezone

### Example Interactions

```
User: What's the weather in Paris?

?? [Tool Call: GetWeather]
   Parameter: location = Paris, France
? [Tool Result]
   Result: {"Location":"Paris, France","Condition":"Sunny","Temperature":24}
