// SPDX-License-Identifier: LicenseRef-MAFPlayground-NPU-1.0-CH
// Copyright (c) 2025 Jose Luis

using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Reflection;

namespace MAFPlayground.Tests;

/// <summary>
/// Test 04: Fan-Out/Fan-In with REAL WORKFLOW CONTEXT STATE OPERATIONS
/// 
/// This test is IDENTICAL to Test03, BUT uses REAL workflow context state operations:
/// - context.ReadStateAsync()
/// - context.QueueStateUpdateAsync()
/// 
/// This tests whether the workflow framework's state operations interfere with fan-in.
/// 
/// Workflow:
/// StartExecutor (class)
///     ? Fan-Out ? ExecutorA (class, uses context.ReadStateAsync)
///              ? ExecutorB (class, uses context.ReadStateAsync)
///     ? Fan-In  ? AggregatorExecutor (class)
///     ? FinalExecutor (class)
/// 
/// Goal: Determine if context state operations break fan-in message routing.
/// 
/// Compare with previous tests:
/// - Test01: Function-based, no async ? WORKS
/// - Test02: Class-based, no async ? WORKS
/// - Test03: Class-based, mock async blocking ? WORKS
/// - Test04: Class-based, REAL context state operations ? Testing now...
/// </summary>
internal static class Test04_FanOutFanInWithRealStateOperations
{
    // ?? CRITICAL TEST FLAG: Toggle context state operations on/off
    // false = No state operations (should work like Test03)
    // true  = WITH context state operations (tests if this breaks fan-in)
    private const bool ENABLE_CONTEXT_STATE_OPS = true;

    // --------------------- Shared State ---------------------
    private sealed class TestState
    {
        public Dictionary<string, string> Data { get; set; } = new();
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
        Console.WriteLine("=== Test 04: Fan-Out/Fan-In with REAL CONTEXT STATE OPERATIONS ===\n");
        Console.WriteLine($"??  CONTEXT STATE OPS FLAG: {(ENABLE_CONTEXT_STATE_OPS ? "ENABLED" : "DISABLED")} ??\n");
        Console.WriteLine("Testing if context.ReadStateAsync/QueueStateUpdateAsync breaks fan-in\n");

        // Build workflow
        var workflow = BuildWorkflow_WithContextStateOps();

        Console.WriteLine("--- Workflow Structure ---");
        Console.WriteLine("StartExecutor (class)");
        Console.WriteLine("  ? Fan-Out ? ExecutorA (class, uses context state)");
        Console.WriteLine("           ? ExecutorB (class, uses context state)");
        Console.WriteLine("  ? Fan-In  ? AggregatorExecutor (class)");
        Console.WriteLine("  ? FinalExecutor (class)\n");

        Console.WriteLine(new string('=', 80));
        Console.WriteLine("EXECUTING TEST WORKFLOW (WITH CONTEXT STATE OPS)");
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

        Console.WriteLine("\n? Test 04 Complete!\n");
        Console.WriteLine($"Test Configuration: CONTEXT_STATE_OPS = {ENABLE_CONTEXT_STATE_OPS}\n");
        Console.WriteLine("Comparison:");
        Console.WriteLine("  Test01 (function-based, no async):           ? Aggregator called");
        Console.WriteLine("  Test02 (class-based, no async):              ? Aggregator called");
        Console.WriteLine("  Test03 (class-based, mock async blocking):   ? Aggregator called");
        Console.WriteLine($"  Test04 (class-based, context state={ENABLE_CONTEXT_STATE_OPS}): ? Check output above");
        Console.WriteLine();
        Console.WriteLine("?? To test the other mode, change ENABLE_CONTEXT_STATE_OPS constant and re-run!");
    }

    // --------------------- Class-Based Workflow ---------------------
    private static Workflow BuildWorkflow_WithContextStateOps()
    {
        Console.WriteLine("Building workflow with CLASS-BASED executors using REAL context state operations...\n");

        var startExecutor = new StartExecutor_ClassBased();
        var executorA = new ExecutorA_WithContextStateOps();
        var executorB = new ExecutorB_WithContextStateOps();
        var aggregator = new AggregatorExecutor_ClassBased();
        var finalExecutor = new FinalExecutor_ClassBased();

        return new WorkflowBuilder(startExecutor)
            .AddFanOutEdge(startExecutor, targets: [executorA, executorB])
            .AddFanInEdge(aggregator, sources: [executorA, executorB])
            .AddEdge(aggregator, finalExecutor)
            .WithOutputFrom(finalExecutor)
            .Build();
    }

    // --------------------- Class-Based Executors ---------------------

    private sealed class StartExecutor_ClassBased : 
        ReflectingExecutor<StartExecutor_ClassBased>, 
        IMessageHandler<string>
    {
        public StartExecutor_ClassBased() : base("StartExecutor") { }

        public async ValueTask HandleAsync(
            string message, 
            IWorkflowContext context, 
            CancellationToken cancellationToken = default)
        {
            Console.WriteLine($"[StartExecutor] Received: '{message}'");
            Console.WriteLine("[StartExecutor] Broadcasting to ExecutorA and ExecutorB\n");
            await context.SendMessageAsync(message, cancellationToken: cancellationToken);
        }
    }

