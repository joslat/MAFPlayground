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

internal static class Sample16_ChatWithWorkflow
{
    // --------------------- Shared state kept minimal (history for Summary) ---------------------
    private sealed class FlowState
    {
        public List<ChatMessage> History { get; } = new();
    }

    // Shared state scope/key
    private static class FlowStateShared
    {
        public const string Scope = "FlowStateScope";
        public const string Key = "singleton";
    }

    private static async Task<FlowState> ReadFlowStateAsync(IWorkflowContext context, CancellationToken cancellationToken)
    {
        var state = await context.ReadStateAsync<FlowState>(FlowStateShared.Key, scopeName: FlowStateShared.Scope);
        return state ?? new FlowState();
    }

    private static ValueTask SaveFlowStateAsync(IWorkflowContext context, FlowState state, CancellationToken cancellationToken)
        => context.QueueStateUpdateAsync(FlowStateShared.Key, state, scopeName: FlowStateShared.Scope);

    // --------------------- Routing contracts (typed) ---------------------
    public enum WorkflowPath { Exit, Writer, Direct }

    public interface IOutputContent { string GetText(); }

    public sealed class RoutingToken : IOutputContent
    {
        public WorkflowPath Path { get; set; }
        public string ManagerResponse { get; set; } = "";
        public ChatMessage OriginalMessage { get; set; } = new(ChatRole.User, "");
        public string GetText() => ManagerResponse;
    }

    public sealed class WriterTask
    {
        public string Prompt { get; set; } = "";
        public string? Feedback { get; set; }
        public int Iteration { get; set; } = 1;
    }

    public sealed class WrittenContent : IOutputContent
    {
        public string Text { get; set; } = "";
        public int Iteration { get; set; }
        public string GetText() => Text;
    }

    public sealed class CriticDecision : IOutputContent
    {
        public bool Approved { get; set; }
        public string Feedback { get; set; } = "";
        public string Content { get; set; } = "";
        public int Iteration { get; set; }
        public string GetText() => Approved ? Content : Feedback;
    }

    // --------------------- UI events ---------------------
    public sealed class ExecutorStartedEvent : WorkflowEvent
    {
        public string Name { get; init; } = "";
    }

    public sealed class DisplayUpdateEvent : WorkflowEvent
    {
        public string Source { get; init; } = "";
        public string Chunk { get; init; } = "";
    }

    public sealed class ReadyForInputEvent : WorkflowEvent { }
    public sealed class TerminateWorkflowEvent : WorkflowEvent { }

