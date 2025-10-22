// SPDX-License-Identifier: LicenseRef-MAFPlayground-NPU-1.0-CH
// Copyright (c) 2025 Jose Luis Latorre

using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Reflection;
using Microsoft.Extensions.AI;
using MAFPlayground.Utils;

/// <summary>
/// This sample introduces concurrent execution using "fan-out" and "fan-in" patterns.
///
/// Unlike sequential workflows where executors run one after another, this workflow
/// runs multiple executors in parallel to process the same input simultaneously.
///
/// The workflow structure:
/// 1. StartExecutor sends the same question to two AI agents concurrently (fan-out)
/// 2. Physicist Agent and Chemist Agent answer independently and in parallel
/// 3. AggregationExecutor collects both responses and combines them (fan-in)
///
/// This pattern is useful when you want multiple perspectives on the same input,
/// or when you can break work into independent parallel tasks for better performance.
/// </summary>
/// <remarks>
/// Pre-requisites:
/// - Foundational samples should be completed first.
/// - An Azure OpenAI chat completion deployment must be configured.
/// </remarks>
internal static class Demo06_ConcurrentWorkflowMixed
{
    public static async Task Execute()
    {
        // Set up the Azure OpenAI client
        var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")
            ?? throw new InvalidOperationException("AZURE_OPENAI_ENDPOINT is not set.");
        var deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME") ?? "gpt-4o-mini";

        // Get chat client and adapt to IChatClient expected by the workflow helpers
        // ISSUE: The AzureCliCredential() below requires that you have the Azure CLI installed and have logged in.
        // var chatClient = // causes issue
        var chatClient = 
            new AzureOpenAIClient(new Uri(endpoint), new AzureCliCredential())
            .GetChatClient(deploymentName);
        IChatClient iChatClient = chatClient.AsIChatClient();

        // Create the executors (agents)
        ChatClientAgent physicist = new ChatClientAgent(
            iChatClient,
            name: "Physicist",
            instructions: "You are an expert in physics. You answer questions from a physics perspective."
        );

        ChatClientAgent chemist = new ChatClientAgent(
            iChatClient,
            name: "Chemist",
            instructions: "You are an expert in chemistry. You answer questions from a chemistry perspective."
        );

        // Create the executors
        UppercaseExecutor uppercase = new();
        ReverseTextExecutor reverse = new();
        var startExecutor = new ConcurrentStartExecutor();
        var aggregationExecutor = new ConcurrentAggregationExecutor();

        // Build the workflow by adding executors and connecting them
        //WorkflowBuilder builder = new(uppercase);
        //builder.AddEdge(uppercase, reverse).WithOutputFrom(reverse);
        //var workflow = builder.Build();

        // ISSUE: need fpr an explicit cast-conversion to Executor (ExecutorIsh)
        //ExecutorIsh PhysicistExecutor = physicist.AsExecutor(Name : "Physicist_001", Description: "Description");

        var workflow = new WorkflowBuilder(uppercase)
            .AddEdge(uppercase, reverse).WithOutputFrom(reverse)
            .AddEdge(reverse,startExecutor)
            .AddFanOutEdge(startExecutor, targets: [physicist, chemist])
            .AddFanInEdge(aggregationExecutor, sources: [physicist, chemist])
            .WithOutputFrom(aggregationExecutor)
            .Build();

        //// Mermaid
        //Console.WriteLine("Mermaid string: \n=======");
        //var mermaid = workflow.ToMermaidString();
        //Console.WriteLine(mermaid);
        //Console.WriteLine("=======");

        //// DOT
        //Console.WriteLine("DiGraph string: *** Tip: To export DOT as an image, install Graphviz and pipe the DOT output to 'dot -Tsvg', 'dot -Tpng', etc. *** \n=======");
        //var dotString = workflow.ToDotString();
        //Console.WriteLine(dotString);
        //Console.WriteLine("=======");
        // Use the WorkflowVisualizerTool utility to print both diagrams
        WorkflowVisualizerTool.PrintAll(workflow, "Sample 05: Concurrent Workflow Visualization");


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
}

/// <summary>
/// Executor that starts the concurrent processing by sending messages to the agents.
/// </summary>
internal sealed class ConcurrentStartExecutor : ReflectingExecutor<ConcurrentStartExecutor>, IMessageHandler<string>
{
    public ConcurrentStartExecutor() : base("ConcurrentStartExecutor") { }

    /// <summary>
    /// Starts the concurrent processing by sending messages to the agents.
    /// </summary>
    public async ValueTask HandleAsync(string message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        // Broadcast the message to all connected agents. Receiving agents will queue
        // the message but will not start processing until they receive a turn token.
        await context.SendMessageAsync(new ChatMessage(ChatRole.User, message), cancellationToken: cancellationToken).ConfigureAwait(false);

        // Broadcast the turn token to kick off the agents.
        await context.SendMessageAsync(new TurnToken(emitEvents: true), cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// Executor that aggregates the results from the concurrent agents.
/// </summary>
internal sealed class ConcurrentAggregationExecutor : ReflectingExecutor<ConcurrentAggregationExecutor>, IMessageHandler<ChatMessage>
{
    public ConcurrentAggregationExecutor() : base("ConcurrentAggregationExecutor") { }

    private readonly List<ChatMessage> _messages = new List<ChatMessage>();

    /// <summary>
    /// Handles incoming messages from the agents and aggregates their responses.
    /// </summary>
    public async ValueTask HandleAsync(ChatMessage message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        this._messages.Add(message);

        // When both agents have replied, aggregate and emit the combined output.
        if (this._messages.Count == 2)
        {
            var formattedMessages = string.Join(Environment.NewLine, this._messages.Select(m => $"{m.AuthorName}: {m.Text}"));
            await context.YieldOutputAsync(formattedMessages, cancellationToken).ConfigureAwait(false);
        }

        await Task.CompletedTask;
    }
}

/// <summary>
/// First executor: converts input text to uppercase.
/// </summary>
internal sealed class UppercaseExecutor() : ReflectingExecutor<UppercaseExecutor>("UppercaseExecutor"), IMessageHandler<string, string>
{
    /// <summary>
    /// Processes the input message by converting it to uppercase.
    /// </summary>
    /// <param name="message">The input text to convert</param>
    /// <param name="context">Workflow context for accessing workflow services and adding events</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.
    /// The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>The input text converted to uppercase</returns>
    public async ValueTask<string> HandleAsync(string message, IWorkflowContext context, CancellationToken cancellationToken = default) =>
        message.ToUpperInvariant(); // The return value will be sent as a message along an edge to subsequent executors
}

/// <summary>
/// Second executor: reverses the input text and completes the workflow.
/// </summary>
internal sealed class ReverseTextExecutor() : ReflectingExecutor<ReverseTextExecutor>("ReverseTextExecutor"), IMessageHandler<string, string>
{
    /// <summary>
    /// Processes the input message by reversing the text.
    /// </summary>
    /// <param name="message">The input text to reverse</param>
    /// <param name="context">Workflow context for accessing workflow services and adding events</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.
    /// The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>The input text reversed</returns>
    public async ValueTask<string> HandleAsync(string message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        // Because we do not suppress it, the returned result will be yielded as an output from this executor.
        return string.Concat(message.Reverse());
    }
}