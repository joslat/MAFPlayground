// SPDX-License-Identifier: LicenseRef-MAFPlayground-NPU-1.0-CH
// Copyright (c) 2025 Jose Luis Latorre

using MAFPlayground.Utils;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MAFPlayground.Samples;

/// <summary>
/// Sample 14: Software Development Pipeline - Code-Only Workflow
/// 
/// This sample demonstrates a complete software development workflow using ONLY code executors
/// (no AI agents). It showcases:
/// 
/// 1. Sequential Processing - Analysis and Specification phases
/// 2. Concurrent Processing (Fan-out/Fan-in) - Expert reviews happening in parallel
/// 3. Conditional Routing - Quality control with approval/rejection paths
/// 4. Loop-back mechanism - Rejected work returns to implementation
/// 5. Mix of function-based and class-based executors
/// 
/// Pipeline Flow:
/// ┌─────────────┐
/// │  Analysis   │ (Sequential: Requirements → Risk → Feasibility)
/// └──────┬──────┘
///        ↓
/// ┌─────────────┐
/// │Specification│ (Sequential: Tech Specs → API Design → Data Model)
/// └──────┬──────┘
///        ↓
/// ┌─────────────────────────────────┐
/// │   Expert Assessment (Parallel)  │
/// │  Security │ Performance │ UX    │ → Aggregator
/// └──────┬──────────────────────────┘
///        ↓
/// ┌─────────────┐
/// │Implementation│ (Sequential: Code → Unit Tests → Integration)
/// └──────┬──────┘
///        ↓
/// ┌─────────────┐
/// │Quality Control│ → APPROVED → [Deploy]
/// └─────────────┘ → REJECTED → [Back to Implementation]
/// </summary>
/// <remarks>
/// This sample demonstrates real-world development processes and shows when to use
/// function-based vs. class-based executors.
/// </remarks>
internal static class Sample14_SoftwareDevelopmentPipeline
{
    public static async Task Execute()
    {
        Console.WriteLine("\n=== Sample 14: Software Development Pipeline (Code-Only Workflow) ===\n");
        Console.WriteLine("Demonstrates: Sequential → Concurrent → Conditional → Loop-back patterns\n");

        // ====================================
        // Phase 1: ANALYSIS (Sequential)
        // ====================================
        Console.WriteLine("Building ANALYSIS phase executors...");

        // Using function-based executors for simple transformations
        Func<string, IWorkflowContext, CancellationToken, ValueTask<string>> gatherRequirementsFunc = 
            (string input, IWorkflowContext ctx, CancellationToken ct) =>
            {
                var result = $"{input}\n[✓ Requirements Gathered: Feature scope defined]";
                Console.WriteLine("  → Requirements gathering complete");
                return ValueTask.FromResult(result);
            };
        var gatherRequirements = gatherRequirementsFunc.BindAsExecutor("GatherRequirements");

        Func<string, IWorkflowContext, CancellationToken, ValueTask<string>> riskAnalysisFunc = 
            (string input, IWorkflowContext ctx, CancellationToken ct) =>
            {
                var result = $"{input}\n[✓ Risk Analysis: Low risk, no blockers identified]";
                Console.WriteLine("  → Risk analysis complete");
                return ValueTask.FromResult(result);
            };
        var riskAnalysis = riskAnalysisFunc.BindAsExecutor("RiskAnalysis");

        Func<string, IWorkflowContext, CancellationToken, ValueTask<string>> feasibilityCheckFunc = 
            (string input, IWorkflowContext ctx, CancellationToken ct) =>
            {
                var result = $"{input}\n[✓ Feasibility Check: Project is feasible with current resources]";
                Console.WriteLine("  → Feasibility check complete");
                return ValueTask.FromResult(result);
            };
        var feasibilityCheck = feasibilityCheckFunc.BindAsExecutor("FeasibilityCheck");

        // ====================================
        // Phase 2: SPECIFICATION (Sequential)
        // ====================================
        Console.WriteLine("Building SPECIFICATION phase executors...");

        Func<string, IWorkflowContext, CancellationToken, ValueTask<string>> technicalSpecsFunc = 
            (string input, IWorkflowContext ctx, CancellationToken ct) =>
            {
                var result = $"{input}\n[✓ Technical Specs: Architecture and design patterns documented]";
                Console.WriteLine("  → Technical specifications complete");
                return ValueTask.FromResult(result);
            };
        var technicalSpecs = technicalSpecsFunc.BindAsExecutor("TechnicalSpecs");

        Func<string, IWorkflowContext, CancellationToken, ValueTask<string>> apiDesignFunc = 
            (string input, IWorkflowContext ctx, CancellationToken ct) =>
            {
                var result = $"{input}\n[✓ API Design: RESTful endpoints defined with OpenAPI spec]";
                Console.WriteLine("  → API design complete");
                return ValueTask.FromResult(result);
            };
        var apiDesign = apiDesignFunc.BindAsExecutor("APIDesign");

        Func<string, IWorkflowContext, CancellationToken, ValueTask<string>> dataModelFunc = 
            (string input, IWorkflowContext ctx, CancellationToken ct) =>
            {
                var result = $"{input}\n[✓ Data Model: Database schema and relationships defined]";
                Console.WriteLine("  → Data model complete");
                return ValueTask.FromResult(result);
            };
        var dataModel = dataModelFunc.BindAsExecutor("DataModel");

        // ====================================
        // Phase 3: EXPERT ASSESSMENT (Concurrent - Fan-out/Fan-in)
        // ====================================
        Console.WriteLine("Building EXPERT ASSESSMENT phase executors (concurrent)...");

        var expertFanOutStart = new ExpertReviewFanOutExecutor();

        // Three expert reviewers working in parallel
        Func<string, IWorkflowContext, CancellationToken, ValueTask<string>> securityExpertFunc = 
            (string input, IWorkflowContext ctx, CancellationToken ct) =>
            {
                var result = "[Security Expert] ✓ PASS - No security vulnerabilities detected. Authentication and authorization properly implemented.";
                Console.WriteLine("  → Security review complete");
                return ValueTask.FromResult(result);
            };
        var securityExpert = securityExpertFunc.BindAsExecutor("SecurityExpert");

        Func<string, IWorkflowContext, CancellationToken, ValueTask<string>> performanceExpertFunc = 
            (string input, IWorkflowContext ctx, CancellationToken ct) =>
            {
                var result = "[Performance Expert] ✓ PASS - Efficient algorithms chosen. Expected response time < 200ms.";
                Console.WriteLine("  → Performance review complete");
                return ValueTask.FromResult(result);
            };
        var performanceExpert = performanceExpertFunc.BindAsExecutor("PerformanceExpert");

        Func<string, IWorkflowContext, CancellationToken, ValueTask<string>> uxExpertFunc = 
            (string input, IWorkflowContext ctx, CancellationToken ct) =>
            {
                var result = "[UX Expert] ✓ PASS - User flows are intuitive. Accessibility standards met.";
                Console.WriteLine("  → UX review complete");
                return ValueTask.FromResult(result);
            };
        var uxExpert = uxExpertFunc.BindAsExecutor("UXExpert");

        // Class-based aggregator (needs state to collect all expert reviews)
        var expertAggregator = new ExpertReviewAggregator(expectedCount: 3);

        // ====================================
        // Phase 4: IMPLEMENTATION (Sequential)
        // ====================================
        Console.WriteLine("Building IMPLEMENTATION phase executors...");

        Func<string, IWorkflowContext, CancellationToken, ValueTask<string>> codingFunc = 
            (string input, IWorkflowContext ctx, CancellationToken ct) =>
            {
                var result = $"{input}\n[✓ Coding: Implementation complete with clean code practices]";
                Console.WriteLine("  → Coding complete");
                return ValueTask.FromResult(result);
            };
        var coding = codingFunc.BindAsExecutor("Coding");

        Func<string, IWorkflowContext, CancellationToken, ValueTask<string>> unitTestsFunc = 
            (string input, IWorkflowContext ctx, CancellationToken ct) =>
            {
                var result = $"{input}\n[✓ Unit Tests: 95% code coverage achieved]";
                Console.WriteLine("  → Unit tests complete");
                return ValueTask.FromResult(result);
            };
        var unitTests = unitTestsFunc.BindAsExecutor("UnitTests");

        Func<string, IWorkflowContext, CancellationToken, ValueTask<string>> integrationTestsFunc = 
            (string input, IWorkflowContext ctx, CancellationToken ct) =>
            {
                var result = $"{input}\n[✓ Integration Tests: All endpoints tested successfully]";
                Console.WriteLine("  → Integration tests complete");
                return ValueTask.FromResult(result);
            };
        var integrationTests = integrationTestsFunc.BindAsExecutor("IntegrationTests");

        // ====================================
        // Phase 5: QUALITY CONTROL (Conditional with Loop-back)
        // ====================================
        Console.WriteLine("Building QUALITY CONTROL phase executors...");

        // Class-based QC executor (needs to return structured data for conditional routing)
        var qualityControl = new QualityControlExecutor();

        Func<QCResult, IWorkflowContext, CancellationToken, ValueTask<string>> deployFunc = 
            (QCResult input, IWorkflowContext ctx, CancellationToken ct) =>
            {
                var result = $"{input.Content}\n\n[🚀 DEPLOYED TO PRODUCTION]\n";
                Console.WriteLine("  → ✅ DEPLOYED TO PRODUCTION!");
                return ValueTask.FromResult(result);
            };
        var deployExecutor = deployFunc.BindAsExecutor("DeployToProduction");

        var rejectExecutor = new RejectionLoopBackExecutor();

        // ====================================
        // Build the Complete Workflow
        // ====================================
        Console.WriteLine("\nAssembling complete development pipeline workflow...\n");

        var workflow = new WorkflowBuilder(gatherRequirements)
            // Analysis Phase (Sequential)
            .AddEdge(gatherRequirements, riskAnalysis)
            .AddEdge(riskAnalysis, feasibilityCheck)
            // Specification Phase (Sequential)
            .AddEdge(feasibilityCheck, technicalSpecs)
            .AddEdge(technicalSpecs, apiDesign)
            .AddEdge(apiDesign, dataModel)
            // Expert Assessment (Concurrent - Fan-out/Fan-in)
            .AddEdge(dataModel, expertFanOutStart)
            .AddFanOutEdge(expertFanOutStart, targets: new[] { securityExpert, performanceExpert, uxExpert })
            .AddFanInEdge(expertAggregator, sources: new[] { securityExpert, performanceExpert, uxExpert })
            // Implementation Phase (Sequential)
            .AddEdge(expertAggregator, coding)
            .AddEdge(coding, unitTests)
            .AddEdge(unitTests, integrationTests)
            // Quality Control (Conditional)
            .AddEdge(integrationTests, qualityControl)
            .AddEdge<QCResult>(qualityControl, deployExecutor, condition: result => result is QCResult qc && qc.Approved)
            .AddEdge<QCResult>(qualityControl, rejectExecutor, condition: result => result is QCResult qc && !qc.Approved)
            // Loop-back from rejection to implementation
            .AddEdge(rejectExecutor, coding)
            // Output from deploy
            .WithOutputFrom(deployExecutor)
            .Build();

        // Visualize the workflow
        WorkflowVisualizerTool.PrintAll(workflow, "Sample 14: Software Development Pipeline");

        // ====================================
        // Execute the Workflow
        // ====================================
        Console.WriteLine("\n" + new string('=', 80));
        Console.WriteLine("EXECUTING SOFTWARE DEVELOPMENT PIPELINE");
        Console.WriteLine(new string('=', 80) + "\n");

        var projectInput = "Project: User Authentication System";
        Console.WriteLine($"Starting project: {projectInput}\n");

        await using StreamingRun run = await InProcessExecution.StreamAsync(workflow, projectInput);
        await foreach (WorkflowEvent evt in run.WatchStreamAsync().ConfigureAwait(false))
        {
            if (evt is WorkflowOutputEvent output)
            {
                Console.WriteLine("\n" + new string('=', 80));
                Console.WriteLine("PIPELINE COMPLETE - FINAL OUTPUT:");
                Console.WriteLine(new string('=', 80));
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(output.Data);
                Console.ResetColor();
                Console.WriteLine(new string('=', 80));
            }
        }

        Console.WriteLine("\n✅ Sample 14 Complete!\n");
        Console.WriteLine("Key Concepts Demonstrated:");
        Console.WriteLine("  ✓ Function-based executors for simple transformations");
        Console.WriteLine("  ✓ Class-based executors for state management and complex logic");
        Console.WriteLine("  ✓ Sequential processing (Analysis, Specification, Implementation)");
        Console.WriteLine("  ✓ Concurrent processing (Expert reviews in parallel)");
        Console.WriteLine("  ✓ Conditional routing (QC approval/rejection)");
        Console.WriteLine("  ✓ Loop-back mechanism (Rejected work returns to implementation)");
        Console.WriteLine("  ✓ Fan-out/Fan-in patterns for parallel work aggregation\n");
    }
}

