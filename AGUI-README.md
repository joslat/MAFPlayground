# AG-UI Demo Projects

A complete demonstration of the **AG-UI (Agent Gateway User Interface) protocol** using .NET 9 and Microsoft Agent Framework.

## ?? Projects Structure

```
MAFPlayground/
??? Shared/                    # Shared configuration library
?   ??? AIConfig.cs           # Azure OpenAI configuration
?   ??? Shared.csproj
??? AGUI.Server/              # ASP.NET Core Web Server
?   ??? Program.cs            # Server implementation
?   ??? AGUI.Server.csproj
??? AGUI.Client/              # Console Client
    ??? Program.cs            # Client implementation
    ??? AGUI.Client.csproj
```

## ?? Quick Start Guide

### Prerequisites

- ? .NET 9.0 SDK
- ? Azure OpenAI Service
- ? Model deployed (e.g., gpt-4o-mini)

### 1. Configure Environment Variables

**Windows (PowerShell):**
```powershell
$env:AZURE_OPENAI_ENDPOINT="https://your-resource.openai.azure.com/"
$env:AZURE_OPENAI_API_KEY="your-api-key"
$env:AZURE_OPENAI_DEPLOYMENT_NAME="gpt-4o-mini"
```

**Linux/Mac:**
```bash
export AZURE_OPENAI_ENDPOINT="https://your-resource.openai.azure.com/"
export AZURE_OPENAI_API_KEY="your-api-key"
export AZURE_OPENAI_DEPLOYMENT_NAME="gpt-4o-mini"
```

### 2. Start the Server

Terminal 1:
```bash
cd AGUI.Server
dotnet run
```

Server will start on: `http://localhost:8888`

### 3. Run the Client

Terminal 2 (keep server running):
```bash
cd AGUI.Client
dotnet run
```

### 4. Chat!

```
User (:q or quit to exit): What is 2 + 2?

[Run Started - Thread: abc123, Run: xyz789]
Assistant: 2 + 2 equals 4.
[Run Finished - Thread: abc123]
```

## ?? What is AG-UI?

**AG-UI (Agent Gateway User Interface)** is a protocol for connecting AI agents across networks using:

- **HTTP POST** - For sending requests
- **Server-Sent Events (SSE)** - For streaming responses
- **JSON** - For data serialization
- **Thread Management** - For conversation context

### Key Benefits

| Feature | Description |
|---------|-------------|
| **Streaming** | Real-time response streaming |
| **Stateless** | Server can be scaled horizontally |
| **Context** | Conversation threads maintained |
| **Standard** | HTTP/SSE - works everywhere |

## ??? Architecture

```
???????????????????????????????????????????????????
?           User (Console Input)                  ?
???????????????????????????????????????????????????
               ?
               ?
????????????????????????????????????????????????????
?         AGUI.Client (Console App)                ?
?  • AGUIChatClient                                ?
?  • Message streaming                             ?
?  • Thread management                             ?
?  • Uses Shared/AIConfig                         ?
????????????????????????????????????????????????????
               ?
               ? HTTP POST
               ? + SSE Stream
               ?
????????????????????????????????????????????????????
?      AGUI.Server (ASP.NET Core)                  ?
?  • MapAGUI("/") endpoint                        ?
?  • AIAgent hosting                               ?
?  • SSE streaming                                 ?
?  • Uses Shared/AIConfig                         ?
????????????????????????????????????????????????????
               ?
               ?
????????????????????????????????????????????????????
?        Azure OpenAI Service                      ?
?  • gpt-4o-mini (or your model)                  ?
?  • Streaming completions                         ?
????????????????????????????????????????????????????
```

## ?? Key Components

### Shared Library

**`AIConfig.cs`** - Centralized Azure OpenAI configuration:

- Thread-safe lazy initialization
- Supports API Key or DefaultAzureCredential auth
- Deployment name configuration
- Reusable across all projects

### Server

**AG-UI Server** hosts the AI agent:

```csharp
// Create agent
AIAgent agent = chatClient.CreateAIAgent(
    name: "AGUIAssistant",
    instructions: "You are a helpful assistant.");

// Map endpoint
app.MapAGUI("/", agent);
```

### Client

**AG-UI Client** connects and streams responses:

