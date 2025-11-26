# AG-UI with Backend Tool Rendering - Complete Guide

## ?? Overview

This implementation adds **backend tool rendering** to AG-UI, enabling AI agents to call server-side functions automatically.

### Key Features

? **Server-side tool execution** - Tools run securely on the server  
? **Real-time streaming** - Tool calls and results stream to clients  
? **Client transparency** - See what the AI is doing  
? **Flexible architecture** - Add tools without changing client code  

---

## ?? Quick Start

### Prerequisites

```powershell
$env:AZURE_OPENAI_ENDPOINT = "https://your-resource.openai.azure.com/"
$env:AZURE_OPENAI_API_KEY = "your-api-key"
$env:AZURE_OPENAI_DEPLOYMENT_NAME = "gpt-4o-mini"
```

### Run with Tools

```powershell
.\start-agui.ps1 tools
```

### Run Basic Agent

```powershell
.\start-agui.ps1
```

---

## ??? Available Tools

### 1. GetWeather
Get current weather for any location

**Example**: "What's the weather in Paris?"

### 2. SearchRestaurants
Find restaurants by location and cuisine

**Example**: "Find Italian restaurants in Tokyo"

### 3. GetCurrentTime
Get time in any timezone

**Example**: "What time is it in New York?"

---

## ?? Example Interaction

```
User: What's the weather in Paris and suggest Italian restaurants?

?? [Tool Call: GetWeather]
   Parameter: location = Paris, France
? [Tool Result] Temperature: 24°C, Sunny

?? [Tool Call: SearchRestaurants]
   Parameter: location = Paris
   Parameter: cuisine = Italian
? [Tool Result] Found 3 restaurants