    // --------------------- Entry (host) ---------------------
    public static async Task Execute()
    {
        Console.WriteLine("=== Sample 16: Agent Loop (Visible Switch + Hybrid Streaming) ===\n");
        Console.WriteLine("Say 'write ...' to trigger Writer→Critic loop until APPROVE.");
        Console.WriteLine("Type 'quit' | 'q' | 'exit' | 'done' to end.\n");

        // Azure OpenAI setup
        var azureClient = new AzureOpenAIClient(AIConfig.Endpoint, AIConfig.KeyCredential);
        var deploymentName = AIConfig.ModelDeployment;
        IChatClient chatClient = azureClient.GetChatClient(deploymentName).AsIChatClient();

        // Agents
        var managerAgent = GetManagerAgent(chatClient);
        var writerAgent = GetWriterAgent(chatClient);
        var criticAgent = GetCriticAgent(chatClient);
        var summaryAgent = GetSummaryAgent(chatClient);

        // Executors
        var userTurnExec = new UserChatTurnExecutor();
        var managerExec = new ManagerAgentExecutor(managerAgent);
        var writerExec = new WriterAgentExecutor(writerAgent);
        var criticExec = new CriticAgentExecutor(criticAgent);
        var summaryExec = new SummaryAgentExecutor(summaryAgent);
        var goodbyeExec = new GoodbyeExecutor();

        var builder = new WorkflowBuilder(userTurnExec)
            .AddEdge(userTurnExec, managerExec)

            .AddSwitch(managerExec, sw => sw
                .AddCase<RoutingToken>(rt => rt is not null && rt.Path == WorkflowPath.Writer, writerExec)
                .AddCase<RoutingToken>(rt => rt is not null && rt.Path == WorkflowPath.Direct, summaryExec)
                .AddCase<RoutingToken>(rt => rt is not null && rt.Path == WorkflowPath.Exit, goodbyeExec))

            .AddEdge(writerExec, criticExec)

            .AddSwitch(criticExec, sw => sw
                .AddCase<CriticDecision>(cd => cd is not null && cd.Approved, summaryExec)
                .AddCase<CriticDecision>(cd => cd is not null && !cd.Approved, writerExec))

            .AddEdge(summaryExec, userTurnExec)

            .WithOutputFrom(summaryExec) // keep summary as main output source
                                         // If the framework supports multiple output sources, you can also expose goodbye:
                                         // .WithOutputFrom(summaryExec, goodbyeExec)
            .Build();

        WorkflowVisualizerTool.PrintAll(builder, "Sample 17: Agent Loop (Visible Switch + Hybrid Streaming)");

        // Start one run; UserChatTurnExecutor will prompt internally
        var run = await InProcessExecution.StreamAsync(builder, new ChatMessage(ChatRole.System, "START"));

        bool terminate = false;
        while (!terminate)
        {
            await run.TrySendMessageAsync(new TurnToken(emitEvents: true));

            bool readyForNextTurn = false;
            
            await foreach (var evt in run.WatchStreamAsync())
            {
                switch (evt)
                {
                    case ExecutorStartedEvent started:
                        Console.WriteLine($"\n=== {started.Name} ===");
                        break;

                    case DisplayUpdateEvent display:
                        Console.Write(display.Chunk);
                        break;

                    case ReadyForInputEvent:
                        // Workflow completed this cycle, ready for next turn
                        readyForNextTurn = true;
                        break;

                    case TerminateWorkflowEvent:
                        terminate = true;
                        break;
                }
                
                // Break out of event stream if ready for next turn or terminating
                if (readyForNextTurn || terminate)
                {
                    break;
                }
            }
        }

        Console.WriteLine("\n[Workflow terminated]");
    }

    // --------------------- Agent factories ---------------------
    private static AIAgent GetManagerAgent(IChatClient chat) =>
        new ChatClientAgent(chat, """
            You are ManagerAgent. You may address the user directly if the task is not related to writing AND must end with a single JSON control line:
            {"route":"WRITER|REPLY|QUIT"}
            - If the user asks to write/compose/create text respond wiht → WRITER. You do not address this directly if the task is about writing. You delegate it to the Writer.
            - If the user says quit|q|exit|done → QUIT.
            - Else → REPLY (and your prior text is your message to the user).
            Stream your helpful text normally; the very last line must be the JSON.
            """);

    private static AIAgent GetWriterAgent(IChatClient chat) =>
        new ChatClientAgent(chat, """
            You are WriterAgent. Produce a complete draft for the user's request.
            If feedback is provided, apply it carefully.
            Stream content; do NOT output routing JSON.
            """);

    private static AIAgent GetCriticAgent(IChatClient chat) =>
        new ChatClientAgent(chat, """
            # ROLE
            You are CriticAgent. Review the draft briefly while streaming rationale/suggestions.
            
            # TASK
            You will always try to provide constructive critic with actionables on what to improve and how to improve.
            In case you do not have feedback you will respond with approved true and no feedback (see the output format)
            
            # FORMAT
            At the very end, output EXACTLY one control JSON line:
            {"approved":true,"feedback":""}  OR  {"approved":false,"feedback":"..."}
            """);

    private static AIAgent GetSummaryAgent(IChatClient chat) =>
        new ChatClientAgent(chat, """
            You are SummaryAgent. Stream the final message to the user.
            If an approved draft is provided, present that as the final output.
            If the manager’s direct reply is provided, present that message.
            Do not output any routing JSON.
            """);

