// SPDX-License-Identifier: LicenseRef-MAFPlayground-NPU-1.0-CH
// Copyright (c) 2025 Jose Luis Latorre

using Azure.AI.OpenAI;
using MAFPlayground.Utils;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Reflection;
using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MAFPlayground.Samples;

/// <summary>
/// This sample demonstrates using the Workflow-as-Agent pattern from Sample10,
/// combined with the Round Robin group chat pattern from Sample12.
/// 
/// Architecture:
/// 1. Build a review workflow with specialized review agents (fan-out/fan-in)
/// 2. SEO Reviewer, PII Reviewer, Fact Checker, and Style Reviewer provide feedback
/// 3. Aggregation executor collects all reviews
/// 4. Editor agent synthesizes all feedback and provides final critique
/// 5. Convert the entire review workflow to an agent using .AsAgent()
/// 6. Use the workflow-agent in a Round Robin group chat with Writer
/// 
/// This demonstrates:
/// - Complex workflow composition with multiple reviewers
/// - Workflow-as-agent pattern for reusable review logic
/// - Nested agent collaboration (Writer + Review Workflow Agent)
/// </summary>
/// <remarks>
/// Pre-requisites:
/// - An Azure OpenAI chat completion deployment must be configured.
/// - Combines concepts from Sample10 and Sample12
/// </remarks>
internal static class Sample12C_WorkflowAsAgentReview
{
    public static async Task Execute()
    {
        Console.WriteLine("=== Sample 12C: Workflow as Agent - Multi-Reviewer Collaboration ===");
        Console.WriteLine("Writer collaborates with a multi-agent review workflow.\n");

        // Set up the Azure OpenAI client using AIConfig
        var azureClient = new AzureOpenAIClient(AIConfig.Endpoint, AIConfig.KeyCredential);
        var deploymentName = AIConfig.ModelDeployment;
        IChatClient chatClient = azureClient.GetChatClient(deploymentName).AsIChatClient();

        // ====================================
        // Step 1: Create Writer agent
        // ====================================
        Console.WriteLine("Creating Writer agent...\n");

        ChatClientAgent writerAgent = new(
            chatClient,
            name: "Writer",
            instructions: @"You are a creative writer who crafts engaging stories.
Focus on creating vivid descriptions, interesting characters, and compelling narratives.
When you receive feedback from reviewers, carefully incorporate their suggestions to improve your work.
Build upon the previous version of the story while addressing all critiques."
        );

        // ====================================
        // Step 2: Build multi-reviewer workflow
        // ====================================
        Console.WriteLine("Building multi-reviewer workflow...\n");

        var reviewWorkflow = await GetMultiReviewerWorkflowAsync(chatClient).ConfigureAwait(false);

        // Visualize the underlying workflow structure
        WorkflowVisualizerTool.PrintAll(reviewWorkflow, "Multi-Reviewer Workflow (before conversion to agent)");

        // ====================================
        // Step 3: Convert workflow to agent
        // ====================================
        Console.WriteLine("\nConverting review workflow to agent...\n");

        AIAgent reviewWorkflowAgent = reviewWorkflow.AsAgent(
            "multi-reviewer-agent",
            "Agent that provides comprehensive review feedback from SEO, PII, Fact-checking, and Style perspectives");

        Console.WriteLine("✅ Review workflow converted to agent.\n");

        // ====================================
        // Step 4: Build Round Robin group chat with Writer + Review Agent
        // ====================================
        Console.WriteLine("Building Round Robin group chat workflow...\n");

        var groupChatWorkflow = AgentWorkflowBuilder
            .CreateGroupChatBuilderWith(agents => new AgentWorkflowBuilder.RoundRobinGroupChatManager(agents)
            {
                MaximumIterationCount = 4  // Writer → Reviewers → Writer → Reviewers
            })
            .AddParticipants([writerAgent, reviewWorkflowAgent])
            .Build();

        WorkflowVisualizerTool.PrintAll(groupChatWorkflow, "Group Chat with Writer + Review Workflow Agent");

        // ====================================
        // Step 5: Execute the collaborative workflow
        // ====================================
        Console.WriteLine("\n--- Starting Collaborative Writing Session ---\n");
        Console.WriteLine("Task: Write a blog post about AI and privacy.\n");
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine();

        var initialMessage = new List<ChatMessage>
        {
            new(ChatRole.User, @"Write a 300-word blog post about artificial intelligence and privacy concerns. 
Make it informative, SEO-friendly, and ensure there are no PII examples. 
Include facts that can be verified and maintain a professional writing style.")
        };

        await using StreamingRun run = await InProcessExecution.StreamAsync(groupChatWorkflow, initialMessage);
        await run.TrySendMessageAsync(new TurnToken(emitEvents: true));

        string? currentAgent = null;
        int turnCount = 0;

        await foreach (WorkflowEvent evt in run.WatchStreamAsync().ConfigureAwait(false))
        {
            if (evt is AgentRunUpdateEvent agentUpdate)
            {
                // Detect agent change
                if (agentUpdate.Update.AuthorName != currentAgent)
                {
                    currentAgent = agentUpdate.Update.AuthorName;
                    turnCount++;

                    Console.WriteLine();
                    Console.WriteLine($">>> Turn {turnCount}: {currentAgent}");
                    Console.WriteLine(new string('-', 80));
                }

                // Stream agent response
                Console.Write(agentUpdate.Update.Text);
            }
            else if (evt is WorkflowOutputEvent output)
            {
                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine("=".PadRight(80, '='));
                Console.WriteLine("=== Collaborative Session Completed ===");
                Console.WriteLine($"Total turns: {turnCount}");
                Console.WriteLine("=".PadRight(80, '='));
            }
        }

        Console.WriteLine();
        Console.WriteLine("✅ Sample 12C Complete!");
        Console.WriteLine();
        Console.WriteLine("Key Takeaways:");
        Console.WriteLine("- Multi-reviewer workflow acts as a single agent");
        Console.WriteLine("- SEO, PII, Fact-checking, and Style reviews happen concurrently");
        Console.WriteLine("- Editor synthesizes all feedback into actionable critiques");
        Console.WriteLine("- Writer receives comprehensive, multi-perspective feedback");
        Console.WriteLine("- Workflow-as-agent enables reusable review logic");
    }

    /// <summary>
    /// Builds a multi-reviewer workflow that fans out to specialist reviewers,
    /// aggregates their feedback, and synthesizes it through an editor.
    /// </summary>
    private static ValueTask<Workflow<List<ChatMessage>>> GetMultiReviewerWorkflowAsync(IChatClient chatClient)
    {
        // Create executors
        var startExecutor = new ReviewStartExecutor();
        var aggregationExecutor = new ReviewAggregationExecutor();

        // Create specialized reviewer agents
        AIAgent seoReviewer = GetSeoReviewerAgent(chatClient);
        AIAgent piiReviewer = GetPiiReviewerAgent(chatClient);
        AIAgent factChecker = GetFactCheckerAgent(chatClient);
        AIAgent styleReviewer = GetStyleReviewerAgent(chatClient);

        // Create editor agent that synthesizes all feedback
        AIAgent editorAgent = GetEditorAgent(chatClient);

        // Build the workflow: fan-out to reviewers, aggregate, then editor synthesizes
        return new WorkflowBuilder(startExecutor)
            .AddFanOutEdge(startExecutor, targets: [seoReviewer, piiReviewer, factChecker, styleReviewer])
            .AddFanInEdge(aggregationExecutor, sources: [seoReviewer, piiReviewer, factChecker, styleReviewer])
            .AddEdge(aggregationExecutor, editorAgent)
            .WithOutputFrom(editorAgent)
            .BuildAsync<List<ChatMessage>>();
    }

    // ====================================
    // Reviewer Agent Definitions
    // ====================================

    private static AIAgent GetSeoReviewerAgent(IChatClient chatClient)
    {
        return new ChatClientAgent(
            chatClient,
            name: "SEO_Reviewer",
            instructions: @"You are an SEO specialist who reviews content for search engine optimization.

Analyze the content and provide feedback in this format:

**SEO Review Status:** [Approved / Needs Improvement]

If Needs Improvement, provide exactly 5 issues with actionable fixes:

**Issue 1:** [Description]
**Actionable:** [Specific fix]

**Issue 2:** [Description]
**Actionable:** [Specific fix]

...and so on.

Focus on: keywords, headings, meta-description readiness, readability, and internal linking opportunities."
        );
    }

    private static AIAgent GetPiiReviewerAgent(IChatClient chatClient)
    {
        return new ChatClientAgent(
            chatClient,
            name: "PII_Reviewer",
            instructions: @"You are a privacy specialist who reviews content for Personally Identifiable Information (PII).

Analyze the content and provide feedback in this format:

**PII Review Status:** [Approved / Needs Improvement]

If Needs Improvement, provide up to 5 issues with actionable fixes:

**Issue 1:** [Description of PII found]
**Actionable:** [How to remove or anonymize]

**Issue 2:** [Description of PII found]
**Actionable:** [How to remove or anonymize]

...and so on.

Check for: names, email addresses, phone numbers, addresses, social security numbers, or any identifiable personal data."
        );
    }

    private static AIAgent GetFactCheckerAgent(IChatClient chatClient)
    {
        return new ChatClientAgent(
            chatClient,
            name: "Fact_Checker",
            instructions: @"You are a fact-checker who verifies claims and statements in content.

Analyze the content and provide feedback in this format:

**Fact-Check Status:** [Approved / Needs Improvement]

If Needs Improvement, provide up to 5 issues with actionable fixes:

**Issue 1:** [Unverified or dubious claim]
**Actionable:** [Request source or suggest correction]

**Issue 2:** [Unverified or dubious claim]
**Actionable:** [Request source or suggest correction]

...and so on.

Focus on: verifiable statistics, accurate dates, correct attributions, industry standards, and logical consistency."
        );
    }

    private static AIAgent GetStyleReviewerAgent(IChatClient chatClient)
    {
        return new ChatClientAgent(
            chatClient,
            name: "Style_Reviewer",
            instructions: @"You are a style editor who reviews content for writing quality and consistency.

Analyze the content and provide feedback in this format:

**Style Review Status:** [Approved / Needs Improvement]

If Needs Improvement, provide up to 5 issues with actionable fixes:

**Issue 1:** [Style problem]
**Actionable:** [Specific improvement]

**Issue 2:** [Style problem]
**Actionable:** [Specific improvement]

...and so on.

Focus on: tone consistency, sentence variety, active voice, clarity, transitions, and engaging writing."
        );
    }

    private static AIAgent GetEditorAgent(IChatClient chatClient)
    {
        return new ChatClientAgent(
            chatClient,
            name: "Editor",
            instructions: @"You are a senior editor who synthesizes feedback from multiple reviewers.

You receive reviews from SEO, PII, Fact-Checking, and Style specialists.

Your job is to:
1. Summarize the key findings from all reviewers
2. Prioritize the most critical issues
3. Provide clear, actionable guidance to the writer
4. Be constructive and encouraging while being thorough

Format your response as:

**Editor's Synthesis:**

**Overall Assessment:** [Brief summary]

**Priority Issues:**
- [Top priority from all reviews]
- [Second priority]
- [Third priority]

**Detailed Feedback:**
[Organize feedback by category: SEO, Privacy, Facts, Style]

**Next Steps:**
[Clear guidance on what the writer should focus on first]"
        );
    }
}

// ====================================
// Review Workflow Executors
// ====================================

/// <summary>
/// Executor that starts the review workflow by broadcasting content to all reviewers.
/// </summary>
internal sealed class ReviewStartExecutor : ReflectingExecutor<ReviewStartExecutor>, IMessageHandler<List<ChatMessage>>
{
    public ReviewStartExecutor() : base("ReviewStartExecutor") { }

