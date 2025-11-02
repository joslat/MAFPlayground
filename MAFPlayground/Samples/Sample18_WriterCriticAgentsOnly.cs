// SPDX-License-Identifier: LicenseRef-MAFPlayground-NPU-1.0-CH
// Copyright (c) 2025 Jose Luis

using Azure.AI.OpenAI;
using MAFPlayground.Utils;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace MAFPlayground.Samples;

/// <summary>
/// Sample 18: Writer-Critic Iteration Workflow (Agents Only)
/// 
/// IMPORTANT: This sample shows a pure Agent workflow that bifurcate based on the text output, implementing a CRITIC workflow pattern.
/// this is not working due that the Conditional Edge is not being properly executed when the switch cases are based on ChatMessage content.
/// And the switch-case will be hit multiple times, once per ChatMessage issued, which triggers that the edge is not properly executed.
/// 
/// Demonstrates the same iterative refinement loop as Sample17, but using ONLY agents
/// without custom executors. The workflow routes based on the Critic's text output.
/// 
/// Workflow Flow:
/// ┌─────────────┐
/// │   Writer    │ → Creates/revises content
/// └──────┬──────┘
///        ↓
/// ┌──────────────┐
/// │   Critic     │ → Reviews and outputs decision
/// └──────┬───────┘
///        ↓
///    [Decision - based on text]
///        ├─ Contains "APPROVE" → Summary → [Output]
///        └─ Otherwise → Writer (loop-back, max 3 iterations)
/// 
/// Key Features:
/// - Pure agent workflow (no custom executors)
/// - Routing based on ChatMessage content
/// - Simpler than Sample17 but less type-safe
/// - Shows alternative approach for agent-only scenarios
/// </summary>
internal static class Sample18_WriterCriticAgentsOnly
{
    private const int MaxIterations = 3;

    public static async Task Execute()
    {
        Console.WriteLine("=== Sample 18: Writer-Critic Iteration Workflow (Agents Only) ===\n");
        Console.WriteLine($"Pure agent workflow - routing based on Critic's text output.\n");
        Console.WriteLine($"Max iterations: {MaxIterations}\n");

        // Azure OpenAI setup
        var azureClient = new AzureOpenAIClient(AIConfig.Endpoint, AIConfig.KeyCredential);
        var deploymentName = AIConfig.ModelDeployment;
        IChatClient chatClient = azureClient.GetChatClient(deploymentName).AsIChatClient();

        // Create agents - using the simpler constructor pattern
        var writerAgent = GetWriterAgent(chatClient);
        var criticAgent = GetCriticAgent(chatClient);
        var summaryAgent = GetSummaryAgent(chatClient);

        // Build workflow - pure agents, no executors
        var workflow = new WorkflowBuilder(writerAgent)
            .AddEdge(writerAgent, criticAgent)
            .AddSwitch(criticAgent, sw => sw
                // ISSUE: This switch-case will be hit multiple times, once per ChatMessage issued
                // which triggers that the edge is not properly executed.
                .AddCase<ChatMessage>(
                    msg => msg is not null &&
                           ContainsApproval(msg.Text ?? ""),
                    summaryAgent)
                .AddCase<ChatMessage>(
                    msg => msg is not null &&
                           !ContainsApproval(msg.Text ?? ""),
                    writerAgent))
            .WithOutputFrom(summaryAgent)
            .Build();

        WorkflowVisualizerTool.PrintAll(workflow, "Sample 18: Writer-Critic Agents Only");

        // Execute
        Console.WriteLine("\n" + new string('=', 80));
        Console.WriteLine("TASK: Write a short blog post about AI ethics (200 words)");
        Console.WriteLine(new string('=', 80) + "\n");

        var initialMessages = new List<ChatMessage>
        {
            new(ChatRole.User, "Write a 200-word blog post about AI ethics. Make it thoughtful and engaging.")
        };

        await using StreamingRun run = await InProcessExecution.StreamAsync(workflow, initialMessages);

        // FIX: Send TurnToken to start the workflow!
        await run.TrySendMessageAsync(new TurnToken(emitEvents: true));

        string? currentAgent = null;
        int iterationCount = 0;

        await foreach (WorkflowEvent evt in run.WatchStreamAsync())
        {
            switch (evt)
            {
                case AgentRunUpdateEvent agentUpdate:
                    // Detect agent changes
                    if (agentUpdate.Update.AuthorName != currentAgent)
                    {
                        currentAgent = agentUpdate.Update.AuthorName;
                        
                        if (currentAgent == "Writer")
                        {
                            iterationCount++;
                            Console.WriteLine($"\n=== Writer (Iteration {iterationCount}) ===\n");
                        }
                        else if (currentAgent == "Critic")
                        {
                            Console.WriteLine($"\n=== Critic (Iteration {iterationCount}) ===\n");
                        }
                        else if (currentAgent == "Summary")
                        {
                            Console.WriteLine("\n=== Summary ===\n");
                        }
                    }

                    // Stream agent output
                    if (!string.IsNullOrEmpty(agentUpdate.Update.Text))
                    {
                        Console.Write(agentUpdate.Update.Text);
                    }
                    break;

                case WorkflowOutputEvent output:
                    Console.WriteLine("\n\n" + new string('=', 80));
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("✅ FINAL APPROVED CONTENT");
                    Console.ResetColor();
                    Console.WriteLine(new string('=', 80));
                    Console.WriteLine();
                    
                    if (output.Data is List<ChatMessage> messages && messages.Count > 0)
                    {
                        Console.WriteLine(messages.Last().Text);
                    }
                    
                    Console.WriteLine();
                    Console.WriteLine(new string('=', 80));
                    break;
            }
        }

        Console.WriteLine("\n✅ Sample 18 Complete!\n");
        Console.WriteLine("Key Concepts Demonstrated:");
        Console.WriteLine("  ✓ Pure agent workflow (no custom executors)");
        Console.WriteLine("  ✓ Routing based on ChatMessage text content");
        Console.WriteLine("  ✓ Simpler implementation but less type-safe");
        Console.WriteLine($"  ✓ Max iteration cap ({MaxIterations}) enforced by Critic");
        Console.WriteLine("  ✓ Alternative approach for agent-only scenarios\n");    }

