// SPDX-License-Identifier: LicenseRef-MAFPlayground-NPU-1.0-CH
// Copyright (c) 2025 Jose Luis Latorre

using Azure.AI.OpenAI;
using MAFPlayground.Utils;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Reflection;
using Microsoft.Extensions.AI;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MAFPlayground.Samples;

/// <summary>
/// This sample combines concurrent execution (fan-out/fan-in) with conditional routing.
///
/// Workflow structure:
/// 1. StartExecutor sends the same question to two AI agents concurrently (fan-out)
/// 2. Physicist Agent and Chemist Agent answer independently and in parallel
/// 3. AggregationExecutor collects both responses and combines them (fan-in)
/// 4. Quality Check Executor evaluates the aggregated response
/// 5. Based on quality:
///    - High quality → Approval Executor (approves and outputs)
///    - Low quality → Rejection Executor (rejects and outputs feedback)
///
/// This demonstrates how to combine parallel processing with decision-based routing
/// for sophisticated workflow automation.
/// </summary>
/// <remarks>
/// Pre-requisites:
/// - Foundational samples should be completed first.
/// - An Azure OpenAI chat completion deployment must be configured.
/// </remarks>
internal static class Sample08_ConcurrentWithConditional
{
    public static async Task Execute()
    {
        // Set up the Azure OpenAI client using AIConfig
        var azureClient = new AzureOpenAIClient(AIConfig.Endpoint, AIConfig.KeyCredential);
        var deploymentName = "gpt-4o-mini";

        IChatClient chatClient = azureClient.GetChatClient(deploymentName).AsIChatClient();

        // Create the concurrent agents (physicist and chemist)
        ChatClientAgent physicist = new ChatClientAgent(
            chatClient,
            name: "Physicist",
            instructions: "You are an expert in physics. You answer questions from a physics perspective."
        );

        ChatClientAgent chemist = new ChatClientAgent(
            chatClient,
            name: "Chemist",
            instructions: "You are an expert in chemistry. You answer questions from a chemistry perspective."
        );

        // Create quality check agent
        ChatClientAgent qualityCheckAgent = new ChatClientAgent(
            chatClient,
            new ChatClientAgentOptions(
                name: "QualityChecker",
                instructions: @"You are a quality checker that evaluates the completeness and accuracy of scientific explanations.
Determine if the response is of high quality (comprehensive, accurate, well-explained) or low quality (incomplete, vague, or inaccurate).")
            {
                ChatOptions = new()
                {
                    ResponseFormat = ChatResponseFormat.ForJsonSchema<QualityCheckResult>()
                }
            }
        );

        // Create executors
        var startExecutor = new ConcurrentStartExecutor();
        var aggregationExecutor = new Sample08AggregationExecutor();
        var qualityCheckExecutor = new QualityCheckExecutor(qualityCheckAgent);
        var approvalExecutor = new ApprovalExecutor();
        var rejectionExecutor = new RejectionExecutor();

        // Build the workflow: concurrent fan-out/fan-in followed by conditional routing
        var workflow = new WorkflowBuilder(startExecutor)
            .AddFanOutEdge(startExecutor, targets: [ physicist, chemist ])
            .AddFanInEdge(aggregationExecutor, sources: [ physicist, chemist ])
            .AddEdge(aggregationExecutor, qualityCheckExecutor)
            .AddEdge(qualityCheckExecutor, approvalExecutor, condition: GetQualityCondition(isHighQuality: true))
            .AddEdge(qualityCheckExecutor, rejectionExecutor, condition: GetQualityCondition(isHighQuality: false))
            .WithOutputFrom(approvalExecutor, rejectionExecutor)
            .Build();

        // ISSUE:Agent Output is Buffered/Delayed
        // The workflow runs all executors asynchronously, but you only observe WorkflowOutputEvent events in your loop—which are emitted only at the end when context.YieldOutputAsync() is called by the final executors (ApprovalExecutor or RejectionExecutor).
        //  ChatClientAgent agents (Physicist, Chemist) generate their responses internally during the fan-out phase, but:
        //•	Their text output isn't immediately printed to the console
        //•	The framework buffers the agent responses as ChatMessage objects
        //•	These messages flow through the workflow edges to the aggregator
        //•	The aggregator collects them and passes them forward
        //•	Only when the final executor calls context.YieldOutputAsync() does the full aggregated response(including the agent outputs) get printed


        // ISSUE: The executor gets a weird id instead of something that clearly references the agent.
        // see
        //flowchart TD
        //    ConcurrentStartExecutor["ConcurrentStartExecutor (Start)"];
        //    9cd24477cd214320807e3a444e3ea7ef["9cd24477cd214320807e3a444e3ea7ef"];
        //    a98a1b5c1da9406891aeaac42fa34e69["a98a1b5c1da9406891aeaac42fa34e69"];
        //    Sample08AggregationExecutor["Sample08AggregationExecutor"];
        //    QualityCheckExecutor["QualityCheckExecutor"];
        //    ApprovalExecutor["ApprovalExecutor"];
        //    RejectionExecutor["RejectionExecutor"];


        // Visualize the workflow
        WorkflowVisualizerTool.PrintAll(workflow, "Sample 08: Concurrent + Conditional Workflow Visualization");

        // Execute the workflow in streaming mode
        await using StreamingRun run = await InProcessExecution.StreamAsync(workflow, "What is temperature?");
        await foreach (WorkflowEvent evt in run.WatchStreamAsync().ConfigureAwait(false))
        {
            if (evt is WorkflowOutputEvent output)
            {
                Console.WriteLine($"Workflow completed with results:\n{output.Data}");
            }
        }
    }

    /// <summary>
    /// Creates a condition for routing based on quality check result.
    /// </summary>
    private static Func<object?, bool> GetQualityCondition(bool isHighQuality) =>
        result => result is QualityCheckResult qcr && qcr.IsHighQuality == isHighQuality;
}

/// <summary>
/// Aggregation executor that collects responses from concurrent agents and returns formatted text.
/// IMPORTANT: This executor waits until both messages are received before returning.
/// </summary>
internal sealed class Sample08AggregationExecutor : ReflectingExecutor<Sample08AggregationExecutor>, IMessageHandler<ChatMessage, string>
{
    public Sample08AggregationExecutor() : base("Sample08AggregationExecutor") { }

