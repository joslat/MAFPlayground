// SPDX-License-Identifier: LicenseRef-MAFPlayground-NPU-1.0-CH
// Copyright (c) 2025 Jose Luis Latorre

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.AI.OpenAI;
using MAFPlayground.Utils;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Reflection;
using Microsoft.Extensions.AI;
using OpenAI;

namespace MAFPlayground.Samples;

/// <summary>
/// This sample demonstrates using a workflow-as-agent as a nested sub-workflow within a larger workflow.
///
/// Architecture:
/// 1. Build a "Text Transformation Sub-Workflow" (prefix → uppercase → reverse → postfix)
/// 2. Convert it to an agent using .AsAgent()
/// 3. Build a "Main Workflow" that:
///    - Prompts for user input
///    - Processes it through the sub-workflow agent
///    - Passes the result to a real AI agent for explanation
///
/// This demonstrates hierarchical workflow composition where a workflow-agent acts as
/// a reusable, modular component within a larger orchestration.
/// </summary>
/// <remarks>
/// Pre-requisites:
/// - Samples 09 (SubWorkflows) and 10 (WorkflowAsAgent) should be completed first.
/// - An Azure OpenAI chat completion deployment must be configured.
/// </remarks>
internal static class Sample11_WorkflowAsAgentNested
{
    public static async Task Execute()
    {
        Console.WriteLine("=== Sample 11: Workflow as Agent (Nested Sub-Workflow) ===\n");

        // Set up the Azure OpenAI client using AIConfig
        var azureClient = new AzureOpenAIClient(AIConfig.Endpoint, AIConfig.KeyCredential);
        var deploymentName = "gpt-4o-mini";
        IChatClient chatClient = azureClient.GetChatClient(deploymentName).AsIChatClient();

        // ====================================
        // Step 1: Build the text transformation sub-workflow
        // ====================================
        Console.WriteLine("Building text transformation sub-workflow...\n");

        var subStartExecutor = new SubWorkflowStartExecutor();
        var prefixExecutor = new AddPrefixExecutor("[START] ");
        var uppercaseExecutor = new UppercaseTransformExecutor();
        var reverseExecutor = new ReverseTransformExecutor();
        var postfixExecutor = new AddPostfixExecutor(" [END]");
        var subOutputExecutor = new SubWorkflowOutputExecutor();

        // Build sub-workflow that takes List<ChatMessage> input
        var wrappedWorkflowTask = new WorkflowBuilder(subStartExecutor)
            .AddEdge(subStartExecutor, prefixExecutor)
            .AddEdge(prefixExecutor, uppercaseExecutor)
            .AddEdge(uppercaseExecutor, reverseExecutor)
            .AddEdge(reverseExecutor, postfixExecutor)
            .AddEdge(postfixExecutor, subOutputExecutor)
            .WithOutputFrom(subOutputExecutor)
            .BuildAsync<List<ChatMessage>>();

        var wrappedWorkflow = await wrappedWorkflowTask;
        WorkflowVisualizerTool.PrintAll(wrappedWorkflow, "Sub-Workflow (Text Transformation)");

        // ====================================
        // Step 2: Convert the sub-workflow to an agent
        // ====================================
        Console.WriteLine("Converting text transformation workflow to agent...\n");

        AIAgent textTransformAgent = wrappedWorkflow.AsAgent(
            "text-transform-agent",
            "Agent that applies prefix, uppercase, reverse, and postfix transformations to text");

        // ====================================
        // Step 3: Create a real AI agent for explanation
        // ====================================
        Console.WriteLine("Creating AI explanation agent...\n");

        ChatClientAgent explanationAgent = new(
            chatClient,
            new ChatClientAgentOptions(
                name: "ExplanationAgent",
                instructions: @"You are a helpful assistant that explains what transformations were applied to text.
Given a transformed text, describe what changes were made in a clear, friendly way.
Be specific about prefixes, suffixes, case changes, and reversals.")
        );

        // ====================================
        // Step 4: Build the main workflow using the sub-workflow agent
        // ====================================
        Console.WriteLine("Building main workflow with nested sub-workflow agent...\n");

        var inputPromptExecutor = new InputPromptExecutor();
        var transformBridgeExecutor = new TransformBridgeExecutor();

        var mainWorkflow = new WorkflowBuilder(inputPromptExecutor)
            .AddFanOutEdge(inputPromptExecutor, targets: [textTransformAgent])
            .AddFanInEdge(transformBridgeExecutor, sources: [textTransformAgent])
            .AddFanOutEdge(transformBridgeExecutor, targets: [explanationAgent])
            .WithOutputFrom(explanationAgent)
            .Build();

        // ISSUE: I think we've hit a fundamental limitation of the framework: workflow-as-agent cannot be properly composed within another workflow using the current patterns. The agent doesn't emit its output in a way that fan-in executors can capture.



        // Visualize the main workflow
        WorkflowVisualizerTool.PrintAll(mainWorkflow, "Main Workflow (with Nested Sub-Workflow Agent)");

        // ====================================
        // Step 5: Execute the main workflow interactively
        // ====================================
        Console.WriteLine("\n✨ Main workflow ready! The sub-workflow has been embedded as an agent.\n");

        while (true)
        {
            Console.WriteLine();
            Console.Write("Enter a phrase (or 'exit' to quit): ");
            string? input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input) || input.Equals("exit", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Exiting Sample 11.");
                break;
            }

            Console.WriteLine("\n--- Executing Main Workflow ---\n");

            var inputMessage = new List<ChatMessage> { new(ChatRole.User, input) };

            await using StreamingRun run = await InProcessExecution.StreamAsync(mainWorkflow, inputMessage);
            await foreach (WorkflowEvent evt in run.WatchStreamAsync().ConfigureAwait(false))
            {
                if (evt is AgentRunUpdateEvent agentUpdate)
                {
                    Console.Write(agentUpdate.Update.Text);
                }
                else if (evt is WorkflowOutputEvent output)
                {
                    Console.WriteLine("\n\n=== Workflow Complete ===\n");
                }
            }
        }

        Console.WriteLine("\n✅ Sample 11 Complete: Sub-workflow agent successfully nested in main workflow!");
    }
}