// ====================================
// Class-Based Executors (Need State or Complex Logic)
// ====================================

/// <summary>
/// Executor that fans out work to multiple expert reviewers.
/// </summary>
internal sealed class ExpertReviewFanOutExecutor : ReflectingExecutor<ExpertReviewFanOutExecutor>, IMessageHandler<string>
{
    public ExpertReviewFanOutExecutor() : base("ExpertReviewFanOut") { }

    public async ValueTask HandleAsync(string message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        Console.WriteLine("\n--- EXPERT ASSESSMENT PHASE (Concurrent Reviews) ---");
        Console.WriteLine("Broadcasting specifications to all expert reviewers...\n");
        
        // Send the message to all connected expert executors
        await context.SendMessageAsync(message, cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// Aggregates feedback from multiple expert reviewers.
/// Uses class-based approach because it needs to maintain state (collected reviews).
/// </summary>
internal sealed class ExpertReviewAggregator : ReflectingExecutor<ExpertReviewAggregator>, IMessageHandler<string, string>
{
    private readonly List<string> _reviews = new();
    private readonly int _expectedCount;

    public ExpertReviewAggregator(int expectedCount) : base("ExpertReviewAggregator")
    {
        _expectedCount = expectedCount;
    }

    public ValueTask<string> HandleAsync(string message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        _reviews.Add(message);
        Console.WriteLine($"  [Aggregator] Collected review {_reviews.Count}/{_expectedCount}");

        // Wait for all expert reviews
        if (_reviews.Count >= _expectedCount)
        {
            Console.WriteLine("\n--- All Expert Reviews Collected ---\n");
            var aggregated = string.Join("\n", _reviews);
            var result = $"[Expert Assessment Complete]\n{aggregated}\n[All experts approved - proceeding to implementation]";
            
            // Reset for potential loop-back
            _reviews.Clear();
            
            return ValueTask.FromResult(result);
        }

        return ValueTask.FromResult<string>(null!);
    }
}

/// <summary>
/// Quality Control result structure for conditional routing.
/// </summary>
public sealed class QCResult
{
    public bool Approved { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// Quality Control executor that randomly approves or rejects work.
/// Uses class-based approach because it needs to return structured data for conditional routing.
/// </summary>
internal sealed class QualityControlExecutor : ReflectingExecutor<QualityControlExecutor>, IMessageHandler<string, QCResult>
{
    private static int _attemptCount = 0;

    public QualityControlExecutor() : base("QualityControl") { }

    public ValueTask<QCResult> HandleAsync(string message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        _attemptCount++;
        Console.WriteLine($"\n--- QUALITY CONTROL (Attempt #{_attemptCount}) ---");
        Console.WriteLine("Running comprehensive quality checks...\n");

        // First attempt always fails to demonstrate loop-back
        bool approved = _attemptCount > 1;

        var result = new QCResult
        {
            Approved = approved,
            Reason = approved 
                ? "All quality criteria met. Code is production-ready." 
                : "Code quality issues detected. Requires fixes in error handling and logging.",
            Content = message
        };

        if (approved)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"  ✅ APPROVED: {result.Reason}");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"  ⚠️  REJECTED: {result.Reason}");
            Console.WriteLine("  → Looping back to implementation phase for fixes...");
            Console.ResetColor();
        }

        return ValueTask.FromResult(result);
    }
}

/// <summary>
/// Handles rejection and loops back to implementation.
/// </summary>
internal sealed class RejectionLoopBackExecutor : ReflectingExecutor<RejectionLoopBackExecutor>, IMessageHandler<QCResult, string>
{
    public RejectionLoopBackExecutor() : base("RejectionLoopBack") { }

    public ValueTask<string> HandleAsync(QCResult result, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        Console.WriteLine("\n--- REWORK REQUIRED ---");
        Console.WriteLine("Returning to implementation phase with QC feedback...\n");
        
        var reworkContent = $"{result.Content}\n[⚠️  REWORK: {result.Reason}]";
        return ValueTask.FromResult(reworkContent);
    }
}