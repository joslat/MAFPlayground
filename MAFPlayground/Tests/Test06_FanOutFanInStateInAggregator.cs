// SPDX-License-Identifier: LicenseRef-MAFPlayground-NPU-1.0-CH
// Copyright (c) 2025 Jose Luis

using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Reflection;

namespace MAFPlayground.Tests;

/// <summary>
/// Test 06: Fan-Out/Fan-In - State Operations in AGGREGATOR Instead of Executors
/// 
/// CRITICAL TEST: Since Test04 and Test05 both failed when executors use context state,
/// let's test if state operations work when called in the AGGREGATOR instead.
/// 
/// This will tell us if:
/// - Context state operations break fan-in ONLY in the fan-out executors
/// - OR if it's a general incompatibility with fan-in pattern
/// 
/// Workflow:
/// StartExecutor (class)
///     ? Fan-Out ? ExecutorA (class, NO state ops - just returns value)
///              ? ExecutorB (class, NO state ops - just returns value)
///     ? Fan-In  ? AggregatorExecutor (class, USES context state ops)
///     ? FinalExecutor (class)
/// 
/// Test Results So Far:
/// - Test01: Function-based, no async ? WORKS
/// - Test02: Class-based, no async ? WORKS
/// - Test03: Class-based, mock async blocking ? WORKS
/// - Test04: Class-based, executors use context state (blocking) ? FAILS
/// - Test05: Class-based, executors use context state (async/await) ? FAILS
/// - Test06: Class-based, AGGREGATOR uses context state ? Testing now...
/// </summary>
internal static class Test06_FanOutFanInStateInAggregator
{
    // ?? TOGGLE FLAG: Where to use context state operations
    // false = No state operations anywhere (should work)
    // true  = State operations in AGGREGATOR (not in executors)
    private const bool USE_STATE_IN_AGGREGATOR = true;

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
        Console.WriteLine("=== Test 06: State Operations in AGGREGATOR (Not Executors) ===\n");
        Console.WriteLine($"??  STATE IN AGGREGATOR: {(USE_STATE_IN_AGGREGATOR ? "ENABLED" : "DISABLED")} ??\n");
        Console.WriteLine("Testing if context state works when used in AGGREGATOR instead of fan-out executors\n");

        var workflow = BuildWorkflow();

        Console.WriteLine("--- Workflow Structure ---");
        Console.WriteLine("StartExecutor (class)");
        Console.WriteLine("  ? Fan-Out ? ExecutorA (class, NO state ops)");
        Console.WriteLine("           ? ExecutorB (class, NO state ops)");
        Console.WriteLine("  ? Fan-In  ? AggregatorExecutor (class, WITH state ops)");
        Console.WriteLine("  ? FinalExecutor (class)\n");

        Console.WriteLine(new string('=', 80));
        Console.WriteLine("EXECUTING TEST WORKFLOW");
        Console.WriteLine(new string('=', 80) + "\n");

        await using StreamingRun run = await InProcessExecution.StreamAsync(workflow, "START");

        bool receivedOutput = false;
        await foreach (WorkflowEvent evt in run.WatchStreamAsync())
        {
            if (evt is WorkflowOutputEvent output)
            {
                receivedOutput = true;
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

        Console.WriteLine("\n? Test 06 Complete!\n");
        Console.WriteLine($"Test Configuration: STATE_IN_AGGREGATOR = {USE_STATE_IN_AGGREGATOR}\n");
        
        if (receivedOutput)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("?? SUCCESS! Aggregator was called!");
            Console.WriteLine("? State operations in AGGREGATOR work fine!");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("? FAILED! Aggregator was NOT called!");
            Console.WriteLine("? State operations even in aggregator break fan-in!");
            Console.ResetColor();
        }
        
        Console.WriteLine();
        Console.WriteLine("Complete Test Results:");
        Console.WriteLine("  Test01 (no state, sync):                       ? WORKS");
        Console.WriteLine("  Test02 (no state, class-based):                ? WORKS");
        Console.WriteLine("  Test03 (mock async, no context state):         ? WORKS");
        Console.WriteLine("  Test04 (executors: context state, blocking):   ? FAILS");
        Console.WriteLine("  Test05 (executors: context state, async):      ? FAILS");
        Console.WriteLine($"  Test06 (aggregator: context state):            {(receivedOutput ? "? WORKS" : "? FAILS")}");
    }

