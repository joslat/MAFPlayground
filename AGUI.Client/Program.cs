// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
// Modified for MAFPlayground by Jose Luis Latorre

using System.ComponentModel;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.AGUI;
using Microsoft.Extensions.AI;
using Shared;

// Set console encoding to UTF-8 to support emojis and special characters
Console.OutputEncoding = System.Text.Encoding.UTF8;

string serverUrl = Environment.GetEnvironmentVariable("AGUI_SERVER_URL") ?? "http://localhost:8888";

Console.WriteLine(new string('═', 80));
Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("=== AG-UI Client ===");
Console.ResetColor();
Console.WriteLine(new string('═', 80));
Console.WriteLine();
Console.WriteLine($"Connecting to AG-UI server at: {serverUrl}");
Console.WriteLine();

// Create the AG-UI client agent
using HttpClient httpClient = new()
{
    Timeout = TimeSpan.FromSeconds(60)
};

AGUIChatClient chatClient = new(httpClient, serverUrl);

// ====================================
// Frontend Tools Definition
// ====================================

// Define frontend tools that execute on the client
AITool[] frontendTools =
[
    AIFunctionFactory.Create(GetUserLocation),
    AIFunctionFactory.Create(ReadClientSensors),
    AIFunctionFactory.Create(GetClientSystemInfo)
];

Console.ForegroundColor = ConsoleColor.Magenta;
Console.WriteLine("📱 Frontend Tools Registered:");
Console.WriteLine("   • GetUserLocation - Get simulated GPS location");
Console.WriteLine("   • ReadClientSensors - Read simulated sensor data");
Console.WriteLine("   • GetClientSystemInfo - Get client system information");
Console.ResetColor();
Console.WriteLine();

// Create agent with frontend tools
AIAgent agent = chatClient.CreateAIAgent(
    name: "agui-client",
    description: "AG-UI Client Agent with frontend tools",
    tools: frontendTools);

AgentThread thread = agent.GetNewThread();
List<ChatMessage> messages =
[
    new(ChatRole.System, "You are a helpful assistant with access to client-side tools for location, sensors, and system information.")
];

Console.WriteLine("✅ Connected to AG-UI server!");
Console.WriteLine();
Console.WriteLine("💡 Features:");
Console.WriteLine("   • Type your message and press Enter");
Console.WriteLine("   • Responses stream in real-time");
Console.WriteLine("   • Frontend tools execute locally on the client");
Console.WriteLine("   • Backend tools execute on the server (if server supports them)");
Console.WriteLine("   • Tool calls are displayed with their parameters and results");
Console.WriteLine("   • Conversation context is maintained");
Console.WriteLine("   • Type ':q' or 'quit' to exit");
Console.WriteLine();
Console.WriteLine("💡 Try asking:");
Console.WriteLine("   • Where am I located?");
Console.WriteLine("   • What are my sensor readings?");
Console.WriteLine("   • What's my system information?");
Console.WriteLine("   • What's the weather in Paris? (if server has backend tools)");
Console.WriteLine();
Console.WriteLine(new string('═', 80));

