// SPDX-License-Identifier: LicenseRef-MAFPlayground-NPU-1.0-CH OR MIT
// Copyright (c) 2025 Jose Luis Latorre
// Note: this demo has been ported to the Microsoft Agent Framework from here and you can find it as a sample.

using MAFPlayground.Utils;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Reflection;
using Microsoft.Extensions.AI;

namespace MAFPlayground.Samples;

/// <summary>
/// This sample demonstrates how to compose workflows hierarchically by converting
/// workflows into agents and using them as executors in larger workflows.
///
/// Workflow composition hierarchy:
/// 1. Build a "Text Processing Sub-Workflow" (uppercase → reverse → append suffix)
/// 2. Convert it to an agent using .AsAgent()
/// 3. Build a "Main Workflow" that uses the sub-workflow agent as an executor
/// 4. Demonstrate that workflows can be nested and reused as modular components
///
/// This pattern enables building complex workflows from simpler, reusable workflow modules.
/// </summary>
/// <remarks>
/// Pre-requisites:
/// - Foundational workflow samples should be completed first.
/// </remarks>
internal static class Demo05_SubWorkflows
{
    public static async Task Execute()
    {
        Console.WriteLine("=== Sample 09: Workflow as Agent (Hierarchical Composition) ===\n");

        // ====================================
        // Step 1: Build a simple text processing sub-workflow
        // ====================================
        Console.WriteLine("Building sub-workflow: Uppercase → Reverse → Append Suffix...\n");


        Func<string, IWorkflowContext, CancellationToken, ValueTask<string>> reverseFunc =
            async (text, ctx, ct) => string.Concat(text.Reverse());
        var reverseExecutor = reverseFunc.AsExecutor("ReverseExecutor");


        var uppercaseExecutor = new UppercaseExecutor();
        //var reverseExecutor = new ReverseExecutor();
        var appendExecutor = new AppendSuffixExecutor(" [PROCESSED]");

        var subWorkflow = new WorkflowBuilder(uppercaseExecutor)
            .AddEdge(uppercaseExecutor, reverseExecutor)
            .AddEdge(reverseExecutor, appendExecutor)
            .WithOutputFrom(appendExecutor)
            .Build();

        // Visualize the sub-workflow
        WorkflowVisualizerTool.PrintAll(subWorkflow, "Sub-Workflow: upper-reverse-append");

        // ISSUE: Why not do it identical to Python, with WorkflowExecutor that wraps a Workflow and exposes it as an Executor.
        // # wrap it as an executor
        // sub_exec = WorkflowExecutor(sub, id = "sub_workflow")
        ExecutorIsh subWorkflowExecutor = subWorkflow.ConfigureSubWorkflow("subWorkflow");

        // ====================================
        // Step 3: Build a main workflow that uses the sub-workflow 
        // ====================================
        Console.WriteLine("Building main workflow that uses the sub-workflow as an executor...\n");

        var prefixExecutor = new PrefixExecutor("INPUT: ");
        var postProcessExecutor = new PostProcessExecutor();

        // SOLUTION: Implement WorkflowExecutor that wraps a Workflow and exposes it as an Executor.
        // same as in Python: https://learn.microsoft.com/en-us/agent-framework/migration-guide/from-autogen/#agent-framework-workflow-nesting where it is imlpemented
        //WorkflowExecutor we = new WorkflowExecutor(subWorkflow);

        // THis fails:
        var mainWorkflow = new WorkflowBuilder(prefixExecutor)
            .AddEdge(prefixExecutor, subWorkflowExecutor)  // ✨ Use the sub-workflow agent as an executor!
            .AddEdge(subWorkflowExecutor, postProcessExecutor)
            .WithOutputFrom(postProcessExecutor)
            .Build();

        // Visualize the main workflow
        WorkflowVisualizerTool.PrintAll(mainWorkflow, "Main Workflow (with Sub-Workflow Agent)");

        // ISSUE: workflow.ToMermaidString() does not show sub-workflow details, just the node.
        // It would be nice to be able to expand/collapse sub-workflows in the visualization.
        // or to define a depth level to which to expand the workflow for visualization.

        //// ====================================
        //// Step 4: Execute the main workflow
        //// ====================================
        Console.WriteLine("Executing main workflow with input: 'Hello, Workflows!'\n");

        await using StreamingRun run = await InProcessExecution.StreamAsync(mainWorkflow, "Hello, Workflows!");
        await foreach (WorkflowEvent evt in run.WatchStreamAsync().ConfigureAwait(false))
        {
            if (evt is WorkflowOutputEvent output)
            {
                Console.WriteLine($"\n=== Main Workflow Completed ===");
                Console.WriteLine($"Final Output: {output.Data}\n");
            }
        }

        Console.WriteLine("\n✅ Sample 09 Complete: Workflows can be composed hierarchically ");
    }
}

// ====================================
// Text Processing Executors
// ====================================

/// <summary>
/// Executor that converts text to uppercase.
/// </summary>
internal sealed class UppercaseExecutor : ReflectingExecutor<UppercaseExecutor>, IMessageHandler<string, string>
{
    public UppercaseExecutor() : base("UppercaseExecutor") { }

    public ValueTask<string> HandleAsync(string message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        var result = message.ToUpperInvariant();
        Console.WriteLine($"[Uppercase] '{message}' → '{result}'");
        return ValueTask.FromResult(result);
    }
}

/// <summary>
/// Executor that reverses text.
/// </summary>
//internal sealed class ReverseExecutor : ReflectingExecutor<ReverseExecutor>, IMessageHandler<string, string>
//{
//    public ReverseExecutor() : base("ReverseExecutor") { }

//    public ValueTask<string> HandleAsync(string message, IWorkflowContext context, CancellationToken cancellationToken = default)
//    {
//        var result = string.Concat(message.Reverse());
//        Console.WriteLine($"[Reverse] '{message}' → '{result}'");
//        return ValueTask.FromResult(result);
//    }
//}

/// <summary>
/// Executor that appends a suffix to text.
/// </summary>
internal sealed class AppendSuffixExecutor : ReflectingExecutor<AppendSuffixExecutor>, IMessageHandler<string, string>
{
    private readonly string _suffix;

    public AppendSuffixExecutor(string suffix) : base("AppendSuffixExecutor")
    {
        _suffix = suffix;
    }

    public ValueTask<string> HandleAsync(string message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        var result = message + _suffix;
        Console.WriteLine($"[AppendSuffix] '{message}' → '{result}'");
        return ValueTask.FromResult(result);
    }
}

/// <summary>
/// Executor that adds a prefix to text (used in main workflow).
/// </summary>
internal sealed class PrefixExecutor : ReflectingExecutor<PrefixExecutor>, IMessageHandler<string, string>
{
    private readonly string _prefix;

    public PrefixExecutor(string prefix) : base("PrefixExecutor")
    {
        _prefix = prefix;
    }

    public ValueTask<string> HandleAsync(string message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        var result = _prefix + message;
        Console.WriteLine($"[Prefix] '{message}' → '{result}'");
        return ValueTask.FromResult(result);
    }
}

/// <summary>
/// Executor that performs final post-processing (used in main workflow).
/// </summary>
internal sealed class PostProcessExecutor : ReflectingExecutor<PostProcessExecutor>, IMessageHandler<string, string>
{
    public PostProcessExecutor() : base("PostProcessExecutor") { }

    public ValueTask<string> HandleAsync(string message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        var result = $"[FINAL] {message} [END]";
        Console.WriteLine($"[PostProcess] '{message}' → '{result}'");
        return ValueTask.FromResult(result);
    }
}