    // --------------------- Executors ---------------------

    private sealed class UserChatTurnExecutor :
        ReflectingExecutor<UserChatTurnExecutor>,
        IMessageHandler<ChatMessage, ChatMessage>,
        IMessageHandler<RoutingToken, ChatMessage>,      // Concrete type from Manager→Summary→User
        IMessageHandler<CriticDecision, ChatMessage>     // Concrete type from Critic→Summary→User
    {
        public UserChatTurnExecutor() : base("UserChatTurnExecutor") { }

        // Handler for ChatMessage (initial workflow start)
        public async ValueTask<ChatMessage> HandleAsync(
            ChatMessage _,
            IWorkflowContext context,
            CancellationToken cancellationToken = default)
        {
            return await PromptUserAsync(context, cancellationToken);
        }

        // Handler for RoutingToken (from Summary after Manager direct reply)
        public async ValueTask<ChatMessage> HandleAsync(
            RoutingToken _,
            IWorkflowContext context,
            CancellationToken cancellationToken = default)
        {
            return await PromptUserAsync(context, cancellationToken);
        }

        // Handler for CriticDecision (from Summary after Critic approval)
        public async ValueTask<ChatMessage> HandleAsync(
            CriticDecision _,
            IWorkflowContext context,
            CancellationToken cancellationToken = default)
        {
            return await PromptUserAsync(context, cancellationToken);
        }

        // Shared implementation
        private async Task<ChatMessage> PromptUserAsync(
            IWorkflowContext context,
            CancellationToken cancellationToken)
        {
            var s = await ReadFlowStateAsync(context, cancellationToken);
            await context.AddEventAsync(new ExecutorStartedEvent { Name = "UserChatTurnExecutor" });

            Console.Write("\nYou: ");
            var input = Console.ReadLine() ?? "";

            var msg = new ChatMessage(ChatRole.User, input);
            s.History.Add(msg);
            await SaveFlowStateAsync(context, s, cancellationToken);
            return msg;
        }
    }

    private sealed class ManagerAgentExecutor :
        ReflectingExecutor<ManagerAgentExecutor>,
        IMessageHandler<ChatMessage, RoutingToken>
    {
        private readonly AIAgent _agent;
        public ManagerAgentExecutor(AIAgent agent) : base("ManagerAgentExecutor") => _agent = agent;

        public async ValueTask<RoutingToken> HandleAsync(
            ChatMessage message,
            IWorkflowContext context,
            CancellationToken cancellationToken = default)
        {
            await context.AddEventAsync(new ExecutorStartedEvent { Name = "ManagerAgentExecutor" }); // FIX

            var norm = (message.Text ?? "").Trim().ToLowerInvariant();
            if (norm is "quit" or "q" or "exit" or "done")
            {
                await context.AddEventAsync(new DisplayUpdateEvent { Source = "Manager", Chunk = "Understood. Exiting...\n" }); // FIX
                return new RoutingToken { Path = WorkflowPath.Exit, ManagerResponse = "Goodbye!", OriginalMessage = message };
            }

            var s = await ReadFlowStateAsync(context, cancellationToken);
            var sb = new StringBuilder();

            await foreach (var up in _agent.RunStreamingAsync(message, cancellationToken: cancellationToken)) // FIX: param name
            {
                if (!string.IsNullOrEmpty(up.Text))
                {
                    sb.Append(up.Text);
                    await context.AddEventAsync(new DisplayUpdateEvent { Source = "Manager", Chunk = up.Text }); // FIX
                }
            }

            var (plain, path) = ExtractRouteJson(sb.ToString());
            s.History.Add(new ChatMessage(ChatRole.Assistant, plain));
            await SaveFlowStateAsync(context, s, cancellationToken);

            return new RoutingToken
            {
                Path = path,
                ManagerResponse = plain,
                OriginalMessage = message
            };
        }

        private static (string plain, WorkflowPath path) ExtractRouteJson(string full)
        {
            string plain = full;
            WorkflowPath path = WorkflowPath.Direct;

            var lines = full.Replace("\r\n", "\n").Split('\n');
            if (lines.Length > 0)
            {
                var last = lines[^1].Trim();
                if (last.StartsWith("{") && last.EndsWith("}"))
                {
                    plain = string.Join("\n", lines[..^1]);
                    try
                    {
                        using var doc = JsonDocument.Parse(last);
                        var r = doc.RootElement.GetProperty("route").GetString()?.ToUpperInvariant();
                        path = r switch
                        {
                            "WRITER" => WorkflowPath.Writer,
                            "REPLY" => WorkflowPath.Direct,
                            "QUIT" => WorkflowPath.Exit,
                            _ => WorkflowPath.Direct
                        };
                    }
                    catch { path = WorkflowPath.Direct; }
                }
            }
            return (plain, path);
        }
    }

