// SPDX-License-Identifier: LicenseRef-MAFPlayground-NPU-1.0-CH
// Copyright (c) 2025 Jose Luis Latorre

using Azure.AI.OpenAI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Reflection;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;

namespace MAFPlayground.Samples.Sample22_WorkshopPlanner;

/// <summary>
/// Sample 22: AI-Powered Workshop Planner with Agentic Workflow
/// 
/// Demonstrates complete workflow with discovery, enrichment loops, and evaluation
/// </summary>
internal static class Sample22_WorkshopPlanner
{
    public static async Task Execute()
    {
        Console.WriteLine("\n╔════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║   Sample 22: AI Workshop Planner - Agentic Workflow      ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════╝\n");

        var outputDirectory = Path.Combine(Directory.GetCurrentDirectory(), "output", "workshop-proposals");

        // ====================================
        // Step 1: Connect to GitHub MCP Server
        // ====================================
        Console.WriteLine("🔌 Connecting to GitHub MCP Server...");
        await using var githubMcpClient = await McpClient.CreateAsync(new StdioClientTransport(new()
        {
            Name = "GitHubMCPServer",
            Command = "npx",
            Arguments = ["-y", "--verbose", "@modelcontextprotocol/server-github"],
        }));
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("✅ GitHub MCP Server connected!");
        Console.ResetColor();

        var githubTools = await githubMcpClient.ListToolsAsync().ConfigureAwait(false);
        Console.WriteLine($"   Found {githubTools.Count} GitHub tools\n");

        // ====================================
        // Step 2: Connect to Microsoft Learn MCP Server
        // ====================================
        Console.WriteLine("🔌 Connecting to Microsoft Learn MCP Server...");
        await using var learnMcpClient = await McpClient.CreateAsync(new HttpClientTransport(new()
        {
            Name = "MicrosoftLearnMCPServer",
            Endpoint = new Uri("https://learn.microsoft.com/api/mcp")
        }));
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("✅ Microsoft Learn MCP Server connected!");
        Console.ResetColor();

        var learnTools = await learnMcpClient.ListToolsAsync().ConfigureAwait(false);
        Console.WriteLine($"   Found {learnTools.Count} Microsoft Learn tools\n");

        // ====================================
        // Step 3: Combine all tools
        // ====================================
        var allTools = new List<AITool>();
        allTools.AddRange(githubTools.Cast<AITool>());
        allTools.AddRange(learnTools.Cast<AITool>());

        var azureClient = new AzureOpenAIClient(AIConfig.Endpoint, AIConfig.KeyCredential);
        var chatClient = azureClient.GetChatClient(AIConfig.ModelDeployment).AsIChatClient();

        var requirementsAgent = Agents.RequirementsAgent.Create(chatClient);
        var discoveryAgent = Agents.DiscoveryAgent.Create(chatClient, allTools);
        var enricherAgent = Agents.EnricherAgent.Create(chatClient, allTools);
        var evaluatorAgent = Agents.EvaluatorAgent.Create(chatClient);
        var plannerAgent = Agents.PlannerAgent.Create(chatClient);

        Console.WriteLine("�� Building agentic workflow...\n");

        Func<string, IWorkflowContext, CancellationToken, ValueTask<WorkshopRequirements>> requirementsFunc =
            (input, ctx, ct) => Executors.RequirementsExecutor.ExecuteAsync(input, ctx, ct, requirementsAgent);
        var requirementsExecutor = requirementsFunc.BindAsExecutor("Requirements");

        // Store requirements for later phases (closure-based state management)
        WorkshopRequirements? sharedRequirements = null;
        ComponentEvaluationState? sharedLoopState = null;

        Func<WorkshopRequirements, IWorkflowContext, CancellationToken, ValueTask<DiscoveryResult>> discoveryFunc =
            (requirements, ctx, ct) => 
            {
                sharedRequirements = requirements;  // Store for later phases
                return Executors.DiscoveryExecutor.ExecuteAsync(requirements, ctx, ct, discoveryAgent, chatClient);
            };
        var discoveryExecutor = discoveryFunc.BindAsExecutor("Discovery");

        // Loop Controller Init - initializes the evaluation queue from discovery results
        Func<DiscoveryResult, IWorkflowContext, CancellationToken, ValueTask<ComponentEvaluationState>> loopInitFunc =
            async (discovery, ctx, ct) =>
            {
                var state = await Executors.EnrichmentLoopControllerExecutor.ExecuteAsync(discovery, ctx, ct);
                sharedLoopState = state;  // Store in closure
                return state;
            };
        var loopInitExecutor = loopInitFunc.BindAsExecutor("LoopInit");

        // Loop Controller - decision point that checks if loop should continue or exit
        // This is a pass-through executor that just returns the state for condition evaluation
        Func<ComponentEvaluationState, IWorkflowContext, CancellationToken, ValueTask<ComponentEvaluationState>> loopControllerFunc =
            async (state, ctx, ct) =>
            {
                // This is the loop decision point - just pass through the state
                // The workflow will evaluate conditions on this state to decide next step
                await ValueTask.CompletedTask;
                return state;
            };
        var loopControllerExecutor = loopControllerFunc.BindAsExecutor("LoopController");

        // Enrichment - processes one candidate at a time
        Func<ComponentEvaluationState, IWorkflowContext, CancellationToken, ValueTask<EnrichedComponent>> enrichmentFunc =
            async (state, ctx, ct) =>
            {
                if (state.CurrentCandidate == null)
                    throw new InvalidOperationException("No candidate to enrich");
                return await Executors.EnrichmentExecutor.ExecuteAsync(state, ctx, ct, enricherAgent);
            };
        var enrichmentExecutor = enrichmentFunc.BindAsExecutor("Enrichment");

        // Evaluation - evaluates one enriched component and returns tuple for loop update
        Func<EnrichedComponent, IWorkflowContext, CancellationToken, ValueTask<EvaluationResult>> evaluationFunc =
            async (enriched, ctx, ct) =>
            {
                if (sharedRequirements == null)
                    throw new InvalidOperationException("Requirements not available");
                
                // Cache enriched component for later aggregation
                Executors.ComponentAggregatorExecutor.CacheEnrichedComponent(enriched);
                
                return await Executors.EvaluationExecutor.ExecuteAsync(
                    (sharedRequirements, enriched), ctx, ct, evaluatorAgent);
            };
        var evaluationExecutor = evaluationFunc.BindAsExecutor("Evaluation");

        // Loop Controller Update - updates state and decides whether to loop or continue
        Func<EvaluationResult, IWorkflowContext, CancellationToken, ValueTask<ComponentEvaluationState>> loopUpdateFunc =
            async (evaluation, ctx, ct) =>
            {
                if (sharedLoopState == null)
                    throw new InvalidOperationException("Loop state not initialized");
                
                var updatedState = await Executors.EnrichmentLoopControllerExecutor.UpdateStateAsync(
                    sharedLoopState, evaluation, ctx, ct);
                
                sharedLoopState = updatedState;  // Update closure state
                
                return updatedState;
            };
        var loopUpdateExecutor = loopUpdateFunc.BindAsExecutor("LoopUpdate");

        // Aggregation - final aggregation when loop completes
        Func<ComponentEvaluationState, IWorkflowContext, CancellationToken, ValueTask<AggregatedComponents>> aggregationFunc =
            (finalState, ctx, ct) => Executors.ComponentAggregatorExecutor.ExecuteAsync(finalState, ctx, ct);
        var aggregationExecutor = aggregationFunc.BindAsExecutor("Aggregation");

        Func<AggregatedComponents, IWorkflowContext, CancellationToken, ValueTask<WorkshopPlan>> planningFunc =
            (components, ctx, ct) => 
            {
                if (sharedRequirements == null)
                    throw new InvalidOperationException("Requirements not available");
                    
                return Executors.WorkshopPlannerExecutor.ExecuteAsync((sharedRequirements, components), ctx, ct, plannerAgent);
            };
        var planningExecutor = planningFunc.BindAsExecutor("Planning");

        Func<WorkshopPlan, IWorkflowContext, CancellationToken, ValueTask<MarkdownDeliverable>> markdownFunc =
            Executors.MarkdownGenerationExecutor.ExecuteAsync;
        var markdownExecutor = markdownFunc.BindAsExecutor("Markdown");

        Func<MarkdownDeliverable, IWorkflowContext, CancellationToken, ValueTask<string>> fileWriterFunc =
            (deliverable, ctx, ct) => Executors.FileWriterExecutor.ExecuteAsync(deliverable, ctx, ct, outputDirectory);
        var fileWriterExecutor = fileWriterFunc.BindAsExecutor("FileWriter");

        // Build the workflow with proper loop pattern
        // NOTE: The loop controller acts as a decision point - it receives ComponentEvaluationState
        // and the workflow evaluates conditions to decide whether to continue loop or exit
        var workflow = new WorkflowBuilder(requirementsExecutor)
            .AddEdge(requirementsExecutor, discoveryExecutor)
            .AddEdge(discoveryExecutor, loopInitExecutor)  // Initialize loop with discovery results
            .AddEdge(loopInitExecutor, loopControllerExecutor)  // Pass to loop controller
            
            // EXIT CONDITION FIRST: Check if loop should exit (no pending components)
            .AddEdge<ComponentEvaluationState>(
                loopControllerExecutor,
                aggregationExecutor,
                condition: state => 
                {
                    var shouldExit = state != null && !state.HasPendingComponents;
                    if (shouldExit)
                    {
                        Console.WriteLine("\n🎯 [Loop Exit] Condition met: No more pending components");
                    }
                    return shouldExit;
                })
            
            // LOOP PROCESSING: If not exiting, process current candidate
            .AddEdge<ComponentEvaluationState>(
                loopControllerExecutor,
                enrichmentExecutor,
                condition: state => state != null && state.HasPendingComponents)  // Only process if we have pending
            
            .AddEdge(enrichmentExecutor, evaluationExecutor)
            .AddEdge(evaluationExecutor, loopUpdateExecutor)
            .AddEdge(loopUpdateExecutor, loopControllerExecutor)  // Loop back to controller!
            
            .AddEdge(aggregationExecutor, planningExecutor)
            .AddEdge(planningExecutor, markdownExecutor)
            .AddEdge(markdownExecutor, fileWriterExecutor)
            .WithOutputFrom(fileWriterExecutor)
            .Build();

        var workshopRequest = """
            Create a 3-4 hour workshop on Microsoft Agents SDK for intermediate developers.
            
            Requirements:
            Structure it around existing Microsoft Learn training modules, Guided Projects (MS Learn Labs), 
            and relevant GitHub repositories from Microsoft. Ideally reuse their content for easy 
            preparation and delivery.

            Focus areas:
            - Microsoft Agents architecture
            - Model Context Protocol (MCP) integration
            - Multi-step agentic workflows
            - Structured outputs and tool calling
            
            Prerequisites: C#, .NET, basic AI/LLM concepts, async programming
            
            Include hands-on labs and practical examples that participants can follow along.

            Generate a comprehensive markdown workshop plan document with:
            - Workshop structure and modules
            - Learning objectives for each module
            - Suggested resources and content sources
            - Success criteria
            """;

        Console.WriteLine("📌 Workshop Request:");
        Console.WriteLine(workshopRequest);
        Console.WriteLine();

        try
        {
            await using StreamingRun run = await InProcessExecution.StreamAsync(workflow, workshopRequest);
            string? result = null;
            
            await foreach (WorkflowEvent evt in run.WatchStreamAsync().ConfigureAwait(false))
            {
                if (evt is WorkflowOutputEvent output)
                {
                    result = output.Data?.ToString();
                }
            }

            Console.WriteLine($"\n✅ Workshop plan generated successfully!");
            Console.WriteLine($"📁 Output file: {result}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Error: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"   Inner: {ex.InnerException.Message}");
            }
        }
    }
}
