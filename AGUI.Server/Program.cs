// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
// Modified for MAFPlayground by Jose Luis Latorre

using AGUI.Server.Agents;
using AGUI.Server.Agents.SharedStateCookingSimple;
using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Shared;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddHttpClient().AddLogging();

// Configure JSON serialization for complex types
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Add(ToolsJsonSerializerContext.Default);
    options.SerializerOptions.TypeInfoResolverChain.Add(RecipeJsonSerializerContext.Default);
});

// Configure CORS for React frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://127.0.0.1:5173") // Vite default port
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()
              .SetIsOriginAllowedToAllowWildcardSubdomains();
    });
});

builder.Services.AddAGUI();

var app = builder.Build();

// Enable CORS (must be before routing)
app.UseCors("AllowReactFrontend");

// Add request logging middleware
app.Use(async (context, next) =>
{
    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.WriteLine($"[Request] {context.Request.Method} {context.Request.Path} from {context.Request.Headers.Origin}");
    Console.ResetColor();
    
    await next();
    
    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.WriteLine($"[Response] {context.Response.StatusCode} for {context.Request.Path}");
    Console.ResetColor();
});

// ====================================
// Agent Configuration
// ====================================

//// Get agent type from environment variable or command-line argument
var agentType = Environment.GetEnvironmentVariable("AGUI_AGENT_TYPE")
    ?? args.FirstOrDefault()
    ?? "basic"; // Default to basic agent
agentType = "recipe";

Console.WriteLine(new string('═', 80));
Console.WriteLine("=== AG-UI Server ===");
Console.WriteLine(new string('═', 80));
Console.WriteLine($"Endpoint: {AIConfig.Endpoint}");
Console.WriteLine($"Deployment: {AIConfig.ModelDeployment}");
Console.WriteLine();

// Create the AzureOpenAIClient using the lazy config from AIConfig
var azureClient = new AzureOpenAIClient(AIConfig.Endpoint, AIConfig.KeyCredential);

// Get the chat client
var chatClient = azureClient
    .GetChatClient(AIConfig.ModelDeployment)
    .AsIChatClient();

// Create the appropriate agent based on configuration
AIAgent agent;
var jsonOptions = app.Services.GetRequiredService<IOptions<Microsoft.AspNetCore.Http.Json.JsonOptions>>().Value;

switch (agentType.ToLowerInvariant())
{
    case "tools":
    case "with-tools":
    case "withtools":
        // Get JSON options for tool serialization
        agent = AgentWithTools.Create(chatClient, jsonOptions);
        app.MapAGUI("/", agent);
        
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"✓ Agent: {agent.Name} (backend tools)");
        Console.ResetColor();
        Console.WriteLine($"  Description: {AgentWithTools.GetDescription()}");
        Console.WriteLine($"  Endpoint: / (root)");
        Console.WriteLine($"  Backend tools available:");
        Console.WriteLine($"    • GetWeather - Get current weather for a location");
        Console.WriteLine($"    • SearchRestaurants - Find restaurants by location and cuisine");
        Console.WriteLine($"    • GetCurrentTime - Get current time for a location/timezone");
        break;

    case "inspectable":
    case "frontend":
    case "frontend-tools":
        agent = InspectableAgent.Create(chatClient);
        app.MapAGUI("/", agent);
        
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine($"✓ Agent: {agent.Name} (frontend tools support)");
        Console.ResetColor();
        Console.WriteLine($"  Description: {InspectableAgent.GetDescription()}");
        Console.WriteLine($"  Endpoint: / (root)");
        Console.WriteLine($"  Frontend tools: Client registers tools (e.g., GetUserLocation, ReadSensors)");
        Console.WriteLine($"  Middleware: Inspects and logs tools sent by client");
        break;

    case "sharedstate":
    case "shared-state":
    case "recipe":
        agent = RecipeAgent.Create(chatClient, jsonOptions);
        app.MapAGUI("/", agent);
        
        // Add health check endpoint for client startup verification
        app.MapGet("/health", () => Results.Ok(new { status = "ready", agent = "RecipeAssistant" }));
        app.MapMethods("/health", new[] { "OPTIONS" }, () => Results.Ok());
        
        // Add a test endpoint to verify server is responding
        app.MapGet("/test", () => "AG-UI Server is running!");
        
        // Handle OPTIONS on root endpoint for CORS preflight (AG-UI agent endpoint)
        app.MapMethods("/", new[] { "OPTIONS" }, () => Results.Ok());
        
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"✓ Agent: {agent.Name} (shared state)");
        Console.ResetColor();
        Console.WriteLine($"  Description: {RecipeAgent.GetDescription()}");
        Console.WriteLine($"  Endpoint: / (AG-UI agent)");
        Console.WriteLine($"  Health check: /health");
        Console.WriteLine($"  Test endpoint: /test");
        Console.WriteLine($"  Features: Instant UI state updates via AG-UI shared state");
        Console.WriteLine($"  Frontend: Use AGUI.Client.React project");
        break;

    case "basic":
    default:
        agent = BasicAgent.Create(chatClient);
        app.MapAGUI("/", agent);
        
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"✓ Agent: {agent.Name} (basic)");
        Console.ResetColor();
        Console.WriteLine($"  Description: {BasicAgent.GetDescription()}");
        Console.WriteLine($"  Endpoint: / (root)");
        break;
}

