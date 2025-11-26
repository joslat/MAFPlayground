# AG-UI with Backend Tool Rendering

This implementation demonstrates **backend tool rendering** with AG-UI, following the official [Microsoft documentation](https://learn.microsoft.com/en-us/agent-framework/integrations/ag-ui/backend-tool-rendering?pivots=programming-language-csharp).

## Overview

Backend tool rendering enables:
- ? **Server-side execution**: Tools run securely on the server
- ? **Automatic streaming**: Tool calls and results stream to clients in real-time
- ? **Client transparency**: Clients see tool execution progress
- ? **No client updates needed**: Add/modify tools without changing client code

---

## Quick Start

```powershell
# Run with tools (Travel Assistant)
.\start-agui.ps1 tools

# Run with basic agent
.\start-agui.ps1
```

---

## Agent Types

### 1. Basic Agent (Default)
- Simple conversational assistant
- No function tools

### 2. Agent with Tools (Travel Assistant)
- **GetWeather** - Current weather for any location
- **SearchRestaurants** - Find restaurants by location/cuisine
- **GetCurrentTime** - Get time in any timezone

---

## Example Interactions

```
User: What's the weather in Paris and suggest Italian restaurants?

?? [Tool Call: GetWeather]
   Parameter: location = Paris, France
? [Tool Result - Temperature: 24°C, Condition: Sunny]

?? [Tool Call: SearchRestaurants]
   Parameter: location = Paris
   Parameter: cuisine = Italian
? [Tool Result - Found 3 restaurants]