    // --------------------- Workflow Building ---------------------
    private static Workflow BuildWorkflow()
    {
        Console.WriteLine("Building workflow with state operations ONLY in aggregator...\n");

        var startExecutor = new StartExecutor();
        var executorA = new ExecutorA_NoState();
        var executorB = new ExecutorB_NoState();
        var aggregator = new AggregatorWithState();
        var finalExecutor = new FinalExecutor();

        return new WorkflowBuilder(startExecutor)
            .AddFanOutEdge(startExecutor, targets: [executorA, executorB])
            .AddFanInEdge(aggregator, sources: [executorA, executorB])
            .AddEdge(aggregator, finalExecutor)
            .WithOutputFrom(finalExecutor)
            .Build();
    }

    // --------------------- Executors ---------------------

    private sealed class StartExecutor : 
        ReflectingExecutor<StartExecutor>, 
        IMessageHandler<string>
    {
        public StartExecutor() : base("StartExecutor") { }

        public async ValueTask HandleAsync(string message, IWorkflowContext context, CancellationToken ct = default)
        {
            Console.WriteLine($"[StartExecutor] Broadcasting to ExecutorA and ExecutorB");
            await context.SendMessageAsync(message, cancellationToken: ct);
        }
    }

    /// <summary>
    /// Executor A - NO state operations (just returns a value)
    /// This should work like Test01/Test02
    /// </summary>
    private sealed class ExecutorA_NoState : 
        ReflectingExecutor<ExecutorA_NoState>, 
        IMessageHandler<string, string>
    {
        public ExecutorA_NoState() : base("ExecutorA") { }

        public ValueTask<string> HandleAsync(string message, IWorkflowContext context, CancellationToken ct = default)
        {
            Console.WriteLine($"[ExecutorA] Processing: {message} (NO state operations)");
            var result = "[ExecutorA] ? Completed - NO state ops";
            Console.WriteLine($"[ExecutorA] Returning: {result}\n");
            return ValueTask.FromResult(result);
        }
    }

    /// <summary>
    /// Executor B - NO state operations (just returns a value)
    /// This should work like Test01/Test02
    /// </summary>
    private sealed class ExecutorB_NoState : 
        ReflectingExecutor<ExecutorB_NoState>, 
        IMessageHandler<string, string>
    {
        public ExecutorB_NoState() : base("ExecutorB") { }

        public ValueTask<string> HandleAsync(string message, IWorkflowContext context, CancellationToken ct = default)
        {
            Console.WriteLine($"[ExecutorB] Processing: {message} (NO state operations)");
            var result = "[ExecutorB] ? Completed - NO state ops";
            Console.WriteLine($"[ExecutorB] Returning: {result}\n");
            return ValueTask.FromResult(result);
        }
    }

    /// <summary>
    /// Aggregator - USES context state operations
    /// Let's see if state operations work HERE instead of in the fan-out executors
    /// </summary>
    private sealed class AggregatorWithState : 
        ReflectingExecutor<AggregatorWithState>, 
        IMessageHandler<string, string>
    {
        private readonly List<string> _messages = new();

        public AggregatorWithState() : base("AggregatorExecutor") { }

        public async ValueTask<string> HandleAsync(string message, IWorkflowContext context, CancellationToken ct = default)
        {
            Console.WriteLine($"[Aggregator] ? HandleAsync CALLED!");
            Console.WriteLine($"[Aggregator] Received: {message}");
            
            _messages.Add(message);

            if (_messages.Count >= 2)
            {
                Console.WriteLine($"[Aggregator] ? Both messages collected!\n");
                
                if (USE_STATE_IN_AGGREGATOR)
                {
                    Console.WriteLine("[Aggregator] ?? Now using context state operations...");
                    
                    // ?? CRITICAL TEST: Use context state operations IN THE AGGREGATOR
                    var state = await ReadStateAsync(context);
                    state.Data["aggregator"] = "Stored by aggregator";
                    state.Data["message_count"] = _messages.Count.ToString();
                    await SaveStateAsync(context, state);
                    
                    Console.WriteLine($"[Aggregator] ? Saved state: {state.Data.Count} items");
                }
                
                var result = string.Join("\n", _messages);
                _messages.Clear();
                return result;
            }

            return null!;
        }
    }

    private sealed class FinalExecutor : 
        ReflectingExecutor<FinalExecutor>, 
        IMessageHandler<string, string>
    {
        public FinalExecutor() : base("FinalExecutor") { }

        public ValueTask<string> HandleAsync(string message, IWorkflowContext context, CancellationToken ct = default)
        {
            Console.WriteLine($"[FinalExecutor] ? Received aggregated result!");
            return ValueTask.FromResult($"? Complete! Aggregated:\n{message}");
        }
    }
}