Console.WriteLine();
Console.WriteLine(new string('-', 80));

// Map the AG-UI agent endpoint at root
// (Already mapped in switch above)

Console.WriteLine();
Console.WriteLine("✓✓ AG-UI Server is starting...");
Console.WriteLine($"   Listening on: http://localhost:8888");
Console.WriteLine($"   AG-UI Endpoint: / (root)");
Console.WriteLine();

Console.WriteLine("💡 Available agent types:");
Console.WriteLine("   • basic        - Simple conversational agent (default)");
Console.WriteLine("   • tools        - Agent with backend tools (weather, restaurants, time)");
Console.WriteLine("   • inspectable  - Agent with frontend tool support");
Console.WriteLine("   • sharedstate  - Recipe agent with shared state (requires React frontend)");
Console.WriteLine();

Console.WriteLine("💡 To switch agent type:");
Console.WriteLine("   $env:AGUI_AGENT_TYPE='sharedstate'");
Console.WriteLine("   dotnet run");
Console.WriteLine();

if (agentType.ToLowerInvariant() is "sharedstate" or "shared-state" or "recipe")
{
    Console.WriteLine("💡 To test Shared State Recipe Agent:");
    Console.WriteLine("   1. Keep this server running");
    Console.WriteLine("   2. In another terminal:");
    Console.WriteLine("      cd AGUI.Client.React");
    Console.WriteLine("      npm install  # First time only");
    Console.WriteLine("      npm run dev");
    Console.WriteLine("   3. Open browser: http://localhost:5173");
    Console.WriteLine();
}
else
{
    Console.WriteLine("💡 To test with console client:");
    Console.WriteLine("   1. Keep this server running");
    Console.WriteLine("   2. In another terminal:");
    Console.WriteLine("      cd AGUI.Client");
    Console.WriteLine("      dotnet run");
    Console.WriteLine();
}
Console.WriteLine("Press Ctrl+C to shut down the server");
Console.WriteLine(new string('═', 80));
Console.WriteLine();

// Log all registered endpoints for debugging
Console.WriteLine("🔍 Registered Endpoints:");
var endpoints = app.Services.GetService<Microsoft.AspNetCore.Routing.EndpointDataSource>();
if (endpoints != null)
{
    foreach (var endpoint in endpoints.Endpoints)
    {
        if (endpoint is Microsoft.AspNetCore.Routing.RouteEndpoint routeEndpoint)
        {
            Console.WriteLine($"   {routeEndpoint.RoutePattern.RawText}");
        }
    }
}
Console.WriteLine();

await app.RunAsync("http://localhost:8888");
