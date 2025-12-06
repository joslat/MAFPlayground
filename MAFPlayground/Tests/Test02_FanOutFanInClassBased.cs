// SPDX-License-Identifier: LicenseRef-MAFPlayground-NPU-1.0-CH
// Copyright (c) 2025 Jose Luis

using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Reflection;

namespace MAFPlayground.Tests;

/// <summary>
/// Test 02: Basic Fan-Out/Fan-In with CLASS-BASED Executors
/// 
/// This test uses the EXACT same workflow as Test01, but with class-based executors
/// instead of function-based executors.
/// 
/// Workflow:
/// StartExecutor (class)
///     ? Fan-Out ? ExecutorA (class)
///              ? ExecutorB (class)
///     ? Fan-In  ? AggregatorExecutor (class)
///     ? FinalExecutor (class)
/// 
/// Goal: Determine if class-based executors can work with fan-in aggregation.
/// 
/// Compare with Test01 results:
/// - Test01: Function-based executors ? WORKS
/// - Test02: Class-based executors ? Testing now...
/// </summary>
internal static class Test02_FanOutFanInClassBased
{
    // --------------------- Entry Point ---------------------
    public static async Task Execute()
    {
        Console.WriteLine("=== Test 02: Basic Fan-Out/Fan-In with CLASS-BASED Executors ===\n");
        Console.WriteLine("Testing if class-based executors work with fan-in aggregation\n");

        // Build workflow with class-based executors
        var workflow = BuildWorkflow_ClassBased();

        Console.WriteLine("--- Workflow Structure ---");
        Console.WriteLine("StartExecutor (class)");
        Console.WriteLine("  ? Fan-Out ? ExecutorA (class)");
        Console.WriteLine("           ? ExecutorB (class)");
        Console.WriteLine("  ? Fan-In  ? AggregatorExecutor (class)");
        Console.WriteLine("  ? FinalExecutor (class)\n");

        Console.WriteLine(new string('=', 80));
        Console.WriteLine("EXECUTING TEST WORKFLOW (CLASS-BASED)");
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

        Console.WriteLine("\n? Test 02 Complete!\n");
        Console.WriteLine("Comparison:");
        Console.WriteLine("  Test01 (function-based): ? Aggregator called");
        Console.WriteLine("  Test02 (class-based):    ? Check output above");
    }

    // --------------------- Class-Based Workflow ---------------------
    private static Workflow BuildWorkflow_ClassBased()
    {
        Console.WriteLine("Building workflow with CLASS-BASED executors...\n");

        // Create class-based executors
        var startExecutor = new StartExecutor_ClassBased();
        var executorA = new ExecutorA_ClassBased();
        var executorB = new ExecutorB_ClassBased();
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
    /// Executor A - processes message and returns result
    /// ? Using synchronous ValueTask.FromResult pattern
    /// </summary>
    private sealed class ExecutorA_ClassBased : 
        ReflectingExecutor<ExecutorA_ClassBased>, 
        IMessageHandler<string, string>
    {
        public ExecutorA_ClassBased() : base("ExecutorA") { }

        public ValueTask<string> HandleAsync(
            string message, 
            IWorkflowContext context, 
            CancellationToken cancellationToken = default)
        {
            Console.WriteLine($"[ExecutorA] Received: '{message}'");
            
            var result = "[ExecutorA] ? Completed - Would add 'A' to state";
            Console.WriteLine($"{result}\n");
            
            // ? Return synchronously like Sample14
            return ValueTask.FromResult(result);
        }
    }

    /// <summary>
    /// Executor B - processes message and returns result
    /// ? Using synchronous ValueTask.FromResult pattern
    /// </summary>
    private sealed class ExecutorB_ClassBased : 
        ReflectingExecutor<ExecutorB_ClassBased>, 
        IMessageHandler<string, string>
    {
        public ExecutorB_ClassBased() : base("ExecutorB") { }

        public ValueTask<string> HandleAsync(
            string message, 
            IWorkflowContext context, 
            CancellationToken cancellationToken = default)
        {
            Console.WriteLine($"[ExecutorB] Received: '{message}'");
            
            var result = "[ExecutorB] ? Completed - Would add 'B' to state";
            Console.WriteLine($"{result}\n");
            
            // ? Return synchronously like Sample14
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