    // --------------------- Helper method ---------------------
    
    /// <summary>
    /// Checks if the text contains approval keywords.
    /// </summary>
    private static bool ContainsApproval(string text)
    {
        bool approval =  text.Contains("APPROVE", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("\"approved\":true", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("\"approved\": true", StringComparison.OrdinalIgnoreCase);
        
        return approval;
    }

    // --------------------- Agent factories ---------------------
    
    private static ChatClientAgent GetWriterAgent(IChatClient chat) =>
        new ChatClientAgent(
            chat,
            name: "Writer",
            instructions: """
                You are a skilled writer. Create clear, engaging content.
                
                IMPORTANT: The conversation history will show you any previous feedback.
                - If this is your first message, write original content based on the user's request.
                - If there's criticism in the conversation history, revise your previous content to address it.
                
                Maintain the same topic and length requirements.
                Output ONLY the content - no meta-commentary.
                """
        );

    private static ChatClientAgent GetCriticAgent(IChatClient chat) =>
        new ChatClientAgent(
            chat,
            name: "Critic",
            instructions: $"""
                You are a constructive critic. Review the most recent content from the Writer.
                
                IMPORTANT: Track iterations - we can iterate up to {MaxIterations} times.
                Look at the conversation history to count how many times you've reviewed.

                Your goal is to ensure high-quality content. For this you will always try to seee 
                improvements, unless the content is clearly excellent. Your goal will be to identify issues
                and suggest specific ways to solve those issues as well as to assess if there is room for 
                improvement and to suggest specific improvements.
                
                If the content is good (or if this is iteration {MaxIterations}), output:
                "APPROVE: The content is ready."
                
                If revisions are needed (and we haven't reached max iterations), output:
                "REVISE: [specific improvements needed]"
                
                Be concise but specific in your feedback.
                """
        )
        {

        };
    private static ChatClientAgent GetSummaryAgent(IChatClient chat) =>
        new ChatClientAgent(
            chat,
            name: "Summary",
            instructions: """
                You present the final approved content to the user.
                Look at the conversation history and find the Writer's latest approved content.
                Extract just the actual content (not approval messages or feedback).
                Present it cleanly - no additional commentary needed.
                """
        );
}