```csharp
// Create client
AGUIChatClient chatClient = new(httpClient, serverUrl);
AIAgent agent = chatClient.CreateAIAgent(name: "agui-client");

// Stream responses
await foreach (AgentRunResponseUpdate update in agent.RunStreamingAsync(messages, thread))
{
    // Display streaming text
}
```

## ?? Features

### Server Features
- ? ASP.NET Core Web API
- ? Server-Sent Events streaming
- ? AG-UI protocol implementation
- ? Flexible authentication (API Key or DefaultAzureCredential)
- ? Configurable deployment

### Client Features
- ? Interactive console interface
- ? Real-time streaming display
- ? Conversation context management
- ? Color-coded output
- ? Error handling with helpful messages

## ?? Configuration Options

### Authentication Methods

**1. API Key (Default):**
```powershell
$env:AZURE_OPENAI_API_KEY="your-key-here"
```

**2. DefaultAzureCredential (Recommended for Production):**
```bash
# Don't set API_KEY
az login
# Ensure you have "Cognitive Services OpenAI Contributor" role
```

### Custom Server URL

Client can connect to different servers:
```powershell
$env:AGUI_SERVER_URL="http://your-server:8888"
```

## ?? Protocol Details

### Request Format (Client ? Server)

```http
POST / HTTP/1.1
Content-Type: application/json

{
  "messages": [
    {"role": "system", "content": "You are helpful"},
    {"role": "user", "content": "Hello!"}
  ]
}
```

### Response Format (Server ? Client via SSE)

```
data: {"type":"RUN_STARTED","threadId":"abc","runId":"xyz"}

data: {"type":"TEXT_MESSAGE_START","messageId":"msg1","role":"assistant"}

data: {"type":"TEXT_MESSAGE_CONTENT","messageId":"msg1","delta":"Hello"}

data: {"type":"TEXT_MESSAGE_CONTENT","messageId":"msg1","delta":" there!"}

data: {"type":"TEXT_MESSAGE_END","messageId":"msg1"}

data: {"type":"RUN_FINISHED","threadId":"abc","runId":"xyz"}
```

## ?? Testing

### Manual Testing with curl

Test the server without the client:

```bash
curl -N http://localhost:8888/ \
  -H "Content-Type: application/json" \
  -H "Accept: text/event-stream" \
  -d '{"messages":[{"role":"user","content":"What is 2+2?"}]}'
```

### Expected Response

You should see SSE events streaming back with the agent's response.

## ?? Troubleshooting

### Server Won't Start

**Problem:** Port 8888 already in use  
**Solution:** Change port in `Program.cs` or kill process using port 8888

**Problem:** Authentication errors  
**Solution:** 
- Check environment variables are set
- For DefaultAzureCredential: run `az login`
- Verify you have correct role on Azure OpenAI resource

### Client Can't Connect

**Problem:** "Connection error"  
**Solution:**
- Ensure server is running first
- Check server is on `http://localhost:8888`
- Try setting `$env:AGUI_SERVER_URL="http://localhost:8888"`

### No Streaming

**Problem:** Responses don't stream  
**Solution:**
- Check HttpClient timeout is sufficient (60 seconds default)
- Verify server is sending SSE correctly
- Try curl test to isolate issue

## ?? Learning Resources

- [AG-UI Protocol Overview](https://learn.microsoft.com/en-us/agent-framework/integrations/ag-ui/)
- [Agent Framework Documentation](https://learn.microsoft.com/en-us/agent-framework/overview/agent-framework-overview)
- [Server-Sent Events Specification](https://html.spec.whatwg.org/multipage/server-sent-events.html)
- [Azure OpenAI Service](https://learn.microsoft.com/en-us/azure/ai-foundry/openai/)

## ?? Next Steps

1. **Add Tools** - Extend the agent with custom functions
2. **Multiple Agents** - Host multiple specialized agents
3. **Web UI** - Build a web frontend instead of console
4. **Authentication** - Add user authentication/authorization
5. **Telemetry** - Add OpenTelemetry for monitoring

## ?? License

See the main MAFPlayground repository for license information.

## ?? Credits

Based on Microsoft Agent Framework and AG-UI protocol documentation.  
Implemented for MAFPlayground by Jose Luis Latorre.