try
{
    while (true)
    {
        // Get user input
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write("User (:q or quit to exit): ");
        Console.ResetColor();
        string? message = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(message))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("⚠️  Request cannot be empty.");
            Console.ResetColor();
            continue;
        }

        if (message is ":q" or "quit")
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("👋 Goodbye! Thanks for using AG-UI Client.");
            Console.ResetColor();
            break;
        }

        messages.Add(new ChatMessage(ChatRole.User, message));

        // Stream the response
        bool isFirstUpdate = true;
        string? threadId = null;
        string? runId = null;

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write("Assistant: ");
        Console.ResetColor();

        await foreach (AgentRunResponseUpdate update in agent.RunStreamingAsync(messages, thread))
        {
            ChatResponseUpdate chatUpdate = update.AsChatResponseUpdate();

            // First update indicates run started
            if (isFirstUpdate)
            {
                threadId = chatUpdate.ConversationId;
                runId = chatUpdate.ResponseId;
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"[Run Started - Thread: {chatUpdate.ConversationId}, Run: {chatUpdate.ResponseId}]");
                Console.ResetColor();
                isFirstUpdate = false;
            }

            // Display streaming content
            foreach (AIContent content in update.Contents)
            {
                switch (content)
                {
                    case TextContent textContent:
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write(textContent.Text);
                        Console.ResetColor();
                        break;

                    case FunctionCallContent functionCallContent:
                        Console.WriteLine(); // New line before tool call
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        Console.WriteLine($"🔧 [Tool Call: {functionCallContent.Name}]");
                        
                        // Display individual parameters
                        if (functionCallContent.Arguments != null)
                        {
                            foreach (var kvp in functionCallContent.Arguments)
                            {
                                Console.ForegroundColor = ConsoleColor.DarkMagenta;
                                Console.WriteLine($"   Parameter: {kvp.Key} = {kvp.Value}");
                            }
                        }
                        Console.ResetColor();
                        break;

                    case FunctionResultContent functionResultContent:
                        Console.ForegroundColor = ConsoleColor.Blue;
                        Console.WriteLine($"✓ [Tool Result - CallId: {functionResultContent.CallId}]");
                        
                        if (functionResultContent.Exception != null)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"   ❌ Exception: {functionResultContent.Exception.Message}");
                        }
                        else if (functionResultContent.Result != null)
                        {
                            // Format the result nicely
                            var resultStr = functionResultContent.Result.ToString();
                            if (resultStr != null && resultStr.Length > 200)
                            {
                                resultStr = resultStr.Substring(0, 200) + "... (truncated)";
                            }
                            Console.ForegroundColor = ConsoleColor.DarkCyan;
                            Console.WriteLine($"   Result: {resultStr}");
                        }
                        Console.ResetColor();
                        Console.WriteLine(); // New line after tool result
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write("Assistant: ");
                        Console.ResetColor();
                        break;

                    case ErrorContent errorContent:
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"\n❌ Error: {errorContent.Message}");
                        Console.ResetColor();
                        break;
                }
            }
        }

        Console.ResetColor();
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"[Run Finished - Thread: {threadId}]");
        Console.ResetColor();
    }
}
catch (HttpRequestException ex)
{
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"❌ Connection error: {ex.Message}");
    Console.WriteLine();
    Console.WriteLine("💡 Make sure the AG-UI server is running:");
    Console.WriteLine("   cd AGUI.Server");
    Console.WriteLine("   dotnet run");
    Console.WriteLine();
    Console.WriteLine("💡 Or use the launch script from solution root:");
    Console.WriteLine("   .\\start-agui.ps1");
    Console.ResetColor();
}
catch (Exception ex)
{
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"❌ An error occurred: {ex.Message}");
    Console.WriteLine();
    Console.WriteLine($"Stack trace:");
    Console.WriteLine(ex.StackTrace);
    Console.ResetColor();
}

Console.WriteLine();
Console.WriteLine(new string('═', 80));
Console.WriteLine("✅ AG-UI Client session ended.");
Console.WriteLine(new string('═', 80));

// ====================================
// Frontend Tool Implementations
// ====================================

/// <summary>
/// Get the user's simulated GPS location (frontend tool).
/// </summary>
[Description("Get the user's current GPS location from the client device")]
static string GetUserLocation()
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("   [Client] Executing GetUserLocation...");
    Console.ResetColor();

    // Simulate GPS reading
    var location = new
    {
        City = "Amsterdam",
        Country = "Netherlands",
        Latitude = 52.3676,
        Longitude = 4.9041,
        Accuracy = "10 meters"
    };

    return $"Amsterdam, Netherlands (Lat: {location.Latitude}°N, Lon: {location.Longitude}°E, Accuracy: {location.Accuracy})";
}

/// <summary>
/// Read simulated sensor data from client device (frontend tool).
/// </summary>
[Description("Read sensor data from the client device including temperature, humidity, and air quality")]
static string ReadClientSensors()
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("   [Client] Executing ReadClientSensors...");
    Console.ResetColor();

    // Simulate sensor readings
    var sensorData = new
    {
        Temperature = 22.5,
        TemperatureUnit = "Celsius",
        Humidity = 45.0,
        HumidityUnit = "%",
        AirQualityIndex = 75,
        AirQualityLevel = "Good",
        Timestamp = DateTime.Now
    };

    return $"Temperature: {sensorData.Temperature}°C, Humidity: {sensorData.Humidity}%, Air Quality: {sensorData.AirQualityLevel} (AQI: {sensorData.AirQualityIndex})";
}

/// <summary>
/// Get client system information (frontend tool).
/// </summary>
[Description("Get system information from the client including OS, machine name, and user")]
static string GetClientSystemInfo()
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("   [Client] Executing GetClientSystemInfo...");
    Console.ResetColor();

    // Get actual system information
    var systemInfo = new
    {
        OS = Environment.OSVersion.ToString(),
        MachineName = Environment.MachineName,
        UserName = Environment.UserName,
        ProcessorCount = Environment.ProcessorCount,
        Is64Bit = Environment.Is64BitOperatingSystem,
        DotNetVersion = Environment.Version.ToString(),
        CurrentDirectory = Environment.CurrentDirectory
    };

    return $"OS: {systemInfo.OS}, Machine: {systemInfo.MachineName}, User: {systemInfo.UserName}, " +
           $"Processors: {systemInfo.ProcessorCount}, .NET: {systemInfo.DotNetVersion}";
}