    private sealed class WriterAgentExecutor :
        ReflectingExecutor<WriterAgentExecutor>,
        IMessageHandler<RoutingToken, WrittenContent>,
        IMessageHandler<CriticDecision, WrittenContent>
    {
        private readonly AIAgent _agent;
        public WriterAgentExecutor(AIAgent agent) : base("WriterAgentExecutor") => _agent = agent;

        public async ValueTask<WrittenContent> HandleAsync(
            RoutingToken token,
            IWorkflowContext context,
            CancellationToken cancellationToken = default)
            => await ProduceAsync(token.OriginalMessage.Text ?? "", 1, context, cancellationToken);

        public async ValueTask<WrittenContent> HandleAsync(
            CriticDecision decision,
            IWorkflowContext context,
            CancellationToken cancellationToken = default)
        {
            var prompt =
                $"Revise the following content applying this feedback.\n\n" +
                $"Feedback:\n{decision.Feedback}\n\n" +
                $"Content:\n{decision.Content}";
            return await ProduceAsync(prompt, decision.Iteration + 1, context, cancellationToken);
        }

        private async Task<WrittenContent> ProduceAsync(
            string prompt,
            int iteration,
            IWorkflowContext context,
            CancellationToken cancellationToken)
        {
            await context.AddEventAsync(new ExecutorStartedEvent { Name = "WriterAgentExecutor" }); // FIX

            var s = await ReadFlowStateAsync(context, cancellationToken);
            var sb = new StringBuilder();

            await foreach (var up in _agent.RunStreamingAsync(new ChatMessage(ChatRole.User, prompt), cancellationToken: cancellationToken)) // FIX
            {
                if (!string.IsNullOrEmpty(up.Text))
                {
                    sb.Append(up.Text);
                    await context.AddEventAsync(new DisplayUpdateEvent { Source = "Writer", Chunk = up.Text }); // FIX
                }
            }

            var text = sb.ToString();
            s.History.Add(new ChatMessage(ChatRole.Assistant, text));
            await SaveFlowStateAsync(context, s, cancellationToken);
            return new WrittenContent { Text = text, Iteration = iteration };
        }
    }

