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
/// This sample demonstrates a working pattern for multi-agent review workflow
/// using sequential execution (non-streaming) to avoid fragmentation issues.
/// 
/// Architecture:
/// 1. Build a review workflow with specialized review agents (fan-out/fan-in)
/// 2. Execute the review workflow independently (non-streaming)
/// 3. Use sequential cycles: Writer → Review → Writer → Review
/// 4. Collect complete outputs from each phase
/// 
/// This demonstrates:
/// - Non-streaming workflow execution
/// - Sequential review cycles
/// - Complete message collection without fragmentation
/// </summary>
/// <remarks>
/// Pre-requisites:
/// - An Azure OpenAI chat completion deployment must be configured.
/// - Demonstrates working alternative to Sample12C's broken pattern
/// </remarks>
internal static class Sample12D_CustomReviewWorkflow
{
    public static async Task Execute()
    {
        Console.WriteLine("=== Sample 12D: Sequential Review Workflow (Non-Streaming) ===");
        Console.WriteLine("Writer collaborates with review workflow in sequential cycles.\n");

        // Set up the Azure OpenAI client using AIConfig
        var azureClient = new AzureOpenAIClient(AIConfig.Endpoint, AIConfig.KeyCredential);
        var deploymentName = AIConfig.ModelDeployment;
        IChatClient chatClient = azureClient.GetChatClient(deploymentName).AsIChatClient();

        // ====================================
        // Step 1: Build multi-reviewer workflow
        // ====================================
        Console.WriteLine("Building multi-reviewer workflow...\n");

        var reviewWorkflow = await GetMultiReviewerWorkflowAsync(chatClient).ConfigureAwait(false);
        WorkflowVisualizerTool.PrintAll(reviewWorkflow, "Multi-Reviewer Workflow");

        // ====================================
        // Step 2: Create Writer agent
        // ====================================
        Console.WriteLine("\nCreating Writer agent...\n");

        ChatClientAgent writerAgent = new(
            chatClient,
            name: "Writer",
            instructions: @"You are a creative writer who crafts engaging blog posts.
When you receive feedback from reviewers, carefully incorporate their suggestions to improve your work.
Build upon the previous version while addressing all critiques."
        );

        Console.WriteLine("✅ Writer agent created.\n");

        // ====================================
        // Step 3: Execute sequential review cycles
        // ====================================
        Console.WriteLine("--- Starting Sequential Review Cycles ---\n");
        Console.WriteLine("Task: Write a blog post about AI and privacy.\n");
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine();

        var conversationHistory = new List<ChatMessage>
        {
            new(ChatRole.User, @"Write a 300-word blog post about artificial intelligence and privacy concerns. 
Make it informative, SEO-friendly, and ensure there are no PII examples. 
Include facts that can be verified and maintain a professional writing style.")
        };

        const int maxCycles = 2;

        for (int cycle = 1; cycle <= maxCycles; cycle++)
        {
            Console.WriteLine($"\n>>> CYCLE {cycle} - WRITER <<<");
            Console.WriteLine(new string('=', 80));
            Console.WriteLine();

            // Writer creates/revises content
            var writerResponse = await writerAgent.RunAsync(conversationHistory);
            Console.WriteLine($"[Writer]:\n{writerResponse.Text}");
            Console.WriteLine();

            conversationHistory.Add(new ChatMessage(ChatRole.Assistant, writerResponse.Text)
            {
                AuthorName = "Writer"
            });

            Console.WriteLine($"\n>>> CYCLE {cycle} - REVIEW WORKFLOW <<<");
            Console.WriteLine(new string('=', 80));
            Console.WriteLine();

            // Execute review workflow (non-streaming)
            var reviewInput = new List<ChatMessage>
            {
                new(ChatRole.User, $"Please review this content:\n\n{writerResponse.Text}")
            };

            await using Run reviewRun = await InProcessExecution.RunAsync(reviewWorkflow, reviewInput);
            
            // Collect all events from the review workflow
            string? editorOutput = null;
            
            foreach (WorkflowEvent evt in reviewRun.NewEvents)
            {
                if (evt is ExecutorCompletedEvent executorComplete)
                {
                    Console.WriteLine($"[{executorComplete.ExecutorId}] completed");
                    
                    // Capture messages from executor completion
                    if (executorComplete.Data is List<ChatMessage> messages && messages.Count > 0)
                    {
                        editorOutput = messages.Last().Text;
                    }
                }
                else if (evt is WorkflowOutputEvent outputEvent)
                {
                    Console.WriteLine($"[Workflow Output Event received]");
                    
                    // Extract editor feedback from workflow output
                    if (outputEvent.Data is List<ChatMessage> outputMessages && outputMessages.Count > 0)
                    {
                        editorOutput = outputMessages.Last().Text;
                    }
                }
            }

            if (!string.IsNullOrEmpty(editorOutput))
            {
                Console.WriteLine($"\n[Editor Synthesis]:\n{editorOutput}");
                Console.WriteLine();

                conversationHistory.Add(new ChatMessage(ChatRole.User, $"Feedback from review team:\n\n{editorOutput}")
                {
                    AuthorName = "ReviewTeam"
                });
            }
            else
            {
                Console.WriteLine("⚠️ Warning: No editor output captured from review workflow.");
            }

            Console.WriteLine(new string('=', 80));
        }

        Console.WriteLine();
        Console.WriteLine("✅ Sample 12D Complete!");
        Console.WriteLine();
        Console.WriteLine("Key Takeaways:");
        Console.WriteLine("- Non-streaming execution avoids fragmentation issues");
        Console.WriteLine("- Sequential cycles provide clear writer/review separation");
        Console.WriteLine("- Complete messages captured via NewEvents iteration");
        Console.WriteLine("- Editor synthesis is properly collected and displayed");
    }

    /// <summary>
    /// Builds a multi-reviewer workflow that fans out to specialist reviewers,
    /// aggregates their feedback, and synthesizes it through an editor.
    /// </summary>
    private static ValueTask<Workflow<List<ChatMessage>>> GetMultiReviewerWorkflowAsync(IChatClient chatClient)
    {
        // Create executors
        var startExecutor = new Sample12DReviewStartExecutor();
        var aggregationExecutor = new Sample12DReviewAggregationExecutor();

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

If Needs Improvement, provide up to 5 issues with actionable fixes.

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

If Needs Improvement, provide up to 5 issues with actionable fixes.

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

If Needs Improvement, provide up to 5 issues with actionable fixes.

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
// Review Workflow Executors (Sample12D-specific to avoid naming conflicts)
// ====================================

/// <summary>
/// Executor that starts the review workflow by broadcasting content to all reviewers.
/// </summary>
internal sealed class Sample12DReviewStartExecutor : ReflectingExecutor<Sample12DReviewStartExecutor>, IMessageHandler<List<ChatMessage>>
{
    public Sample12DReviewStartExecutor() : base("Sample12DReviewStartExecutor") { }

    public async ValueTask HandleAsync(List<ChatMessage> message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        Console.WriteLine("[ReviewStart] Distributing content to all reviewers...");

        // Broadcast the content to all connected reviewer agents
        await context.SendMessageAsync(message, cancellationToken: cancellationToken).ConfigureAwait(false);

        // Broadcast the turn token to kick off the reviewers
        await context.SendMessageAsync(new TurnToken(emitEvents: true), cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// Executor that aggregates feedback from all reviewers before passing to editor.
/// </summary>
internal sealed class Sample12DReviewAggregationExecutor : ReflectingExecutor<Sample12DReviewAggregationExecutor>, IMessageHandler<ChatMessage, List<ChatMessage>>
{
    public Sample12DReviewAggregationExecutor() : base("Sample12DReviewAggregationExecutor") { }

    private readonly List<ChatMessage> _reviews = new List<ChatMessage>();

    public ValueTask<List<ChatMessage>> HandleAsync(ChatMessage message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[ReviewAggregation] Received review from {message.AuthorName}");
        _reviews.Add(message);

        // When all 4 reviewers have responded, pass to editor
        if (_reviews.Count >= 4)
        {
            Console.WriteLine("[ReviewAggregation] All 4 reviews collected, sending to Editor for synthesis.");

            // Format all reviews for the editor
            var aggregatedReviews = new List<ChatMessage>
            {
                new(ChatRole.User, "Here are the reviews from all specialists:\n\n" +
                    string.Join("\n\n---\n\n", _reviews.Select(r => $"**{r.AuthorName}:**\n{r.Text}")))
            };

            return ValueTask.FromResult(aggregatedReviews);
        }

        // Not all reviews collected yet - return null to continue waiting
        return ValueTask.FromResult<List<ChatMessage>>(null!);
    }
}