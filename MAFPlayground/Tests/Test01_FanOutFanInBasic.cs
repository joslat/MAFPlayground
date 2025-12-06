// SPDX-License-Identifier: LicenseRef-MAFPlayground-NPU-1.0-CH
// Copyright (c) 2025 Jose Luis

using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Reflection;

namespace MAFPlayground.Tests;

/// <summary>
/// Test 01: Basic Fan-Out/Fan-In with Pure Executors
/// 
/// This is a minimal test case to understand the fan-out/fan-in pattern
/// without any AI agents - just pure executors manipulating shared state.
/// 
/// Workflow:
/// StartExecutor
///     ? Fan-Out ? ExecutorA (adds "A" to state)
///              ? ExecutorB (adds "B" to state)
///     ? Fan-In  ? AggregatorExecutor (collects both)
/// 
/// Each executor:
/// - Adds a string to shared state
/// - Returns a confirmation string
/// 
/// This test will validate:
/// ? Do function-based executors work with fan-in?
/// ? Does shared state persist correctly?
/// ? Does the aggregator receive both messages?
/// </summary>
internal static class Test01_FanOutFanInBasic
{
    // --------------------- Shared State ---------------------
    private sealed class TestState
    {
        public List<string> Items { get; } = new();
    }

    private static class TestStateShared
    {
        public const string Scope = "TestScope";
        public const string Key = "singleton";
    }

    private static async Task<TestState> ReadStateAsync(IWorkflowContext context)
    {
        var state = await context.ReadStateAsync<TestState>(TestStateShared.Key, scopeName: TestStateShared.Scope);
        return state ?? new TestState();
    }

    private static ValueTask SaveStateAsync(IWorkflowContext context, TestState state)
        => context.QueueStateUpdateAsync(TestStateShared.Key, state, scopeName: TestStateShared.Scope);

    // --------------------- Entry Point ---------------------
    public static async Task Execute()
    {
        Console.WriteLine("=== Test 01: Basic Fan-Out/Fan-In with Pure Executors ===\n");
        Console.WriteLine("Testing minimal fan-out/fan-in pattern following Sample14\n");

        // Build workflow with function-based executors (Sample14 style)
        var workflow = BuildWorkflow_FunctionBased();

        Console.WriteLine("--- Workflow Structure ---");
        Console.WriteLine("StartExecutor");
        Console.WriteLine("  ? Fan-Out ? ExecutorA");
        Console.WriteLine("           ? ExecutorB");
        Console.WriteLine("  ? Fan-In  ? AggregatorExecutor");
        Console.WriteLine("  ? FinalExecutor (displays results)\n");

        Console.WriteLine(new string('=', 80));
        Console.WriteLine("EXECUTING TEST WORKFLOW");
        Console.WriteLine(new string('=', 80) + "\n");

        await using StreamingRun run = await InProcessExecution.StreamAsync(workflow, "START");

        await foreach (WorkflowEvent evt in run.WatchStreamAsync())
        {
            if (evt is WorkflowOutputEvent output)
            {
                Console.WriteLine("\n" + new string('=', 80));
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("? TEST COMPLETE");
                Console.ResetColor();
                Console.WriteLine(new string('=', 80));
                Console.WriteLine();
                Console.WriteLine(output.Data);
                Console.WriteLine();
                Console.WriteLine(new string('=', 80));
            }
        }

        Console.WriteLine("\n? Test 01 Complete!\n");
    }

