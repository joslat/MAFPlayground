# Sample 20: DevUI Basic Usage

## Overview

This sample demonstrates how to use the **DevUI (Developer UI)** - a web-based interface for testing and debugging AI agents and workflows. Unlike the other samples which are console applications, this sample starts an ASP.NET Core web server with an interactive UI.

## What is DevUI?

DevUI is a browser-based developer tool that provides:

- **Interactive Agent Testing** - Chat with agents through a web interface
- **Visual Workflow Debugging** - See workflows execute in real-time
- **Telemetry Visualization** - View traces, metrics, and logs
- **Multi-Agent Support** - Test multiple agents without code changes
- **Python Compatibility** - OpenAI Responses API for cross-platform testing

## How to Run

1. **Uncomment the Sample20 line in Program.cs:**
   ```csharp
   Sample20_DevUIBasicUsage.Execute(); // Note: This is synchronous (starts web server)
   ```

2. **Comment out any other samples** (only one sample should run at a time)

3. **Run the application:**
   ```bash
   dotnet run
   ```

4. **Open your browser** to: `http://localhost:5000/devui`

## Available Endpoints

Once the server is running, you can access:

| Endpoint | Description |
|----------|-------------|
| `http://localhost:5000/devui` | Main DevUI web interface |
| `http://localhost:5000/v1/responses` | OpenAI Responses API (Python compatible) |
| `http://localhost:5000/v1/conversations` | OpenAI Conversations API |

## Registered Agents

This sample registers 4 agents/workflows that you can test:

1. **assistant** - General purpose helper
   - Answers questions concisely and accurately

2. **poet** - Creative poetry generator
   - Responds to all requests with beautiful poetry

3. **coder** - Programming expert
   - Helps with coding questions and provides code examples

4. **review-workflow** - Assistant?Reviewer workflow
   - Sequential workflow that generates content and then reviews it

## Using the DevUI

### Step 1: Select an Agent
In the DevUI interface, use the dropdown menu to select which agent you want to test.

### Step 2: Start Chatting
Type your message in the input box and press Enter or click Send.

### Step 3: View Results
- See the agent's response in real-time
- Switch between agents without restarting
- View telemetry data (if configured)

## Key Differences from Other Samples

| Feature | Other Samples | Sample20 (DevUI) |
|---------|--------------|------------------|
| Interface | Console | Web Browser |
| Execution | Single run | Long-running server |
| Agent Switching | Requires code changes | Dropdown selection |
| Visualization | Text output | Interactive UI |
| Method | `async Task Execute()` | `void Execute()` |

## Architecture

```
???????????????????????????
?  Your Browser           ?
?  http://localhost:5000  ?
???????????????????????????
            ?
            ?
???????????????????????????
?  ASP.NET Core Web App   ?
?  • DevUI Middleware     ?
?  • OpenAI Responses API ?
?  • Agent Hosting        ?
???????????????????????????
            ?
            ?
???????????????????????????
?  Azure OpenAI           ?
?  (via AIConfig)         ?
???????????????????????????
```

## Technical Details

### SDK Change
Note that this sample requires changing the project SDK to `Microsoft.NET.Sdk.Web`:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
```

This enables ASP.NET Core features including `WebApplication` and `WebApplicationBuilder`.

### Development Mode
The sample forces **Development mode** to ensure DevUI is enabled:

```csharp
var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    EnvironmentName = Environments.Development
});
```

Without this, DevUI would not be mapped (it only works in Development mode).

### Required Packages
- `Microsoft.Agents.AI.DevUI` - DevUI web interface
- `Microsoft.Agents.AI.Hosting` - Agent hosting infrastructure
- `Microsoft.Agents.AI.Hosting.OpenAI` - OpenAI-specific hosting extensions

### Agent Registration
Agents are registered using the hosting extension methods:

```csharp
builder.AddAIAgent("agent-name", "Instructions for the agent");
```

### Workflow Registration
Workflows can be registered and exposed as agents:

```csharp
builder.AddWorkflow("workflow-name", (sp, key) => {
    // Build and return workflow
    return AgentWorkflowBuilder.BuildSequential(...);
}).AddAsAIAgent();
```

## Stopping the Server

To stop the DevUI server, press `Ctrl+C` in the terminal.

## Python DevUI Compatibility

The OpenAI Responses API (`/v1/responses`) is compatible with Python DevUI clients. This allows:
- Cross-platform agent testing
- Integration with Python tools
- Standardized API access

The endpoint dynamically routes requests to agents based on the `model` field in the request.

## Next Steps

- Add your own agents to test
- Create custom workflows to debug
- Integrate with telemetry systems (see AgentOpenTelemetry project)
- Explore the Python DevUI compatibility

## Troubleshooting

### 404 Error on DevUI Endpoint

If you get a 404 error, check:
1. **Environment Mode** - The sample now forces Development mode, so this should work
2. **Correct URL** - Use `http://localhost:5000/devui` (HTTP, not HTTPS)
3. **Server Logs** - Check console output for "Now listening on" message

### Port Already in Use
If port 5000 is already in use, you can change it by adding:
```csharp
builder.WebHost.UseUrls("http://localhost:YOUR_PORT");
```

### Certificate Errors
This sample uses HTTP (not HTTPS) to avoid certificate issues. For production, you'd want HTTPS:
```bash
dotnet dev-certs https --trust
```

### DevUI Not Loading
- Ensure the sample forced Development mode (check console output)
- Check the console for error messages
- Verify all required packages are installed
- Try clearing browser cache
