// SPDX-License-Identifier: LicenseRef-MAFPlayground-NPU-1.0-CH
// Copyright (c) 2025 Jose Luis

using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Reflection;

namespace MAFPlayground.Tests;

/// <summary>
/// Test 03: Fan-Out/Fan-In with CLASS-BASED Executors and ASYNC BLOCKING
/// 
/// This test is IDENTICAL to Test02, BUT ExecutorA and ExecutorB simulate
/// async operations (like agent calls) and block on them using GetAwaiter().GetResult().
/// 
/// This tests whether blocking on async operations breaks the fan-in pattern.
/// 
/// Workflow:
/// StartExecutor (class)
///     ? Fan-Out ? ExecutorA (class, blocks on async work)
///              ? ExecutorB (class, blocks on async work)
///     ? Fan-In  ? AggregatorExecutor (class)
///     ? FinalExecutor (class)
/// 
/// Goal: Determine if .GetAwaiter().GetResult() on async operations breaks fan-in.
/// 
/// Compare with previous tests:
/// - Test01: Function-based executors, no async ? WORKS
/// - Test02: Class-based executors, no async ? WORKS
/// - Test03: Class-based executors WITH async blocking ? Testing now...
/// </summary>
internal static class Test03_FanOutFanInWithAsyncBlocking
{
    // ?? CRITICAL TEST FLAG: Toggle async blocking on/off
    // false = No async blocking (should work like Test02)
    // true  = WITH async blocking (tests if this breaks fan-in)
    private const bool ENABLE_ASYNC_BLOCKING = true;

    // --------------------- Entry Point ---------------------
    public static async Task Execute()
    {
        Console.WriteLine("=== Test 03: Fan-Out/Fan-In with ASYNC BLOCKING ===\n");
        Console.WriteLine($"??  ASYNC BLOCKING FLAG: {(ENABLE_ASYNC_BLOCKING ? "ENABLED" : "DISABLED")} ??\n");
        Console.WriteLine("Testing if .GetAwaiter().GetResult() on async operations breaks fan-in\n");

        // Build workflow with class-based executors that block on async work
        var workflow = BuildWorkflow_WithAsyncBlocking();

        Console.WriteLine("--- Workflow Structure ---");
        Console.WriteLine("StartExecutor (class)");
        Console.WriteLine("  ? Fan-Out ? ExecutorA (class, BLOCKS on async)");
        Console.WriteLine("           ? ExecutorB (class, BLOCKS on async)");
        Console.WriteLine("  ? Fan-In  ? AggregatorExecutor (class)");
        Console.WriteLine("  ? FinalExecutor (class)\n");

        Console.WriteLine(new string('=', 80));
        Console.WriteLine("EXECUTING TEST WORKFLOW (WITH ASYNC BLOCKING)");
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

        Console.WriteLine("\n? Test 03 Complete!\n");
        Console.WriteLine($"Test Configuration: ASYNC_BLOCKING = {ENABLE_ASYNC_BLOCKING}\n");
        Console.WriteLine("Comparison:");
        Console.WriteLine("  Test01 (function-based, no async):        ? Aggregator called");
        Console.WriteLine("  Test02 (class-based, no async):           ? Aggregator called");
        Console.WriteLine($"  Test03 (class-based, async={ENABLE_ASYNC_BLOCKING}): ? Check output above");
        Console.WriteLine();
        Console.WriteLine("?? To test the other mode, change ENABLE_ASYNC_BLOCKING constant and re-run!");
    }

    // --------------------- Mock Async Operations ---------------------
    
    /// <summary>
    /// Simulates an async agent call that takes time to complete
    /// </summary>
    private static async Task<string> MockAgentCallAsync(string input, CancellationToken cancellationToken)
    {
        // Simulate some async work (like an agent API call)
        await Task.Delay(10, cancellationToken);
        return $"Processed: {input}";
    }