    // --------------------- Function-Based Workflow (Sample14 Pattern) ---------------------
    private static Workflow BuildWorkflow_FunctionBased()
    {
        Console.WriteLine("Building workflow with FUNCTION-BASED executors (Sample14 pattern)...\n");

        // Start executor - sends initial message
        Func<string, IWorkflowContext, CancellationToken, ValueTask> startFunc = 
            async (string input, IWorkflowContext ctx, CancellationToken ct) =>
            {
                Console.WriteLine($"[StartExecutor] Received: '{input}'");
                Console.WriteLine("[StartExecutor] Broadcasting to ExecutorA and ExecutorB\n");
                await ctx.SendMessageAsync(input, cancellationToken: ct);
            };
        var startExecutor = startFunc.BindAsExecutor("StartExecutor");

        // Executor A - processes and returns result
        // ? Following Sample14 pattern: synchronous with ValueTask.FromResult
        Func<string, IWorkflowContext, CancellationToken, ValueTask<string>> executorAFunc = 
            (string input, IWorkflowContext ctx, CancellationToken ct) =>
            {
                Console.WriteLine($"[ExecutorA] Received: '{input}'");
                
                // Note: We can't use async operations here to match Sample14 exactly
                // In real code, you'd make this async if you need state operations
                var result = "[ExecutorA] ? Completed - Would add 'A' to state";
                Console.WriteLine($"{result}\n");
                
                // ? Sample14 pattern: Just return the result
                return ValueTask.FromResult(result);
            };
        var executorA = executorAFunc.BindAsExecutor("ExecutorA");

        // Executor B - processes and returns result
        // ? Following Sample14 pattern: synchronous with ValueTask.FromResult
        Func<string, IWorkflowContext, CancellationToken, ValueTask<string>> executorBFunc = 
            (string input, IWorkflowContext ctx, CancellationToken ct) =>
            {
                Console.WriteLine($"[ExecutorB] Received: '{input}'");
                
                // Note: We can't use async operations here to match Sample14 exactly
                // In real code, you'd make this async if you need state operations
                var result = "[ExecutorB] ? Completed - Would add 'B' to state";
                Console.WriteLine($"{result}\n");
                
                // ? Sample14 pattern: Just return the result
                return ValueTask.FromResult(result);
            };
        var executorB = executorBFunc.BindAsExecutor("ExecutorB");

        // Aggregator - collects results from A and B
        var aggregator = new AggregatorExecutor_FunctionBased();

        // Final executor - displays results
        // ? Keeping it simple like Sample14
        Func<string, IWorkflowContext, CancellationToken, ValueTask<string>> finalFunc = 
            (string input, IWorkflowContext ctx, CancellationToken ct) =>
            {
                Console.WriteLine("[FinalExecutor] Aggregation complete!");
                Console.WriteLine($"[FinalExecutor] Received: '{input}'\n");
                
                var result = $"? Test Complete!\n" +
                           $"Aggregated confirmations:\n{input}";
                
                return ValueTask.FromResult(result);
            };
        var finalExecutor = finalFunc.BindAsExecutor("FinalExecutor");

        // Build workflow
        return new WorkflowBuilder(startExecutor)
            .AddFanOutEdge(startExecutor, targets: [executorA, executorB])
            .AddFanInEdge(aggregator, sources: [executorA, executorB])
            .AddEdge(aggregator, finalExecutor)
            .WithOutputFrom(finalExecutor)
            .Build();
    }

    // --------------------- Aggregator (Function-Based) ---------------------
    private sealed class AggregatorExecutor_FunctionBased : 
        ReflectingExecutor<AggregatorExecutor_FunctionBased>, 
        IMessageHandler<string, string>
    {
        private readonly List<string> _messages = new();
        private const int ExpectedCount = 2;

        public AggregatorExecutor_FunctionBased() : base("AggregatorExecutor") { }

        public ValueTask<string> HandleAsync(
            string message, 
            IWorkflowContext context, 
            CancellationToken cancellationToken = default)
        {
            Console.WriteLine($"[Aggregator] HandleAsync CALLED!");
            Console.WriteLine($"[Aggregator] Received message {_messages.Count + 1}/{ExpectedCount}: '{message}'\n");
            
            _messages.Add(message);

            if (_messages.Count >= ExpectedCount)
            {
                Console.WriteLine("[Aggregator] ? Both messages collected!\n");
                var aggregated = string.Join("\n", _messages);
                _messages.Clear(); // Reset for potential re-runs
                return ValueTask.FromResult(aggregated);
            }

            // Return null to signal workflow to wait for more messages
            return ValueTask.FromResult<string>(null!);
        }
    }
}
