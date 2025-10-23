// Copyright (c) Microsoft. All rights reserved.
// SPDX-License-Identifier: MIT

using Azure.AI.OpenAI;
using MAFPlayground.Utils;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Reflection;
using Microsoft.Extensions.AI;

namespace MAFPlayground.Samples;

/// <summary>
/// This sample demonstrates how to convert a workflow into an agent using .AsAgent(),
/// allowing you to interact with a workflow as if it were a single AI agent.
///
/// The workflow created here uses two language agents (French and English) to process
/// input concurrently and aggregate their responses. By converting this workflow to an
/// agent, you can:
/// - Use it in interactive conversations with thread-based context
/// - Stream responses just like a regular agent
/// - Compose it into larger workflows or agent systems
///
/// This pattern enables building complex multi-agent systems while presenting a simple,
/// unified agent interface to the caller.
/// </summary>
/// <remarks>
/// Pre-requisites:
/// - Foundational workflow and agent samples should be completed first.
/// - An Azure OpenAI chat completion deployment must be configured.
/// </remarks>
internal static class Sample10_WorkflowAsAgent
{
    public static async Task Execute()
    {
        Console.WriteLine("=== Sample 10: Workflow as Agent ===");
        Console.WriteLine("This workflow responds in both French and English concurrently.\n");

        // Set up the Azure OpenAI client using AIConfig
        var azureClient = new AzureOpenAIClient(AIConfig.Endpoint, AIConfig.KeyCredential);
        var deploymentName = "gpt-4o-mini";
        IChatClient chatClient = azureClient.GetChatClient(deploymentName).AsIChatClient();

        // Create the workflow that processes input using two language agents
        var workflow = await GetBilingualWorkflowAsync(chatClient).ConfigureAwait(false);

        // Visualize the underlying workflow structure
        WorkflowVisualizerTool.PrintAll(workflow, "Bilingual Workflow (before conversion to agent)");

        // Convert the workflow to an agent - this is the key capability!
        AIAgent workflowAgent = workflow.AsAgent(
            "bilingual-workflow-agent",
            "Agent that responds in both French and English");

        // Create a thread for conversation context
        AgentThread thread = workflowAgent.GetNewThread();

        Console.WriteLine("\n✨ The workflow has been converted to an agent!");
        Console.WriteLine("You can now interact with it like any other agent.\n");

        // Start an interactive loop to demonstrate the workflow-as-agent
        while (true)
        {
            Console.WriteLine();
            Console.Write("User (or 'exit' to quit): ");
            string? input = Console.ReadLine();
            
            if (string.IsNullOrWhiteSpace(input) || input.Equals("exit", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Exiting Sample 10.");
                break;
            }

            await ProcessInputAsync(workflowAgent, thread, input);
        }
    }

    /// <summary>
    /// Creates a workflow that uses two language agents to process input concurrently.
    /// </summary>
    /// <param name="chatClient">The chat client to use for the agents</param>
    /// <returns>A workflow that processes input using French and English agents</returns>
    private static ValueTask<Workflow<List<ChatMessage>>> GetBilingualWorkflowAsync(IChatClient chatClient)
    {
        // Create executors
        var startExecutor = new ConcurrentStartExecutor();
        var aggregationExecutor = new ConcurrentAggregationExecutor();
        
        // Create language agents
        AIAgent frenchAgent = GetLanguageAgent("French", chatClient);
        AIAgent englishAgent = GetLanguageAgent("English", chatClient);

        // Build the workflow: fan-out to both agents, then fan-in to aggregate
        return new WorkflowBuilder(startExecutor)
            .AddFanOutEdge(startExecutor, targets: [ frenchAgent, englishAgent])
            .AddFanInEdge(aggregationExecutor, sources: [frenchAgent, englishAgent ])
            .WithOutputFrom(aggregationExecutor)
            .BuildAsync<List<ChatMessage>>();
    }

    /// <summary>
    /// Creates a language agent for the specified target language.
    /// </summary>
    /// <param name="targetLanguage">The target language for responses</param>
    /// <param name="chatClient">The chat client to use for the agent</param>
    /// <returns>A ChatClientAgent configured for the specified language</returns>
    private static ChatClientAgent GetLanguageAgent(string targetLanguage, IChatClient chatClient) =>
        new(
            chatClient,
            instructions: $"You're a helpful assistant who always responds in {targetLanguage}.",
            name: $"{targetLanguage}Agent");

    /// <summary>
    /// Processes user input and displays streaming responses from the workflow-agent.
    /// Buffers updates by message ID to correctly display multiple interleaved responses.
    /// </summary>
    private static async Task ProcessInputAsync(AIAgent agent, AgentThread thread, string input)
    {
        Dictionary<string, List<AgentRunResponseUpdate>> buffer = new();
        
        await foreach (AgentRunResponseUpdate update in agent.RunStreamingAsync(input, thread).ConfigureAwait(false))
        {
            if (update.MessageId is null)
            {
                // Skip updates that don't have a message ID
                continue;
            }

            Console.Clear();

            // Buffer updates by message ID
            if (!buffer.TryGetValue(update.MessageId, out List<AgentRunResponseUpdate>? value))
            {
                value = new List<AgentRunResponseUpdate>();
                buffer[update.MessageId] = value;
            }
            value.Add(update);

            // Re-render all messages on each update to show interleaved streaming correctly
            foreach (var (messageId, segments) in buffer)
            {
                string combinedText = string.Concat(segments.Select(s => s.Text));
                Console.WriteLine($"{segments[0].AuthorName}: {combinedText}");
                Console.WriteLine();
            }
        }
    }

    /// <summary>
    /// Executor that starts the concurrent processing by sending messages to the agents.
    /// </summary>
    private sealed class ConcurrentStartExecutor : 
        ReflectingExecutor<ConcurrentStartExecutor>, 
        IMessageHandler<List<ChatMessage>>
    {
        public ConcurrentStartExecutor() : base("ConcurrentStartExecutor") { }

        /// <summary>
        /// Broadcasts the message to all connected agents and sends a turn token to start processing.
        /// </summary>
        public async ValueTask HandleAsync(List<ChatMessage> message, IWorkflowContext context, CancellationToken cancellationToken = default)
        {
            // Broadcast the message to all connected agents (they will queue it)
            await context.SendMessageAsync(message, cancellationToken: cancellationToken).ConfigureAwait(false);
            
            // Broadcast the turn token to kick off the agents
            await context.SendMessageAsync(new TurnToken(emitEvents: true), cancellationToken: cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Executor that aggregates the results from the concurrent agents.
    /// </summary>
    private sealed class ConcurrentAggregationExecutor : 
        ReflectingExecutor<ConcurrentAggregationExecutor>, 
        IMessageHandler<ChatMessage>
    {
        public ConcurrentAggregationExecutor() : base("ConcurrentAggregationExecutor") { }

        private readonly List<ChatMessage> _messages = new();

        /// <summary>
        /// Collects messages from the agents and yields output when both have responded.
        /// </summary>
        public async ValueTask HandleAsync(ChatMessage message, IWorkflowContext context, CancellationToken cancellationToken = default)
        {
            this._messages.Add(message);

            // When both agents have replied, aggregate and yield the combined output
            if (this._messages.Count == 2)
            {
                var formattedMessages = string.Join(Environment.NewLine, this._messages.Select(m => m.Text));
                await context.YieldOutputAsync(formattedMessages, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}