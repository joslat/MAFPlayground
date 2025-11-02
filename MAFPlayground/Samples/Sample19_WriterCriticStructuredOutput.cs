// SPDX-License-Identifier: LicenseRef-MAFPlayground-NPU-1.0-CH
// Copyright (c) 2025 Jose Luis

using System.ComponentModel;
using System.Text;
using System.Text.Json.Serialization;
using Azure.AI.OpenAI;
using MAFPlayground.Utils;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Reflection;
using Microsoft.Extensions.AI;

namespace MAFPlayground.Samples;

/// <summary>
/// Sample 19: Writer-Critic Iteration Workflow with Structured Output
/// 
/// Demonstrates the same iterative refinement loop as Sample17, but using
/// ChatResponseFormat.ForJsonSchema for the Critic's structured decision output.
/// 
/// This shows how to use OpenAI's Structured Output feature to guarantee
/// type-safe JSON responses from the Critic agent.
/// 
/// Workflow Flow:
/// ┌─────────────┐
/// │   Writer    │ → Creates/revises content
/// └──────┬──────┘
///        ↓
/// ┌──────────────┐
/// │   Critic     │ → Reviews and outputs STRUCTURED decision
/// └──────┬───────┘
///        ↓
///    [Decision]
///        ├─ Approved → Summary → [Output]
///        └─ Rejected → Writer (loop-back, max 3 iterations)
/// 
/// Key Features:
/// - Structured output for critic decisions (guaranteed JSON schema)
/// - Type-safe deserialization
/// - No manual JSON parsing needed
/// - Same workflow pattern as Sample17
/// </summary>
internal static class Sample19_WriterCriticStructuredOutput
{
    private const int MaxIterations = 3;

    // --------------------- Shared state ---------------------
    private sealed class FlowState
    {
        public int Iteration { get; set; } = 1;
        public List<ChatMessage> History { get; } = new();
    }

    private static class FlowStateShared
    {
        public const string Scope = "FlowStateScope";
        public const string Key = "singleton";
    }

    private static async Task<FlowState> ReadFlowStateAsync(IWorkflowContext context)
    {
        var state = await context.ReadStateAsync<FlowState>(FlowStateShared.Key, scopeName: FlowStateShared.Scope);
        return state ?? new FlowState();
    }

    private static ValueTask SaveFlowStateAsync(IWorkflowContext context, FlowState state)
        => context.QueueStateUpdateAsync(FlowStateShared.Key, state, scopeName: FlowStateShared.Scope);

    // --------------------- Structured Critic Decision ---------------------
    /// <summary>
    /// Structured output schema for the Critic's decision.
    /// Uses JsonPropertyName and Description attributes for OpenAI's JSON schema.
    /// </summary>
    [Description("Critic's review decision including approval status and feedback")]
    public sealed class CriticDecision
    {
        [JsonPropertyName("approved")]
        [Description("Whether the content is approved (true) or needs revision (false)")]
        public bool Approved { get; set; }

        [JsonPropertyName("feedback")]
        [Description("Specific feedback for improvements if not approved, empty if approved")]
        public string Feedback { get; set; } = "";

        // Non-JSON properties for workflow use
        [JsonIgnore]
        public string Content { get; set; } = "";

        [JsonIgnore]
        public int Iteration { get; set; }
    }

