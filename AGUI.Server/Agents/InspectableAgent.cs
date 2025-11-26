// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
// Modified for MAFPlayground by Jose Luis Latorre

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace AGUI.Server.Agents;

/// <summary>
/// Inspectable AI agent that displays frontend tools sent by the client.
/// This agent uses middleware to inspect and log the tools available in each run.
/// Based on BasicAgent with added middleware for tool inspection.
/// </summary>
public static class InspectableAgent
{
    public static AIAgent Create(IChatClient chatClient)
    {
        // Create a basic agent first
        var baseAgent = chatClient.CreateAIAgent(
            name: "InspectableAssistant",
            instructions: "You are a helpful assistant that can use client-side tools when available. " +
                         "When tools are available, you can use them to provide more personalized responses based on the user's local environment.");

        // Wrap with middleware to inspect tools
        var inspectableAgent = baseAgent
            .AsBuilder()
            .Use(runFunc: null, runStreamingFunc: InspectToolsMiddleware)
            .Build();

        return inspectableAgent;
    }

    public static string GetDescription()
    {
        return "Inspectable assistant with frontend tool support (client-side tools)";
    }

    /// <summary>
    /// Middleware that inspects and logs tools available in each agent run.
    /// This demonstrates how to access frontend tools sent by the client.
    /// </summary>
    private static async IAsyncEnumerable<AgentRunResponseUpdate> InspectToolsMiddleware(
        IEnumerable<ChatMessage> messages,
        AgentThread? thread,
        AgentRunOptions? options,
        AIAgent innerAgent,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // Access the tools from ChatClientAgentRunOptions
        if (options is ChatClientAgentRunOptions chatOptions)
        {
            IList<AITool>? tools = chatOptions.ChatOptions?.Tools;
            if (tools != null && tools.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"\n[Middleware] Frontend tools available for this run: {tools.Count}");
                foreach (AITool tool in tools)
                {
                    // AITool has a ToString() that shows the tool name
                    Console.WriteLine($"  • {tool}");
                }
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine("\n[Middleware] No frontend tools registered by client");
                Console.ResetColor();
            }
        }

        // Pass through to the inner agent
        await foreach (AgentRunResponseUpdate update in innerAgent.RunStreamingAsync(messages, thread, options, cancellationToken))
        {
            yield return update;
        }
    }
}