    /// <summary>
    /// Executor A - Uses REAL context.ReadStateAsync and context.QueueStateUpdateAsync
    /// ?? CRITICAL TEST: Does this break fan-in?
    /// </summary>
    private sealed class ExecutorA_WithContextStateOps : 
        ReflectingExecutor<ExecutorA_WithContextStateOps>, 
        IMessageHandler<string, string>
    {
        public ExecutorA_WithContextStateOps() : base("ExecutorA") { }

        public ValueTask<string> HandleAsync(
            string message, 
            IWorkflowContext context, 
            CancellationToken cancellationToken = default)
        {
            Console.WriteLine($"[ExecutorA] Received: '{message}'");
            
            string result;
            
            if (ENABLE_CONTEXT_STATE_OPS)
            {
                Console.WriteLine($"[ExecutorA] ??  CONTEXT STATE OPS ENABLED - Using context.ReadStateAsync/QueueStateUpdateAsync");
                
                // ?? CRITICAL TEST: Use REAL workflow context state operations
                var state = ReadStateAsync(context).GetAwaiter().GetResult();
                state.Data["A"] = $"Processed by A: {message}";
                SaveStateAsync(context, state).GetAwaiter().GetResult();
                
                result = $"[ExecutorA] ? Completed - Added to context state (WITH context state ops)";
            }
            else
            {
                Console.WriteLine($"[ExecutorA] ? CONTEXT STATE OPS DISABLED - Running without state");
                result = "[ExecutorA] ? Completed - No state operations (DISABLED)";
            }
            
            Console.WriteLine($"{result}\n");
            
            return ValueTask.FromResult(result);
        }
    }

    /// <summary>
    /// Executor B - Uses REAL context.ReadStateAsync and context.QueueStateUpdateAsync
    /// ?? CRITICAL TEST: Does this break fan-in?
    /// </summary>
    private sealed class ExecutorB_WithContextStateOps : 
        ReflectingExecutor<ExecutorB_WithContextStateOps>, 
        IMessageHandler<string, string>
    {
        public ExecutorB_WithContextStateOps() : base("ExecutorB") { }

        public ValueTask<string> HandleAsync(
            string message, 
            IWorkflowContext context, 
            CancellationToken cancellationToken = default)
        {
            Console.WriteLine($"[ExecutorB] Received: '{message}'");
            
            string result;
            
            if (ENABLE_CONTEXT_STATE_OPS)
            {
                Console.WriteLine($"[ExecutorB] ??  CONTEXT STATE OPS ENABLED - Using context.ReadStateAsync/QueueStateUpdateAsync");
                
                // ?? CRITICAL TEST: Use REAL workflow context state operations
                var state = ReadStateAsync(context).GetAwaiter().GetResult();
                state.Data["B"] = $"Processed by B: {message}";
                SaveStateAsync(context, state).GetAwaiter().GetResult();
                
                result = $"[ExecutorB] ? Completed - Added to context state (WITH context state ops)";
            }
            else
            {
                Console.WriteLine($"[ExecutorB] ? CONTEXT STATE OPS DISABLED - Running without state");
                result = "[ExecutorB] ? Completed - No state operations (DISABLED)";
            }
            
            Console.WriteLine($"{result}\n");
            
            return ValueTask.FromResult(result);
        }
    }

    private sealed class AggregatorExecutor_ClassBased : 
        ReflectingExecutor<AggregatorExecutor_ClassBased>, 
        IMessageHandler<string, string>
    {
        private readonly List<string> _messages = new();
        private const int ExpectedCount = 2;

        public AggregatorExecutor_ClassBased() : base("AggregatorExecutor") { }

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
                
                if (ENABLE_CONTEXT_STATE_OPS)
                {
                    // Verify state was updated
                    var state = ReadStateAsync(context).GetAwaiter().GetResult();
                    Console.WriteLine($"[Aggregator] Verifying context state:");
                    Console.WriteLine($"  State contains {state.Data.Count} items");
                    foreach (var kvp in state.Data)
                    {
                        Console.WriteLine($"    {kvp.Key}: {kvp.Value}");
                    }
                    Console.WriteLine();
                }
                
                var aggregated = string.Join("\n", _messages);
                _messages.Clear();
                return ValueTask.FromResult(aggregated);
            }

            return ValueTask.FromResult<string>(null!);
        }
    }

    private sealed class FinalExecutor_ClassBased : 
        ReflectingExecutor<FinalExecutor_ClassBased>, 
        IMessageHandler<string, string>
    {
        public FinalExecutor_ClassBased() : base("FinalExecutor") { }

        public ValueTask<string> HandleAsync(
            string message, 
            IWorkflowContext context, 
            CancellationToken cancellationToken = default)
        {
            Console.WriteLine("[FinalExecutor] Aggregation complete!");
            Console.WriteLine($"[FinalExecutor] Received: '{message}'\n");
            
            var result = $"? Test Complete!\n" +
                       $"Aggregated confirmations:\n{message}";
            
            return ValueTask.FromResult(result);
        }
    }
}
