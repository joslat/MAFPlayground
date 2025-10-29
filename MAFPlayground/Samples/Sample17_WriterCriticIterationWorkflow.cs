// SPDX-License-Identifier: LicenseRef-MAFPlayground-NPU-1.0-CH
// Copyright (c) 2025 Jose Luis

using System.Text;
using System.Text.Json;
using Azure.AI.OpenAI;
using MAFPlayground.Utils;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Reflection;
using Microsoft.Extensions.AI;

namespace MAFPlayground.Samples;

/// <summary>
/// Sample 17: Writer-Critic Iteration Workflow
/// 
/// Demonstrates a clean iterative refinement loop between Writer and Critic agents.
/// The workflow continues until the Critic approves or max iterations is reached.
/// 
/// Workflow Flow:
/// ┌─────────────┐
/// │   Writer    │ → Creates/revises content
/// └──────┬──────┘
///        ↓
/// ┌──────────────┐
/// │   Critic     │ → Reviews and decides
/// └──────┬───────┘
///        ↓
///    [Decision]
///        ├─ Approved → Summary → [Output]
///        └─ Rejected → Writer (loop-back, max 3 iterations)
/// 
/// Key Features:
/// - Simple ChatMessage input/output
/// - Shared state for iteration tracking
/// - Clean conditional routing
/// - Streaming support for real-time feedback
/// </summary>
internal static class Sample17_WriterCriticIterationWorkflow
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

    // --------------------- Critic decision ---------------------
    public sealed class CriticDecision
    {
        public bool Approved { get; set; }
        public string Feedback { get; set; } = "";
        public string Content { get; set; } = "";
        public int Iteration { get; set; }
    }

    // --------------------- Entry point ---------------------
    public static async Task Execute()
    {
        Console.WriteLine("=== Sample 17: Writer-Critic Iteration Workflow ===\n");
        Console.WriteLine($"Writer and Critic will iterate up to {MaxIterations} times until approval.\n");

        // Azure OpenAI setup
        var azureClient = new AzureOpenAIClient(AIConfig.Endpoint, AIConfig.KeyCredential);
        var deploymentName = AIConfig.ModelDeployment;
        IChatClient chatClient = azureClient.GetChatClient(deploymentName).AsIChatClient();

        // Agents
        var writerAgent = GetWriterAgent(chatClient);
        var criticAgent = GetCriticAgent(chatClient);
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

        // Note: the switch could have been done with conditional edges directly, but it is not so explicit.
        //.AddEdge<CriticDecision>(criticExec, summaryExec, condition: cd => cd is not null && cd.Approved)
        //.AddEdge<CriticDecision>(criticExec, writerExec, condition: cd => cd is not null && !cd.Approved)

        WorkflowVisualizerTool.PrintAll(workflow, "Sample 17: Writer-Critic Iteration Workflow");

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

        Console.WriteLine("\n✅ Sample 17 Complete!\n");
        Console.WriteLine("Key Concepts Demonstrated:");
        Console.WriteLine("  ✓ Iterative Writer-Critic loop with conditional routing");
        Console.WriteLine("  ✓ Shared state for iteration tracking");
        Console.WriteLine($"  ✓ Max iteration cap ({MaxIterations}) for safety");
        Console.WriteLine("  ✓ Clean ChatMessage flow throughout");
        Console.WriteLine("  ✓ Streaming support for real-time feedback\n");
    }

    // --------------------- Agent factories ---------------------
    private static AIAgent GetWriterAgent(IChatClient chat) =>
        new ChatClientAgent(chat, """
            You are a skilled writer. Create clear, engaging content.
            If you receive feedback, carefully revise the content to address all concerns.
            Maintain the same topic and length requirements.
            """);

    private static AIAgent GetCriticAgent(IChatClient chat) =>
        new ChatClientAgent(chat, """
            You are a constructive critic. Review the content and provide specific feedback.
            
            At the end, output EXACTLY one JSON line:
            {"approved":true,"feedback":""} if the content is good
            {"approved":false,"feedback":"<specific improvements needed>"} if revisions are needed
            
            Be concise but specific in your feedback.
            """);

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

        // Initial writing
        public async ValueTask<ChatMessage> HandleAsync(
            ChatMessage message,
            IWorkflowContext context,
            CancellationToken cancellationToken = default)
        {
            // Just pass through the original message
            return await HandleAsyncCore(message, context, cancellationToken);
        }

        // Revision based on critic feedback
        public async ValueTask<ChatMessage> HandleAsync(
            CriticDecision decision,
            IWorkflowContext context,
            CancellationToken cancellationToken = default)
        {
            // Compose message with feedback for the agent
            var prompt = $"Revise the following content based on this feedback:\n\n" +
                        $"Feedback: {decision.Feedback}\n\n" +
                        $"Original Content:\n{decision.Content}";
            
            return await HandleAsyncCore(new ChatMessage(ChatRole.User, prompt), context, cancellationToken);
        }

        // Shared core implementation
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

            var full = sb.ToString();
            var (approved, feedback) = ParseDecision(full);

            // Safety: approve if max iterations reached
            if (!approved && state.Iteration >= MaxIterations)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"⚠️ Max iterations ({MaxIterations}) reached - auto-approving");
                Console.ResetColor();
                approved = true;
                feedback = "";
            }

            // Increment iteration ONLY if rejecting (will loop back to Writer)
            if (!approved)
            {
                state.Iteration++;
            }

            state.History.Add(new ChatMessage(ChatRole.Assistant, StripTrailingJson(full)));
            await SaveFlowStateAsync(context, state);

            return new CriticDecision
            {
                Approved = approved,
                Feedback = feedback,
                Content = message.Text ?? "",
                Iteration = state.Iteration
            };
        }

        private static (bool approved, string feedback) ParseDecision(string full)
        {
            string? lastJson = null;
            foreach (var ln in full.Replace("\r\n", "\n").Split('\n').Reverse())
            {
                var t = ln.Trim();
                if (t.StartsWith("{") && t.EndsWith("}")) { lastJson = t; break; }
            }

            if (lastJson is null)
            {
                if (full.Contains("APPROVE", StringComparison.OrdinalIgnoreCase))
                    return (true, "");
                return (false, "Missing approval decision.");
            }

            try
            {
                using var doc = JsonDocument.Parse(lastJson);
                var ok = doc.RootElement.GetProperty("approved").GetBoolean();
                var fb = doc.RootElement.TryGetProperty("feedback", out var el) ? el.GetString() ?? "" : "";
                return (ok, fb);
            }
            catch
            {
                return (false, "Malformed approval JSON.");
            }
        }

        private static string StripTrailingJson(string text)
        {
            var lines = text.Replace("\r\n", "\n").Split('\n');
            if (lines.Length == 0) return text;
            var last = lines[^1].Trim();
            if (last.StartsWith("{") && last.EndsWith("}"))
                return string.Join("\n", lines[..^1]);
            return text;
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