    // --------------------- Entry point ---------------------
    public static async Task Execute()
    {
        Console.WriteLine("=== Sample 19: Writer-Critic with Structured Output ===\n");
        Console.WriteLine($"Writer and Critic will iterate up to {MaxIterations} times until approval.\n");
        Console.WriteLine("✨ Using OpenAI Structured Output for guaranteed JSON schema compliance.\n");

        // Azure OpenAI setup
        var azureClient = new AzureOpenAIClient(AIConfig.Endpoint, AIConfig.KeyCredential);
        var deploymentName = "gpt-4o-mini";
        IChatClient chatClient = azureClient.GetChatClient(deploymentName).AsIChatClient();

        // Agents
        var writerAgent = GetWriterAgent(chatClient);
        var criticAgent = GetCriticAgent(chatClient); // Now uses structured output
        var summaryAgent = GetSummaryAgent(chatClient);

        // Executors
        var writerExec = new WriterExecutor(writerAgent);
        var criticExec = new CriticExecutor(criticAgent);
        var summaryExec = new SummaryExecutor(summaryAgent);

        // Build workflow
        var workflow = new WorkflowBuilder(writerExec)
            .AddEdge(writerExec, criticExec)
            .AddSwitch(criticExec, sw => sw
                .AddCase<CriticDecision>(cd => cd is not null && cd.Approved, summaryExec)
                .AddCase<CriticDecision>(cd => cd is not null && !cd.Approved, writerExec))
            .WithOutputFrom(summaryExec)
            .Build();

        WorkflowVisualizerTool.PrintAll(workflow, "Sample 19: Writer-Critic with Structured Output");

        // Execute
        Console.WriteLine("\n" + new string('=', 80));
        Console.WriteLine("TASK: Write a short blog post about AI ethics (200 words)");
        Console.WriteLine(new string('=', 80) + "\n");

        var initialMessage = new ChatMessage(ChatRole.User,
            "Write a 200-word blog post about AI ethics. Make it thoughtful and engaging.");

        await using StreamingRun run = await InProcessExecution.StreamAsync(workflow, initialMessage);

        await foreach (WorkflowEvent evt in run.WatchStreamAsync())
        {
            switch (evt)
            {
                case AgentRunUpdateEvent agentUpdate:
                    // Stream agent output in real-time
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
                    Console.WriteLine(output.Data);
                    Console.WriteLine();
                    Console.WriteLine(new string('=', 80));
                    break;
            }
        }

        Console.WriteLine("\n✅ Sample 19 Complete!\n");
        Console.WriteLine("Key Concepts Demonstrated:");
        Console.WriteLine("  ✓ Iterative Writer-Critic loop with conditional routing");
        Console.WriteLine("  ✓ Shared state for iteration tracking");
        Console.WriteLine($"  ✓ Max iteration cap ({MaxIterations}) for safety");
        Console.WriteLine("  ✓ ✨ Structured Output using ChatResponseFormat.ForJsonSchema");
        Console.WriteLine("  ✓ Type-safe JSON deserialization (no manual parsing)");
        Console.WriteLine("  ✓ Guaranteed schema compliance from OpenAI\n");
        
        Console.WriteLine("Compare with Sample 17:");
        Console.WriteLine("  • Sample 17: Manual JSON parsing in ParseDecision()");
        Console.WriteLine("  • Sample 19: Automatic structured output (this sample)");
        Console.WriteLine("  • Sample 19: No parsing errors, guaranteed schema match\n");
    }

    // --------------------- Agent factories ---------------------
    private static AIAgent GetWriterAgent(IChatClient chat) =>
        new ChatClientAgent(chat, """
            You are a skilled writer. Create clear, engaging content.
            If you receive feedback, carefully revise the content to address all concerns.
            Maintain the same topic and length requirements.
            """);

    private static AIAgent GetCriticAgent(IChatClient chat) =>
        new ChatClientAgent(chat, new ChatClientAgentOptions
        {
            Name = "Critic",
            Instructions = """
                You are a constructive critic. Review the content and provide specific feedback.
                
                You will alway try to provide improvement feedback unless the content is excellent.

                Output your decision in the following JSON format:
                - Set "approved" to true if the content is good, false if it needs revision
                - Provide "feedback" with specific improvements needed (empty string if approved)
                
                Be concise but specific in your feedback.
                """,
            ChatOptions = new()
            {
                // ✨ This is the key: ForJsonSchema enforces the structured output
                ResponseFormat = ChatResponseFormat.ForJsonSchema<CriticDecision>()
            }
        });

    private static AIAgent GetSummaryAgent(IChatClient chat) =>
        new ChatClientAgent(chat, """
            You present the final approved content to the user.
            Simply output the polished content - no additional commentary needed.
            """);

    // --------------------- Executors ---------------------