    /// <summary>
    /// Simulates reading from async state storage
    /// </summary>
    private static async Task<Dictionary<string, string>> MockReadStateAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(5, cancellationToken);
        return new Dictionary<string, string>();
    }

    /// <summary>
    /// Simulates writing to async state storage
    /// </summary>
    private static async Task MockSaveStateAsync(Dictionary<string, string> state, CancellationToken cancellationToken)
    {
        await Task.Delay(5, cancellationToken);
    }

    // --------------------- Class-Based Workflow ---------------------
    private static Workflow BuildWorkflow_WithAsyncBlocking()
    {
        Console.WriteLine("Building workflow with CLASS-BASED executors that BLOCK on async operations...\n");

        // Create class-based executors
        var startExecutor = new StartExecutor_ClassBased();
        var executorA = new ExecutorA_WithAsyncBlocking();
        var executorB = new ExecutorB_WithAsyncBlocking();
        var aggregator = new AggregatorExecutor_ClassBased();
        var finalExecutor = new FinalExecutor_ClassBased();

        // Build workflow
        return new WorkflowBuilder(startExecutor)
            .AddFanOutEdge(startExecutor, targets: [executorA, executorB])
            .AddFanInEdge(aggregator, sources: [executorA, executorB])
            .AddEdge(aggregator, finalExecutor)
            .WithOutputFrom(finalExecutor)
            .Build();
    }

    // --------------------- Class-Based Executors ---------------------

    /// <summary>
    /// Start executor - broadcasts message to fan-out targets
    /// </summary>
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
    /// Executor A - BLOCKS on async operations (like Demo12V2)
    /// ? Using synchronous ValueTask.FromResult pattern BUT with async blocking
    /// </summary>
    private sealed class ExecutorA_WithAsyncBlocking : 
        ReflectingExecutor<ExecutorA_WithAsyncBlocking>, 
        IMessageHandler<string, string>
    {
        public ExecutorA_WithAsyncBlocking() : base("ExecutorA") { }

        public ValueTask<string> HandleAsync(
            string message, 
            IWorkflowContext context, 
            CancellationToken cancellationToken = default)
        {
            Console.WriteLine($"[ExecutorA] Received: '{message}'");
            
            string result;
            
            if (ENABLE_ASYNC_BLOCKING)
            {
                Console.WriteLine($"[ExecutorA] ??  ASYNC BLOCKING ENABLED - Simulating async agent call...");
                
                // ?? CRITICAL TEST: Block on async operations like Demo12V2
                var state = MockReadStateAsync(cancellationToken).GetAwaiter().GetResult();
                var agentResponse = MockAgentCallAsync(message, cancellationToken).GetAwaiter().GetResult();
                state["A"] = agentResponse;
                MockSaveStateAsync(state, cancellationToken).GetAwaiter().GetResult();
                
                result = $"[ExecutorA] ? Completed - Added '{agentResponse}' to state (WITH async blocking)";
            }
            else
            {
                Console.WriteLine($"[ExecutorA] ? ASYNC BLOCKING DISABLED - Running synchronously");
                result = "[ExecutorA] ? Completed - Would add 'A' to state (NO async blocking)";
            }
            
            Console.WriteLine($"{result}\n");
            
            // ? Return synchronously
            return ValueTask.FromResult(result);
        }
    }

    /// <summary>
    /// Executor B - BLOCKS on async operations (like Demo12V2)
    /// ? Using synchronous ValueTask.FromResult pattern BUT with async blocking
    /// </summary>
    private sealed class ExecutorB_WithAsyncBlocking : 
        ReflectingExecutor<ExecutorB_WithAsyncBlocking>, 
        IMessageHandler<string, string>
    {
        public ExecutorB_WithAsyncBlocking() : base("ExecutorB") { }

        public ValueTask<string> HandleAsync(
            string message, 
            IWorkflowContext context, 
            CancellationToken cancellationToken = default)
        {
            Console.WriteLine($"[ExecutorB] Received: '{message}'");
            
            string result;
            
            if (ENABLE_ASYNC_BLOCKING)
            {
                Console.WriteLine($"[ExecutorB] ??  ASYNC BLOCKING ENABLED - Simulating async agent call...");
                
                // ?? CRITICAL TEST: Block on async operations like Demo12V2
                var state = MockReadStateAsync(cancellationToken).GetAwaiter().GetResult();
                var agentResponse = MockAgentCallAsync(message, cancellationToken).GetAwaiter().GetResult();
                state["B"] = agentResponse;
                MockSaveStateAsync(state, cancellationToken).GetAwaiter().GetResult();
                
                result = $"[ExecutorB] ? Completed - Added '{agentResponse}' to state (WITH async blocking)";
            }
            else
            {
                Console.WriteLine($"[ExecutorB] ? ASYNC BLOCKING DISABLED - Running synchronously");
                result = "[ExecutorB] ? Completed - Would add 'B' to state (NO async blocking)";
            }
            
            Console.WriteLine($"{result}\n");
            
            // ? Return synchronously
            return ValueTask.FromResult(result);
        }
    }

    /// <summary>
    /// Aggregator - collects results from ExecutorA and ExecutorB
    /// </summary>
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
                var aggregated = string.Join("\n", _messages);
                _messages.Clear(); // Reset for potential re-runs
                return ValueTask.FromResult(aggregated);
            }

            // Return null to signal workflow to wait for more messages
            return ValueTask.FromResult<string>(null!);
        }
    }

    /// <summary>
    /// Final executor - displays aggregated results
    /// </summary>
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