    public async ValueTask HandleAsync(List<ChatMessage> message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        Console.WriteLine("[ReviewStart] Distributing content to all reviewers...\n");

        // Broadcast the content to all connected reviewer agents
        await context.SendMessageAsync(message, cancellationToken: cancellationToken).ConfigureAwait(false);

        // Broadcast the turn token to kick off the reviewers
        await context.SendMessageAsync(new TurnToken(emitEvents: true), cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// Executor that aggregates feedback from all reviewers before passing to editor.
/// </summary>
internal sealed class ReviewAggregationExecutor : ReflectingExecutor<ReviewAggregationExecutor>, IMessageHandler<ChatMessage, List<ChatMessage>>
{
    public ReviewAggregationExecutor() : base("ReviewAggregationExecutor") { }

    private readonly List<ChatMessage> _reviews = new List<ChatMessage>();

    public ValueTask<List<ChatMessage>> HandleAsync(ChatMessage message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[ReviewAggregation] Received review from {message.AuthorName}\n");
        _reviews.Add(message);

        // When all 4 reviewers have responded, pass to editor
        if (_reviews.Count >= 4)
        {
            Console.WriteLine("[ReviewAggregation] All reviews collected, sending to Editor for synthesis.\n");

            // Format all reviews for the editor
            var aggregatedReviews = new List<ChatMessage>
            {
                new(ChatRole.User, "Here are the reviews from all specialists:\n\n" +
                    string.Join("\n\n---\n\n", _reviews.Select(r => $"**{r.AuthorName}:**\n{r.Text}")))
            };

            return ValueTask.FromResult(aggregatedReviews);
        }

        // Not all reviews collected yet
        return ValueTask.FromResult<List<ChatMessage>>(null!);
    }
}