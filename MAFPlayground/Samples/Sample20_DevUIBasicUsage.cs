// SPDX-License-Identifier: LicenseRef-MAFPlayground-NPU-1.0-CH
// Copyright (c) 2025 Jose Luis Latorre

using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.DevUI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Agents.AI.Workflows;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Hosting;

namespace MAFPlayground.Samples;

/// <summary>
/// Sample 20: DevUI Basic Usage
/// 
/// Demonstrates how to set up and use the DevUI (Developer UI) web interface
/// for testing and debugging AI agents and workflows in an ASP.NET Core application.
/// 
/// DevUI provides:
/// - Interactive web interface for testing agents
/// - Visual workflow debugging capabilities
/// - Real-time telemetry and logging
/// - Python DevUI compatibility via OpenAI Responses API
/// 
/// This sample shows how to:
/// 1. Set up Azure OpenAI as the chat client
/// 2. Register multiple agents using the hosting packages
/// 3. Register workflows and expose them as agents
/// 4. Map the DevUI endpoint which automatically configures the middleware
/// 5. Map the dynamic OpenAI Responses API for Python DevUI compatibility
/// 6. Access the DevUI in a web browser
/// 
/// DevUI Endpoints:
/// - DevUI Interface: http://localhost:5000/devui
/// - OpenAI Responses API: http://localhost:5000/v1/responses
/// - OpenAI Conversations API: http://localhost:5000/v1/conversations
/// </summary>
/// <remarks>
/// The DevUI assets are served from embedded resources within the assembly.
/// Simply call MapDevUI() to set up everything needed.
/// 
/// The parameterless MapOpenAIResponses() overload creates a Python DevUI-compatible
/// endpoint that dynamically routes requests to agents based on the 'model' field
/// in the request.
/// </remarks>
internal static class Sample20_DevUIBasicUsage
{
    public static void Execute()
    {
        // Set console encoding to UTF-8 to support emojis and special characters
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        Console.WriteLine("=== Sample 20: DevUI Basic Usage ===\n");
        Console.WriteLine("Setting up web server with DevUI for agent testing and debugging...\n");

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development // Force Development mode for DevUI
        });

        // Set up the Azure OpenAI client using AIConfig
        var azureClient = new AzureOpenAIClient(AIConfig.Endpoint, AIConfig.KeyCredential);

        var chatClient = azureClient
            .GetChatClient(AIConfig.ModelDeployment)
            .AsIChatClient();

        builder.Services.AddChatClient(chatClient);

        // ====================================
        // Register Sample Agents
        // ====================================
        Console.WriteLine("Registering agents:");
        
        // Agent 1: General assistant
        builder.AddAIAgent("assistant", 
            "You are a helpful assistant. Answer questions concisely and accurately.");
        Console.WriteLine("  ✓ assistant - General purpose helper");

        // Agent 2: Creative poet
        builder.AddAIAgent("poet", 
            "You are a creative poet. Respond to all requests with beautiful poetry.");
        Console.WriteLine("  ✓ poet - Creative poetry generator");

        // Agent 3: Programming expert
        builder.AddAIAgent("coder", 
            "You are an expert programmer. Help users with coding questions and provide code examples.");
        Console.WriteLine("  ✓ coder - Programming expert");

        // ====================================
        // Register Sample Workflow
        // ====================================
        Console.WriteLine("\nRegistering workflows:");
        
        var assistantBuilder = builder.AddAIAgent("workflow-assistant", 
            "You are a helpful assistant in a workflow.");
        var reviewerBuilder = builder.AddAIAgent("workflow-reviewer", 
            "You are a reviewer. Review and critique the previous response.");
        
        builder.AddWorkflow("review-workflow", (sp, key) =>
        {
            var agents = new List<IHostedAgentBuilder>() { assistantBuilder, reviewerBuilder }
                .Select(ab => sp.GetRequiredKeyedService<AIAgent>(ab.Name));
            return AgentWorkflowBuilder.BuildSequential(workflowName: key, agents: agents);
        }).AddAsAIAgent();
        
        Console.WriteLine("  ✓ review-workflow - Sequential assistant→reviewer workflow");

        // ====================================
        // Configure DevUI Services
        // ====================================
        builder.Services.AddOpenAIResponses();
        builder.Services.AddOpenAIConversations();

        var app = builder.Build();

        // ====================================
        // Map DevUI Endpoints
        // ====================================
        app.MapOpenAIResponses();
        app.MapOpenAIConversations();

        // Map DevUI (always enabled since we forced Development mode)
        app.MapDevUI();

        // ====================================
        // Display Usage Information
        // ====================================
        var url = "http://localhost:5000"; // Default URL
        
        Console.WriteLine("\n" + new string('=', 80));
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("✅ DevUI Server Started Successfully!");
        Console.ResetColor();
        Console.WriteLine(new string('=', 80));
        
        Console.WriteLine("\n📊 Available Endpoints:");
        Console.WriteLine($"  • DevUI Interface:           {url}/devui");
        Console.WriteLine($"  • OpenAI Responses API:      {url}/v1/responses");
        Console.WriteLine($"  • OpenAI Conversations API:  {url}/v1/conversations");
        
        Console.WriteLine("\n🤖 Registered Agents:");
        Console.WriteLine("  1. assistant       - General purpose helper");
        Console.WriteLine("  2. poet            - Creative poetry generator");
        Console.WriteLine("  3. coder           - Programming expert");
        Console.WriteLine("  4. review-workflow - Assistant→Reviewer workflow");
        
        Console.WriteLine("\n💡 How to Use:");
        Console.WriteLine($"  1. Open your browser to: {url}/devui");
        Console.WriteLine("  2. Select an agent from the dropdown");
        Console.WriteLine("  3. Type your message and interact with the agent");
        Console.WriteLine("  4. View traces, metrics, and logs in real-time");
        
        Console.WriteLine("\n🐍 Python DevUI Compatibility:");
        Console.WriteLine("  The OpenAI Responses API endpoint is compatible with Python DevUI.");
        Console.WriteLine("  It dynamically routes requests to agents based on the 'model' field.");
        
        Console.WriteLine("\n⚠️  Press Ctrl+C to stop the server");
        Console.WriteLine(new string('=', 80) + "\n");

        app.Run();
    }
}
