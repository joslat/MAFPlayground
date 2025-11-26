// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
// Modified for MAFPlayground by Jose Luis Latorre

using AGUI.Server.Agents;
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
});

builder.Services.AddAGUI();

var app = builder.Build();

// ====================================
// Agent Configuration
// ====================================

// Get agent type from environment variable or command-line argument
//var agentType = Environment.GetEnvironmentVariable("AGUI_AGENT_TYPE") 
//    ?? args.FirstOrDefault() 
//    ?? "basic"; // Default to basic agent
var agentType = "Inspectable"; // For testing InspectableAgent

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
switch (agentType.ToLowerInvariant())
{
    case "tools":
    case "with-tools":
    case "withtools":
        // Get JSON options for tool serialization
        var jsonOptions = app.Services.GetRequiredService<IOptions<Microsoft.AspNetCore.Http.Json.JsonOptions>>().Value;
        agent = AgentWithTools.Create(chatClient, jsonOptions);
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"✓ Agent: {agent.Name} (backend tools)");
        Console.ResetColor();
        Console.WriteLine($"  Description: {AgentWithTools.GetDescription()}");
        Console.WriteLine($"  Backend tools available:");
        Console.WriteLine($"    • GetWeather - Get current weather for a location");
        Console.WriteLine($"    • SearchRestaurants - Find restaurants by location and cuisine");
        Console.WriteLine($"    • GetCurrentTime - Get current time for a location/timezone");
        break;

    case "inspectable":
    case "frontend":
    case "frontend-tools":
        agent = InspectableAgent.Create(chatClient);
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine($"✓ Agent: {agent.Name} (frontend tools support)");
        Console.ResetColor();
        Console.WriteLine($"  Description: {InspectableAgent.GetDescription()}");
        Console.WriteLine($"  Frontend tools: Client registers tools (e.g., GetUserLocation, ReadSensors)");
        Console.WriteLine($"  Middleware: Inspects and logs tools sent by client");
        break;

    case "basic":
    default:
        agent = BasicAgent.Create(chatClient);
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"✓ Agent: {agent.Name} (basic)");
        Console.ResetColor();
        Console.WriteLine($"  Description: {BasicAgent.GetDescription()}");
        break;
}

Console.WriteLine();
Console.WriteLine(new string('-', 80));

// Map the AG-UI agent endpoint at root
app.MapAGUI("/", agent);

Console.WriteLine();
Console.WriteLine("✓✓ AG-UI Server is starting...");
Console.WriteLine($"   Listening on: http://localhost:8888");
Console.WriteLine($"   AG-UI Endpoint: http://localhost:8888/");
Console.WriteLine();
Console.WriteLine("💡 To test with the client:");
Console.WriteLine("   1. Keep this server running");
Console.WriteLine("   2. Run the AGUI.Client project in another terminal");
Console.WriteLine();
Console.WriteLine("💡 To switch agent type:");
Console.WriteLine("   Set environment variable: $env:AGUI_AGENT_TYPE='tools'");
Console.WriteLine("   Or pass as argument:");
Console.WriteLine("     • dotnet run basic      - Simple conversational agent");
Console.WriteLine("     • dotnet run tools      - Agent with backend tools");
Console.WriteLine("     • dotnet run inspectable - Agent with frontend tool support");
Console.WriteLine();
Console.WriteLine("Press Ctrl+C to shut down the server");
Console.WriteLine(new string('═', 80));
Console.WriteLine();

await app.RunAsync("http://localhost:8888");
