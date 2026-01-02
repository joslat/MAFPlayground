// SPDX-License-Identifier: LicenseRef-MAFPlayground-NPU-1.0-CH
// Copyright (c) 2025 Jose Luis Latorre

using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.DevUI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CourseSamples;

/// <summary>
/// Helper class for running agents in DevUI mode.
/// Provides a consistent, DRY approach to launching any agent with the DevUI web interface.
/// 
/// Note: DevUI uses ASP.NET Core's DI container to create agents at runtime.
/// Pass agent specifications (name, instructions, tools) and the helper handles registration.
/// </summary>
public static class DevUIHelper
{
    /// <summary>
    /// Default port for DevUI server
    /// </summary>
    public const int DefaultPort = 5000;

    /// <summary>
    /// Creates the default chat client using AIConfig
    /// </summary>
    private static IChatClient CreateChatClient()
    {
        var azureClient = new AzureOpenAIClient(AIConfig.Endpoint, AIConfig.KeyCredential);
        return azureClient
            .GetChatClient(AIConfig.ModelDeployment)
            .AsIChatClient();
    }

    /// <summary>
    /// Runs a single agent in DevUI mode (simple agent without tools)
    /// </summary>
    /// <param name="agentName">Name of the agent</param>
    /// <param name="agentInstructions">System instructions for the agent</param>
    /// <param name="port">Port to run on (default: 5000)</param>
    public static void RunWithDevUI(string agentName, string agentInstructions, int port = DefaultPort)
    {
        RunWithDevUI(new[] { new AgentSpec(agentName, agentInstructions) }, port);
    }

    /// <summary>
    /// Runs a single agent in DevUI mode (agent with tools)
    /// </summary>
    /// <param name="spec">Agent specification including name, instructions, and optional tools</param>
    /// <param name="port">Port to run on (default: 5000)</param>
    public static void RunWithDevUI(AgentSpec spec, int port = DefaultPort)
    {
        RunWithDevUI(new[] { spec }, port);
    }

    /// <summary>
    /// Runs multiple agents in DevUI mode
    /// </summary>
    /// <param name="agents">Collection of agent specifications</param>
    /// <param name="port">Port to run on (default: 5000)</param>
    public static void RunWithDevUI(IEnumerable<AgentSpec> agents, int port = DefaultPort)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development
        });

        // Configure Azure OpenAI chat client
        var chatClient = CreateChatClient();
        builder.Services.AddChatClient(chatClient);

        // Register all agents
        Console.WriteLine("\n🤖 Registering agents:");
        var agentList = agents.ToList();
        foreach (var spec in agentList)
        {
            if (spec.Tools?.Any() == true)
            {
                builder.AddAIAgent(spec.Name, spec.Instructions, spec.Tools);
                Console.WriteLine($"   ✓ {spec.Name} (with {spec.Tools.Count()} tools)");
            }
            else
            {
                builder.AddAIAgent(spec.Name, spec.Instructions);
                Console.WriteLine($"   ✓ {spec.Name}");
            }
        }

        // Configure DevUI services
        builder.Services.AddOpenAIResponses();
        builder.Services.AddOpenAIConversations();

        var app = builder.Build();

        // Map endpoints
        app.MapOpenAIResponses();
        app.MapOpenAIConversations();
        app.MapDevUI();

        // Display info
        PrintDevUIInfo($"http://localhost:{port}", agentList);

        app.Run($"http://localhost:{port}");
    }

    private static void PrintDevUIInfo(string url, List<AgentSpec> agents)
    {
        Console.WriteLine("\n" + new string('═', 80));
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("✅ DevUI Server Started Successfully!");
        Console.ResetColor();
        Console.WriteLine(new string('═', 80));

        Console.WriteLine("\n📊 Available Endpoints:");
        Console.WriteLine($"   • DevUI Interface:           {url}/devui");
        Console.WriteLine($"   • OpenAI Responses API:      {url}/v1/responses");
        Console.WriteLine($"   • OpenAI Conversations API:  {url}/v1/conversations");

        Console.WriteLine("\n🤖 Registered Agents:");
        for (int i = 0; i < agents.Count; i++)
        {
            var toolInfo = agents[i].Tools?.Any() == true 
                ? $" ({agents[i].Tools!.Count()} tools)" 
                : "";
            Console.WriteLine($"   {i + 1}. {agents[i].Name}{toolInfo}");
        }

        Console.WriteLine("\n💡 How to Use:");
        Console.WriteLine($"   1. Open your browser to: {url}/devui");
        Console.WriteLine("   2. Select an agent from the dropdown");
        Console.WriteLine("   3. Type your message and interact with the agent");
        Console.WriteLine("   4. View traces, metrics, and logs in real-time");

        Console.WriteLine("\n⚠️  Press Ctrl+C to stop the server");
        Console.WriteLine(new string('═', 80) + "\n");
    }
}

/// <summary>
/// Specification for creating an agent in DevUI.
/// This is a lightweight DTO that holds the agent configuration.
/// </summary>
public class AgentSpec
{
    /// <summary>
    /// Creates a simple agent specification without tools
    /// </summary>
    public AgentSpec(string name, string instructions)
    {
        Name = name;
        Instructions = instructions;
    }

    /// <summary>
    /// Creates an agent specification with tools
    /// </summary>
    public AgentSpec(string name, string instructions, IEnumerable<AITool> tools)
    {
        Name = name;
        Instructions = instructions;
        Tools = tools;
    }

    /// <summary>
    /// The agent's name (used for identification in DevUI)
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The system instructions for the agent
    /// </summary>
    public string Instructions { get; }

    /// <summary>
    /// Optional tools available to the agent
    /// </summary>
    public IEnumerable<AITool>? Tools { get; }
}