    private sealed class WriterExecutor :
        ReflectingExecutor<WriterExecutor>,
        IMessageHandler<ChatMessage, ChatMessage>,
        IMessageHandler<CriticDecision, ChatMessage>
    {
        private readonly AIAgent _agent;
        public WriterExecutor(AIAgent agent) : base("Writer") => _agent = agent;

        public async ValueTask<ChatMessage> HandleAsync(
            ChatMessage message,
            IWorkflowContext context,
            CancellationToken cancellationToken = default)
        {
            return await HandleAsyncCore(message, context, cancellationToken);
        }

        public async ValueTask<ChatMessage> HandleAsync(
            CriticDecision decision,
            IWorkflowContext context,
            CancellationToken cancellationToken = default)
        {
            var prompt = $"Revise the following content based on this feedback:\n\n" +
                        $"Feedback: {decision.Feedback}\n\n" +
                        $"Original Content:\n{decision.Content}";
            
            return await HandleAsyncCore(new ChatMessage(ChatRole.User, prompt), context, cancellationToken);
        }

        private async Task<ChatMessage> HandleAsyncCore(
            ChatMessage message,
            IWorkflowContext context,
            CancellationToken cancellationToken)
        {
            var state = await ReadFlowStateAsync(context);
            
            Console.WriteLine($"\n=== Writer (Iteration {state.Iteration}) ===\n");
            
            var sb = new StringBuilder();
            await foreach (var up in _agent.RunStreamingAsync(message, cancellationToken: cancellationToken))
            {
                if (!string.IsNullOrEmpty(up.Text))
                {
                    sb.Append(up.Text);
                    Console.Write(up.Text);
                }
            }
            Console.WriteLine("\n");

            var text = sb.ToString();
            state.History.Add(new ChatMessage(ChatRole.Assistant, text));
            await SaveFlowStateAsync(context, state);

            return new ChatMessage(ChatRole.User, text);
        }
    }

    private sealed class CriticExecutor :
        ReflectingExecutor<CriticExecutor>,
        IMessageHandler<ChatMessage, CriticDecision>
    {
        private readonly AIAgent _agent;
        public CriticExecutor(AIAgent agent) : base("Critic") => _agent = agent;

        public async ValueTask<CriticDecision> HandleAsync(
            ChatMessage message,
            IWorkflowContext context,
            CancellationToken cancellationToken = default)
        {
            var state = await ReadFlowStateAsync(context);
            
            Console.WriteLine($"=== Critic (Iteration {state.Iteration}) ===\n");

            // ✨ Use RunStreamingAsync to get streaming updates, then deserialize at the end
            var updates = _agent.RunStreamingAsync(message, cancellationToken: cancellationToken);

            // Stream the output in real-time (for any rationale/explanation)
            await foreach (var up in updates)
            {
                if (!string.IsNullOrEmpty(up.Text))
                {
                    Console.Write(up.Text);
                }
            }
            Console.WriteLine("\n");

            // ✨ Convert the stream to a response and deserialize the structured output
            var response = await updates.ToAgentRunResponseAsync();
            var decision = response.Deserialize<CriticDecision>(System.Text.Json.JsonSerializerOptions.Web);

            // Safety: approve if max iterations reached
            if (!decision.Approved && state.Iteration >= MaxIterations)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"⚠️ Max iterations ({MaxIterations}) reached - auto-approving");
                Console.ResetColor();
                decision.Approved = true;
                decision.Feedback = "";
            }

            // Increment iteration ONLY if rejecting (will loop back to Writer)
            if (!decision.Approved)
            {
                state.Iteration++;
            }

            // Store the decision in history
            state.History.Add(new ChatMessage(ChatRole.Assistant, 
                $"Approved: {decision.Approved}, Feedback: {decision.Feedback}"));
            await SaveFlowStateAsync(context, state);

            // Populate workflow-specific fields
            decision.Content = message.Text ?? "";
            decision.Iteration = state.Iteration;

            return decision;
        }
    }

    private sealed class SummaryExecutor :
        ReflectingExecutor<SummaryExecutor>,
        IMessageHandler<CriticDecision, ChatMessage>
    {
        private readonly AIAgent _agent;
        public SummaryExecutor(AIAgent agent) : base("Summary") => _agent = agent;

        public async ValueTask<ChatMessage> HandleAsync(
            CriticDecision decision,
            IWorkflowContext context,
            CancellationToken cancellationToken = default)
        {
            Console.WriteLine("=== Summary ===\n");

            var prompt = $"Present this approved content:\n\n{decision.Content}";

            var sb = new StringBuilder();
            await foreach (var up in _agent.RunStreamingAsync(new ChatMessage(ChatRole.User, prompt), cancellationToken: cancellationToken))
            {
                if (!string.IsNullOrEmpty(up.Text))
                {
                    sb.Append(up.Text);
                }
            }

            return new ChatMessage(ChatRole.Assistant, sb.ToString());
        }
    }
}