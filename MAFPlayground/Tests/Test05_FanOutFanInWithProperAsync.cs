// SPDX-License-Identifier: LicenseRef-MAFPlayground-NPU-1.0-CH
// Copyright (c) 2025 Jose Luis

using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Reflection;

namespace MAFPlayground.Tests;

/// <summary>
/// Test 05: Fan-Out/Fan-In with PROPER ASYNC/AWAIT (The Fix!)
/// 
/// This test demonstrates the SOLUTION to the issue found in Test04.
/// Instead of blocking on context state operations with .GetAwaiter().GetResult(),
/// we use proper async/await.
/// 
/// Workflow:
/// StartExecutor (class)
///     ? Fan-Out ? ExecutorA (class, uses async/await properly)
///              ? ExecutorB (class, uses async/await properly)
///     ? Fan-In  ? AggregatorExecutor (class)
///     ? FinalExecutor (class)
/// 
/// Goal: Prove that using proper async/await fixes the fan-in routing issue.
/// 
/// Test Results Summary:
/// - Test01: Function-based, no async ? WORKS
/// - Test02: Class-based, no async ? WORKS
/// - Test03: Class-based, mock async blocking ? WORKS
/// - Test04: Class-based, context state with blocking ? FAILS (aggregator not called)
/// - Test05: Class-based, context state with async/await ? SHOULD WORK!
/// </summary>
internal static class Test05_FanOutFanInWithProperAsync
{
    // ?? TOGGLE FLAG: Compare blocking vs proper async
    // false = Use blocking (.GetAwaiter().GetResult()) - FAILS like Test04
    // true  = Use proper async/await - SHOULD WORK!
    private const bool USE_PROPER_ASYNC = true;

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
        Console.WriteLine("=== Test 05: Fan-Out/Fan-In with PROPER ASYNC/AWAIT (The Fix!) ===\n");
        Console.WriteLine($"??  ASYNC MODE: {(USE_PROPER_ASYNC ? "PROPER ASYNC/AWAIT ?" : "BLOCKING (like Test04) ?")} ??\n");
        Console.WriteLine("Testing the SOLUTION: Using proper async/await for context state operations\n");

        // Build workflow
        var workflow = BuildWorkflow_WithProperAsync();

        Console.WriteLine("--- Workflow Structure ---");
        Console.WriteLine("StartExecutor (class)");
        Console.WriteLine("  ? Fan-Out ? ExecutorA (class, proper async/await)");
        Console.WriteLine("           ? ExecutorB (class, proper async/await)");
        Console.WriteLine("  ? Fan-In  ? AggregatorExecutor (class)");
        Console.WriteLine("  ? FinalExecutor (class)\n");