    private readonly List<ChatMessage> _messages = new List<ChatMessage>();

    /// <summary>
    /// Handles incoming messages from the agents and aggregates their responses.
    /// Only returns a value when both agent responses have been received.
    /// </summary>
    public async ValueTask<string> HandleAsync(ChatMessage message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[Aggregation] Received message from {message.AuthorName}:\n{message.Text}\n");
        this._messages.Add(message);

        if (this._messages.Count >= 2)
        {
            var formattedMessages = string.Join(
                Environment.NewLine + Environment.NewLine, 
                this._messages.Select(m => $"[{m.AuthorName}]: {m.Text}"));
            
            Console.WriteLine($"[Aggregation] Collected {this._messages.Count} responses, proceeding to quality check.");

            return formattedMessages; // ✅ Return completed task
        }

        return default;  // ⚠️ Returns default(ValueTask<string>) - less clear intent
    }
}

/// <summary>
/// Represents the result of a quality check.
/// </summary>
public sealed class QualityCheckResult
{
    [JsonPropertyName("is_high_quality")]
    public bool IsHighQuality { get; set; }

    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;

    [JsonIgnore]
    public string AggregatedResponse { get; set; } = string.Empty;
}

/// <summary>
/// Executor that performs quality check on aggregated responses.
/// </summary>
internal sealed class QualityCheckExecutor : ReflectingExecutor<QualityCheckExecutor>, IMessageHandler<string, QualityCheckResult>
{
    private readonly AIAgent _qualityCheckAgent;

    public QualityCheckExecutor(AIAgent qualityCheckAgent) : base("QualityCheckExecutor")
    {
        this._qualityCheckAgent = qualityCheckAgent;
    }

    public async ValueTask<QualityCheckResult> HandleAsync(string aggregatedResponse, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        // Guard against empty responses (shouldn't happen with corrected aggregator, but safe to check)
        if (string.IsNullOrWhiteSpace(aggregatedResponse))
        {
            Console.WriteLine("[QualityCheck] Received empty response, marking as low quality.");
            return new QualityCheckResult 
            { 
                IsHighQuality = false, 
                Reason = "No response to check yet.",
                AggregatedResponse = string.Empty
            };
        }

        Console.WriteLine($"[QualityCheck] Evaluating response of length {aggregatedResponse.Length}...");

        // Invoke the quality check agent
        var response = await this._qualityCheckAgent.RunAsync(
            $"Evaluate the quality of this scientific explanation:\n\n{aggregatedResponse}",
            cancellationToken: cancellationToken);

        var result = JsonSerializer.Deserialize<QualityCheckResult>(response.Text) ?? new QualityCheckResult();
        result.AggregatedResponse = aggregatedResponse;

        Console.WriteLine($"[QualityCheck] IsHighQuality: {result.IsHighQuality}, Reason: {result.Reason}");

        return result;
    }
}

/// <summary>
/// Executor that approves high-quality responses.
/// </summary>
internal sealed class ApprovalExecutor : ReflectingExecutor<ApprovalExecutor>, IMessageHandler<QualityCheckResult>
{
    public ApprovalExecutor() : base("ApprovalExecutor") { }

    public async ValueTask HandleAsync(QualityCheckResult result, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        if (result.IsHighQuality)
        {
            await context.YieldOutputAsync(
                $"✅ APPROVED: Response meets quality standards.\n\nReason: {result.Reason}\n\nResponse:\n{result.AggregatedResponse}",
                cancellationToken);
        }
        else
        {
            throw new InvalidOperationException("This executor should only handle high-quality responses.");
        }
    }
}

/// <summary>
/// Executor that handles low-quality responses.
/// </summary>
internal sealed class RejectionExecutor : ReflectingExecutor<RejectionExecutor>, IMessageHandler<QualityCheckResult>
{
    public RejectionExecutor() : base("RejectionExecutor") { }

    public async ValueTask HandleAsync(QualityCheckResult result, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        if (!result.IsHighQuality)
        {
            await context.YieldOutputAsync(
                $"❌ REJECTED: Response does not meet quality standards.\n\nReason: {result.Reason}\n\nResponse:\n{result.AggregatedResponse}",
                cancellationToken);
        }
        else
        {
            throw new InvalidOperationException("This executor should only handle low-quality responses.");
        }
    }
}