// ====================================
// Sub-Workflow Executors
// ====================================

/// <summary>
/// Executor that starts the sub-workflow by extracting text from ChatMessage.
/// </summary>
internal sealed class SubWorkflowStartExecutor : ReflectingExecutor<SubWorkflowStartExecutor>, IMessageHandler<List<ChatMessage>, string>
{
    public SubWorkflowStartExecutor() : base("SubWorkflowStartExecutor") { }

    public ValueTask<string> HandleAsync(List<ChatMessage> message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        var text = message.LastOrDefault()?.Text ?? string.Empty;
        Console.WriteLine($"[SubWorkflowStart] Received: '{text}'");
        return ValueTask.FromResult(text);
    }
}

/// <summary>
/// Executor that outputs the result from the sub-workflow as a ChatMessage.
/// Returns the ChatMessage so it flows through the workflow's output.
/// </summary>
internal sealed class SubWorkflowOutputExecutor : ReflectingExecutor<SubWorkflowOutputExecutor>, IMessageHandler<string, ChatMessage>
{
    public SubWorkflowOutputExecutor() : base("SubWorkflowOutputExecutor") { }

    public ValueTask<ChatMessage> HandleAsync(string message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[SubWorkflowOutput] Converting to ChatMessage: '{message}'");
        
        // Return as ChatMessage - this should flow as the workflow output
        var chatMessage = new ChatMessage(ChatRole.Assistant, message);
        return ValueTask.FromResult(chatMessage);
    }
}

// ====================================
// Text Transformation Executors
// ====================================

internal sealed class AddPrefixExecutor : ReflectingExecutor<AddPrefixExecutor>, IMessageHandler<string, string>
{
    private readonly string _prefix;

    public AddPrefixExecutor(string prefix) : base("AddPrefixExecutor")
    {
        _prefix = prefix;
    }

    public ValueTask<string> HandleAsync(string message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        var result = _prefix + message;
        Console.WriteLine($"[AddPrefix] '{message}' → '{result}'");
        return ValueTask.FromResult(result);
    }
}

internal sealed class UppercaseTransformExecutor : ReflectingExecutor<UppercaseTransformExecutor>, IMessageHandler<string, string>
{
    public UppercaseTransformExecutor() : base("UppercaseTransformExecutor") { }

    public ValueTask<string> HandleAsync(string message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        var result = message.ToUpperInvariant();
        Console.WriteLine($"[Uppercase] '{message}' → '{result}'");
        return ValueTask.FromResult(result);
    }
}

internal sealed class ReverseTransformExecutor : ReflectingExecutor<ReverseTransformExecutor>, IMessageHandler<string, string>
{
    public ReverseTransformExecutor() : base("ReverseTransformExecutor") { }

    public ValueTask<string> HandleAsync(string message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        var result = string.Concat(message.Reverse());
        Console.WriteLine($"[Reverse] '{message}' → '{result}'");
        return ValueTask.FromResult(result);
    }
}

internal sealed class AddPostfixExecutor : ReflectingExecutor<AddPostfixExecutor>, IMessageHandler<string, string>
{
    private readonly string _postfix;

    public AddPostfixExecutor(string postfix) : base("AddPostfixExecutor")
    {
        _postfix = postfix;
    }

    public ValueTask<string> HandleAsync(string message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        var result = message + _postfix;
        Console.WriteLine($"[AddPostfix] '{message}' → '{result}'");
        return ValueTask.FromResult(result);
    }
}

// ====================================
// Main Workflow Executors
// ====================================

/// <summary>
/// Executor that starts the main workflow by sending input to the transform agent.
/// </summary>
internal sealed class InputPromptExecutor : ReflectingExecutor<InputPromptExecutor>, IMessageHandler<List<ChatMessage>>
{
    public InputPromptExecutor() : base("InputPromptExecutor") { }

    public async ValueTask HandleAsync(List<ChatMessage> message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        var text = message.LastOrDefault()?.Text ?? string.Empty;
        Console.WriteLine($"[InputPrompt] Received input: '{text}'");
        Console.WriteLine("[InputPrompt] Sending to text transformation sub-workflow agent...\n");
        
        await context.SendMessageAsync(message, cancellationToken: cancellationToken).ConfigureAwait(false);
        await context.SendMessageAsync(new TurnToken(emitEvents: true), cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// Executor that bridges the transform agent output to the explanation agent.
/// </summary>
internal sealed class TransformBridgeExecutor : ReflectingExecutor<TransformBridgeExecutor>, IMessageHandler<ChatMessage>
{
    public TransformBridgeExecutor() : base("TransformBridgeExecutor") { }

    public async ValueTask HandleAsync(ChatMessage message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[TransformBridge] Received transformed result: '{message.Text}'");
        Console.WriteLine("[TransformBridge] Sending to explanation agent...\n");
        
        var explanationPrompt = new List<ChatMessage> 
        { 
            new(ChatRole.User, $"The text was transformed to: '{message.Text}'. Please explain what transformations were applied.")
        };
        
        await context.SendMessageAsync(explanationPrompt, cancellationToken: cancellationToken).ConfigureAwait(false);
        await context.SendMessageAsync(new TurnToken(emitEvents: true), cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}