        Console.WriteLine(new string('=', 80));
        Console.WriteLine("EXECUTING TEST WORKFLOW (PROPER ASYNC)");
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
                Console.WriteLine("? TEST COMPLETE - OUTPUT RECEIVED!");
                Console.ResetColor();
                Console.WriteLine(new string('=', 80));
                Console.WriteLine();
                Console.WriteLine(output.Data);
                Console.WriteLine();
                Console.WriteLine(new string('=', 80));
            }
        }

        Console.WriteLine("\n? Test 05 Complete!\n");
        Console.WriteLine($"Test Configuration: USE_PROPER_ASYNC = {USE_PROPER_ASYNC}\n");
        
        if (receivedOutput)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("?? SUCCESS! The aggregator was called and workflow completed!");
            Console.WriteLine("? PROPER ASYNC/AWAIT FIXES THE ISSUE!");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("? FAILED! The aggregator was NOT called!");
            Console.WriteLine("? This means blocking is still being used somewhere.");
            Console.ResetColor();
        }
        
        Console.WriteLine();
        Console.WriteLine("Test Results Summary:");
        Console.WriteLine("  Test01 (function-based, no async):              ? WORKS");
        Console.WriteLine("  Test02 (class-based, no async):                 ? WORKS");
        Console.WriteLine("  Test03 (class-based, mock async blocking):      ? WORKS");
        Console.WriteLine("  Test04 (class-based, context state blocking):   ? FAILS");
        Console.WriteLine($"  Test05 (class-based, proper async={USE_PROPER_ASYNC}): {(receivedOutput ? "? WORKS!" : "? FAILS")}");
        Console.WriteLine();
        Console.WriteLine("?? Toggle USE_PROPER_ASYNC to compare blocking vs async behavior!");
    }

    // --------------------- Class-Based Workflow ---------------------
    private static Workflow BuildWorkflow_WithProperAsync()
    {
        Console.WriteLine("Building workflow with CLASS-BASED executors using PROPER ASYNC/AWAIT...\n");

        var startExecutor = new StartExecutor_ClassBased();
        var executorA = new ExecutorA_ProperAsync();
        var executorB = new ExecutorB_ProperAsync();
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
    /// Executor A - Uses PROPER ASYNC/AWAIT (THE FIX!)
    /// ? This should work correctly with fan-in!
    /// </summary>
    private sealed class ExecutorA_ProperAsync : 
        ReflectingExecutor<ExecutorA_ProperAsync>, 
        IMessageHandler<string, string>
    {
        public ExecutorA_ProperAsync() : base("ExecutorA") { }

        // ? SOLUTION: Use async/await properly!
        public async ValueTask<string> HandleAsync(
            string message, 
            IWorkflowContext context, 
            CancellationToken cancellationToken = default)
        {
            Console.WriteLine($"[ExecutorA] Received: '{message}'");
            
            string result;
            
            if (USE_PROPER_ASYNC)
            {
                Console.WriteLine($"[ExecutorA] ? Using PROPER ASYNC/AWAIT for context state operations");
                
                // ? THE FIX: Use proper async/await!
                var state = await ReadStateAsync(context);
                state.Data["A"] = $"Processed by A: {message}";
                await SaveStateAsync(context, state);
                
                result = $"[ExecutorA] ? Completed - Added to context state (WITH proper async/await)";
            }
            else
            {
                Console.WriteLine($"[ExecutorA] ??  Using BLOCKING (for comparison with Test04)");
                
                // ? The broken pattern from Test04 (for comparison)
                var state = ReadStateAsync(context).GetAwaiter().GetResult();
                state.Data["A"] = $"Processed by A: {message}";
                SaveStateAsync(context, state).GetAwaiter().GetResult();
                
                result = $"[ExecutorA] ? Completed - Added to context state (WITH blocking - broken)";
            }
            
            Console.WriteLine($"[ExecutorA] Returning: {result}\n");
            return result;
        }
    }

    /// <summary>
    /// Executor B - Uses PROPER ASYNC/AWAIT (THE FIX!)
    /// ? This should work correctly with fan-in!
    /// </summary>
    private sealed class ExecutorB_ProperAsync : 
        ReflectingExecutor<ExecutorB_ProperAsync>, 
        IMessageHandler<string, string>
    {
        public ExecutorB_ProperAsync() : base("ExecutorB") { }

        // ? SOLUTION: Use async/await properly!
        public async ValueTask<string> HandleAsync(
            string message, 
            IWorkflowContext context, 
            CancellationToken cancellationToken = default)
        {
            Console.WriteLine($"[ExecutorB] Received: '{message}'");
            
            string result;
            
            if (USE_PROPER_ASYNC)
            {
                Console.WriteLine($"[ExecutorB] ? Using PROPER ASYNC/AWAIT for context state operations");
                
                // ? THE FIX: Use proper async/await!
                var state = await ReadStateAsync(context);
                state.Data["B"] = $"Processed by B: {message}";
                await SaveStateAsync(context, state);
                
                result = $"[ExecutorB] ? Completed - Added to context state (WITH proper async/await)";
            }
            else
            {
                Console.WriteLine($"[ExecutorB] ??  Using BLOCKING (for comparison with Test04)");
                
                // ? The broken pattern from Test04 (for comparison)
                var state = ReadStateAsync(context).GetAwaiter().GetResult();
                state.Data["B"] = $"Processed by B: {message}";
                SaveStateAsync(context, state).GetAwaiter().GetResult();
                
                result = $"[ExecutorB] ? Completed - Added to context state (WITH blocking - broken)";
            }
            
            Console.WriteLine($"[ExecutorB] Returning: {result}\n");
            return result;
        }
    }

    private sealed class AggregatorExecutor_ClassBased : 
        ReflectingExecutor<AggregatorExecutor_ClassBased>, 
        IMessageHandler<string, string>
    {
        private readonly List<string> _messages = new();
        private const int ExpectedCount = 2;

        public AggregatorExecutor_ClassBased() : base("AggregatorExecutor") { }

        public async ValueTask<string> HandleAsync(
            string message, 
            IWorkflowContext context, 
            CancellationToken cancellationToken = default)
        {
            Console.WriteLine($"[Aggregator] ? HandleAsync CALLED!");
            Console.WriteLine($"[Aggregator] Received message {_messages.Count + 1}/{ExpectedCount}: '{message}'\n");
            
            _messages.Add(message);

            if (_messages.Count >= ExpectedCount)
            {
                Console.WriteLine("[Aggregator] ? Both messages collected!\n");
                
                // Verify state was updated
                var state = await ReadStateAsync(context);
                Console.WriteLine($"[Aggregator] Verifying context state:");
                Console.WriteLine($"  State contains {state.Data.Count} items");
                foreach (var kvp in state.Data)
                {
                    Console.WriteLine($"    {kvp.Key}: {kvp.Value}");
                }
                Console.WriteLine();
                
                var aggregated = string.Join("\n", _messages);
                _messages.Clear();
                return aggregated;
            }

            return null!;
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
            Console.WriteLine("[FinalExecutor] ? Aggregation complete!");
            Console.WriteLine($"[FinalExecutor] Received: '{message}'\n");
            
            var result = $"? Test Complete!\n" +
                       $"Aggregated confirmations:\n{message}\n\n" +
                       $"?? PROPER ASYNC/AWAIT WORKS WITH FAN-IN AND CONTEXT STATE!";
            
            return ValueTask.FromResult(result);
        }
    }
}