    private sealed class CriticAgentExecutor :
        ReflectingExecutor<CriticAgentExecutor>,
        IMessageHandler<WrittenContent, CriticDecision>
    {
        private readonly AIAgent _agent;
        private const int MaxIterations = 2;
        public CriticAgentExecutor(AIAgent agent) : base("CriticAgentExecutor") => _agent = agent;

        public async ValueTask<CriticDecision> HandleAsync(
            WrittenContent content,
            IWorkflowContext context,
            CancellationToken cancellationToken = default) // FIX: add token to match interface
        {
            await context.AddEventAsync(new ExecutorStartedEvent { Name = "CriticAgentExecutor" }); // FIX

            var s = await ReadFlowStateAsync(context, cancellationToken); // FIX: no GetSharedState
            var sb = new StringBuilder();

            var prompt =
                "Review the draft. Stream rationale succinctly. " +
                "At the very end, output EXACTLY one JSON line: " +
                "{\"approved\":true,\"feedback\":\"\"} or {\"approved\":false,\"feedback\":\"...\"}\n\n" +
                content.Text;

            await foreach (var up in _agent.RunStreamingAsync(new ChatMessage(ChatRole.User, prompt), cancellationToken: cancellationToken)) // FIX
            {
                if (!string.IsNullOrEmpty(up.Text))
                {
                    sb.Append(up.Text);
                    await context.AddEventAsync(new DisplayUpdateEvent { Source = "Critic", Chunk = up.Text }); // FIX
                }
            }

            var full = sb.ToString();
            var (approved, feedback) = ParseDecision(full);

            if (!approved && content.Iteration >= MaxIterations)
                approved = true; // safety cap

            // Keep rationale (without JSON) in history
            s.History.Add(new ChatMessage(ChatRole.Assistant, StripTrailingJson(full)));
            await SaveFlowStateAsync(context, s, cancellationToken);

            return new CriticDecision
            {
                Approved = approved,
                Feedback = feedback,
                Content = content.Text,
                Iteration = content.Iteration
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
                if (full.Contains("APPROVE", StringComparison.OrdinalIgnoreCase)) return (true, "");
                return (false, "Missing approval JSON.");
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

    private sealed class SummaryAgentExecutor :
        ReflectingExecutor<SummaryAgentExecutor>,
        IMessageHandler<RoutingToken, IOutputContent>,
        IMessageHandler<CriticDecision, IOutputContent>
    {
        private readonly AIAgent _agent;
        public SummaryAgentExecutor(AIAgent agent) : base("SummaryAgentExecutor") => _agent = agent;

        // Handler for RoutingToken (from Manager's REPLY path)
        public async ValueTask<IOutputContent> HandleAsync(
            RoutingToken token,
            IWorkflowContext context,
            CancellationToken cancellationToken = default)
            => await ProcessSummaryAsync(token, context, cancellationToken);

        // Handler for CriticDecision (from Critic's approved path)
        public async ValueTask<IOutputContent> HandleAsync(
            CriticDecision decision,
            IWorkflowContext context,
            CancellationToken cancellationToken = default)
            => await ProcessSummaryAsync(decision, context, cancellationToken);

        private async Task<IOutputContent> ProcessSummaryAsync(
            IOutputContent content,
            IWorkflowContext context,
            CancellationToken cancellationToken)
        {
            await context.AddEventAsync(new ExecutorStartedEvent { Name = "SummaryAgentExecutor" });

            var s = await ReadFlowStateAsync(context, cancellationToken);
            var history = string.Join("\n---\n", s.History.Select(h => $"{h.Role}: {h.Text}"));
            var payload = content.GetText();

            var prompt = $"Conversation so far:\n{history}\n\nFinal reply for the user:\n{payload}";

            await foreach (var up in _agent.RunStreamingAsync(new ChatMessage(ChatRole.User, prompt), cancellationToken: cancellationToken))
            {
                if (!string.IsNullOrEmpty(up.Text))
                {
                    await context.AddEventAsync(new DisplayUpdateEvent { Source = "Summary", Chunk = up.Text });
                }
            }

            // Signal next input; edge goes back to UserChatTurnExecutor
            await context.AddEventAsync(new ReadyForInputEvent());
            return content;
        }
    }

    private sealed class GoodbyeExecutor :
        ReflectingExecutor<GoodbyeExecutor>,
        IMessageHandler<RoutingToken, RoutingToken>
    {
        public GoodbyeExecutor() : base("GoodbyeExecutor") { }

        public async ValueTask<RoutingToken> HandleAsync(
            RoutingToken token,
            IWorkflowContext context,
            CancellationToken cancellationToken = default)
        {
            await context.AddEventAsync(new ExecutorStartedEvent { Name = "GoodbyeExecutor" }); // FIX
            await context.AddEventAsync(new DisplayUpdateEvent { Source = "System", Chunk = "\nGoodbye!\n" }); // FIX
            await context.AddEventAsync(new TerminateWorkflowEvent()); // FIX
            return token;
        }
    }
}
