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
/// Sample 15: Software Development Pipeline with Hierarchical Sub-Workflows
/// 
/// This sample demonstrates the same software development workflow as Sample 14, but organized
/// using hierarchical sub-workflows for better modularity and reusability. Each phase
/// (Analysis, Specification, Implementation) is encapsulated as a separate sub-workflow.
/// 
/// Key improvements over Sample 14:
/// 1. Modular Design - Each phase is a reusable sub-workflow
/// 2. Clear Separation of Concerns - Each sub-workflow has a single responsibility
/// 3. Easier Maintenance - Changes to a phase only affect that sub-workflow
/// 4. Better Visualization - Sub-workflows can be visualized independently
/// 5. Reusability - Sub-workflows can be used in other pipelines
/// 
/// Pipeline Flow with Sub-Workflows:
/// ┌─────────────────────┐
/// │ Analysis Sub-WF     │ → Requirements → Risk → Feasibility
/// └──────────┬──────────┘
///            ↓
/// ┌─────────────────────┐
/// │ Specification Sub-WF│ → Tech Specs → API Design → Data Model
/// └──────────┬──────────┘
///            ↓
/// ┌─────────────────────────────────┐
/// │ Expert Assessment (Parallel)    │
/// │ Security │ Performance │ UX     │ → Aggregator
/// └──────────┬──────────────────────┘
///            ↓
/// ┌─────────────────────┐
/// │ Implementation Sub-WF│ → Code → Unit Tests → Integration
/// └──────────┬──────────┘
///            ↓
/// ┌─────────────┐
/// │Quality Control│ → APPROVED → [Deploy]
/// └─────────────┘ → REJECTED → [Back to Implementation Sub-WF]
/// </summary>
/// <remarks>
/// This sample demonstrates workflow composition and hierarchical design patterns.
/// Compare with Sample 14 to see the benefits of sub-workflow organization.
/// </remarks>
internal static class Sample15_SoftwareDevelopmentPipelineWithSubWorkflows
{
    public static async Task Execute()
    {
        Console.WriteLine("\n=== Sample 15: Software Development Pipeline with Hierarchical Sub-Workflows ===\n");
        Console.WriteLine("Demonstrates: Modular sub-workflows for each development phase\n");

        // ====================================
        // Build Sub-Workflows for Each Phase
        // ====================================
        var analysisSubWorkflow = BuildAnalysisSubWorkflow();
        var specificationSubWorkflow = BuildSpecificationSubWorkflow();
        var expertAssessmentSubWorkflow = BuildExpertAssessmentSubWorkflow();
        var implementationSubWorkflow = BuildImplementationSubWorkflow();
        var qualityControlSubWorkflow = BuildQualityControlSubWorkflow();
        var deploymentSubWorkflow = BuildDeploymentSubWorkflow();

        // ====================================
        // Build the Main Workflow (Orchestrates Sub-Workflows)
        // ====================================
        Console.WriteLine("\nAssembling main workflow with sub-workflows...\n");

        var mainWorkflow = new WorkflowBuilder(analysisSubWorkflow)
            .AddEdge(analysisSubWorkflow, specificationSubWorkflow)
            .AddEdge(specificationSubWorkflow, expertAssessmentSubWorkflow)
            .AddEdge(expertAssessmentSubWorkflow, implementationSubWorkflow)
            .AddEdge(implementationSubWorkflow, qualityControlSubWorkflow)
            // Conditional routing based on QC result
            .AddEdge<QCResult>(qualityControlSubWorkflow, deploymentSubWorkflow, 
                condition: result => result is QCResult qc && qc.Approved)
            .AddEdge<QCResult>(qualityControlSubWorkflow, implementationSubWorkflow, 
                condition: result => result is QCResult qc && !qc.Approved)
            // Output from deployment only
            .WithOutputFrom(deploymentSubWorkflow)
            .Build();

        // Visualize the main workflow
        WorkflowVisualizerTool.PrintAll(mainWorkflow, "Sample 15: Main Pipeline (with Sub-Workflows)");

        // ====================================
        // Execute the Workflow
        // ====================================
        Console.WriteLine("\n" + new string('=', 80));
        Console.WriteLine("EXECUTING SOFTWARE DEVELOPMENT PIPELINE (WITH SUB-WORKFLOWS)");
        Console.WriteLine(new string('=', 80) + "\n");

        var projectInput = "Project: User Authentication System";
        Console.WriteLine($"Starting project: {projectInput}\n");

        await using StreamingRun run = await InProcessExecution.StreamAsync(mainWorkflow, projectInput);
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

        Console.WriteLine("\n✅ Sample 15 Complete!\n");
        Console.WriteLine("Key Concepts Demonstrated:");
        Console.WriteLine("  ✓ Hierarchical workflow composition with sub-workflows");
        Console.WriteLine("  ✓ Modular phase organization (Analysis, Specification, Implementation, QC, Deployment)");
        Console.WriteLine("  ✓ Sub-workflow reusability and independent visualization");
        Console.WriteLine("  ✓ Concurrent processing within sub-workflows (Expert reviews)");
        Console.WriteLine("  ✓ Conditional routing based on QCResult at main workflow level");
        Console.WriteLine("  ✓ Loop-back to sub-workflows on rejection");
        Console.WriteLine("  ✓ Clean separation of concerns between phases");
        Console.WriteLine("  ✓ Function-based sub-workflow builders for readability\n");
        Console.WriteLine("Compare with Sample 14 to see the benefits of sub-workflow organization!\n");
    }

    // ====================================
    // Sub-Workflow Builder Functions
    // ====================================

    /// <summary>
    /// Builds the Analysis Phase sub-workflow: Requirements → Risk → Feasibility
    /// </summary>
    private static ExecutorIsh BuildAnalysisSubWorkflow()
    {
        Console.WriteLine("Building ANALYSIS sub-workflow...");

        Func<string, IWorkflowContext, CancellationToken, ValueTask<string>> gatherRequirementsFunc = 
            (string input, IWorkflowContext ctx, CancellationToken ct) =>
            {
                var result = $"{input}\n[✓ Requirements Gathered: Feature scope defined]";
                Console.WriteLine("  → Requirements gathering complete");
                return ValueTask.FromResult(result);
            };
        var gatherRequirements = gatherRequirementsFunc.AsExecutor("GatherRequirements");

        Func<string, IWorkflowContext, CancellationToken, ValueTask<string>> riskAnalysisFunc = 
            (string input, IWorkflowContext ctx, CancellationToken ct) =>
            {
                var result = $"{input}\n[✓ Risk Analysis: Low risk, no blockers identified]";
                Console.WriteLine("  → Risk analysis complete");
                return ValueTask.FromResult(result);
            };
        var riskAnalysis = riskAnalysisFunc.AsExecutor("RiskAnalysis");

        Func<string, IWorkflowContext, CancellationToken, ValueTask<string>> feasibilityCheckFunc = 
            (string input, IWorkflowContext ctx, CancellationToken ct) =>
            {
                var result = $"{input}\n[✓ Feasibility Check: Project is feasible with current resources]";
                Console.WriteLine("  → Feasibility check complete");
                return ValueTask.FromResult(result);
            };
        var feasibilityCheck = feasibilityCheckFunc.AsExecutor("FeasibilityCheck");

        var workflow = new WorkflowBuilder(gatherRequirements)
            .AddEdge(gatherRequirements, riskAnalysis)
            .AddEdge(riskAnalysis, feasibilityCheck)
            .WithOutputFrom(feasibilityCheck)
            .Build();

        WorkflowVisualizerTool.PrintAll(workflow, "Sub-Workflow 1: Analysis Phase");

        return workflow.ConfigureSubWorkflow("AnalysisPhase");
    }

    /// <summary>
    /// Builds the Specification Phase sub-workflow: Tech Specs → API Design → Data Model
    /// </summary>
    private static ExecutorIsh BuildSpecificationSubWorkflow()
    {
        Console.WriteLine("\nBuilding SPECIFICATION sub-workflow...");

        Func<string, IWorkflowContext, CancellationToken, ValueTask<string>> technicalSpecsFunc = 
            (string input, IWorkflowContext ctx, CancellationToken ct) =>
            {
                var result = $"{input}\n[✓ Technical Specs: Architecture and design patterns documented]";
                Console.WriteLine("  → Technical specifications complete");
                return ValueTask.FromResult(result);
            };
        var technicalSpecs = technicalSpecsFunc.AsExecutor("TechnicalSpecs");

        Func<string, IWorkflowContext, CancellationToken, ValueTask<string>> apiDesignFunc = 
            (string input, IWorkflowContext ctx, CancellationToken ct) =>
            {
                var result = $"{input}\n[✓ API Design: RESTful endpoints defined with OpenAPI spec]";
                Console.WriteLine("  → API design complete");
                return ValueTask.FromResult(result);
            };
        var apiDesign = apiDesignFunc.AsExecutor("APIDesign");

        Func<string, IWorkflowContext, CancellationToken, ValueTask<string>> dataModelFunc = 
            (string input, IWorkflowContext ctx, CancellationToken ct) =>
            {
                var result = $"{input}\n[✓ Data Model: Database schema and relationships defined]";
                Console.WriteLine("  → Data model complete");
                return ValueTask.FromResult(result);
            };
        var dataModel = dataModelFunc.AsExecutor("DataModel");

        var workflow = new WorkflowBuilder(technicalSpecs)
            .AddEdge(technicalSpecs, apiDesign)
            .AddEdge(apiDesign, dataModel)
            .WithOutputFrom(dataModel)
            .Build();

        WorkflowVisualizerTool.PrintAll(workflow, "Sub-Workflow 2: Specification Phase");

        return workflow.ConfigureSubWorkflow("SpecificationPhase");
    }

    /// <summary>
    /// Builds the Expert Assessment Phase sub-workflow: Fan-out → Security/Performance/UX → Aggregator
    /// </summary>
    private static ExecutorIsh BuildExpertAssessmentSubWorkflow()
    {
        Console.WriteLine("\nBuilding EXPERT ASSESSMENT sub-workflow (concurrent)...");

        var expertFanOutStart = new ExpertReviewFanOutExecutor();

        Func<string, IWorkflowContext, CancellationToken, ValueTask<string>> securityExpertFunc = 
            (string input, IWorkflowContext ctx, CancellationToken ct) =>
            {
                var result = "[Security Expert] ✓ PASS - No security vulnerabilities detected. Authentication and authorization properly implemented.";
                Console.WriteLine("  → Security review complete");
                return ValueTask.FromResult(result);
            };
        var securityExpert = securityExpertFunc.AsExecutor("SecurityExpert");

        Func<string, IWorkflowContext, CancellationToken, ValueTask<string>> performanceExpertFunc = 
            (string input, IWorkflowContext ctx, CancellationToken ct) =>
            {
                var result = "[Performance Expert] ✓ PASS - Efficient algorithms chosen. Expected response time < 200ms.";
                Console.WriteLine("  → Performance review complete");
                return ValueTask.FromResult(result);
            };
        var performanceExpert = performanceExpertFunc.AsExecutor("PerformanceExpert");

        Func<string, IWorkflowContext, CancellationToken, ValueTask<string>> uxExpertFunc = 
            (string input, IWorkflowContext ctx, CancellationToken ct) =>
            {
                var result = "[UX Expert] ✓ PASS - User flows are intuitive. Accessibility standards met.";
                Console.WriteLine("  → UX review complete");
                return ValueTask.FromResult(result);
            };
        var uxExpert = uxExpertFunc.AsExecutor("UXExpert");

        var expertAggregator = new ExpertReviewAggregator(expectedCount: 3);

        var workflow = new WorkflowBuilder(expertFanOutStart)
            .AddFanOutEdge(expertFanOutStart, targets: new[] { securityExpert, performanceExpert, uxExpert })
            .AddFanInEdge(expertAggregator, sources: new[] { securityExpert, performanceExpert, uxExpert })
            .WithOutputFrom(expertAggregator)
            .Build();

        WorkflowVisualizerTool.PrintAll(workflow, "Sub-Workflow 3: Expert Assessment Phase");

        return workflow.ConfigureSubWorkflow("ExpertAssessmentPhase");
    }

    /// <summary>
    /// Builds the Implementation Phase sub-workflow: Coding → Unit Tests → Integration Tests
    /// </summary>
    private static ExecutorIsh BuildImplementationSubWorkflow()
    {
        Console.WriteLine("\nBuilding IMPLEMENTATION sub-workflow...");

        Func<string, IWorkflowContext, CancellationToken, ValueTask<string>> codingFunc = 
            (string input, IWorkflowContext ctx, CancellationToken ct) =>
            {
                var result = $"{input}\n[✓ Coding: Implementation complete with clean code practices]";
                Console.WriteLine("  → Coding complete");
                return ValueTask.FromResult(result);
            };
        var coding = codingFunc.AsExecutor("Coding");

        Func<string, IWorkflowContext, CancellationToken, ValueTask<string>> unitTestsFunc = 
            (string input, IWorkflowContext ctx, CancellationToken ct) =>
            {
                var result = $"{input}\n[✓ Unit Tests: 95% code coverage achieved]";
                Console.WriteLine("  → Unit tests complete");
                return ValueTask.FromResult(result);
            };
        var unitTests = unitTestsFunc.AsExecutor("UnitTests");

        Func<string, IWorkflowContext, CancellationToken, ValueTask<string>> integrationTestsFunc = 
            (string input, IWorkflowContext ctx, CancellationToken ct) =>
            {
                var result = $"{input}\n[✓ Integration Tests: All endpoints tested successfully]";
                Console.WriteLine("  → Integration tests complete");
                return ValueTask.FromResult(result);
            };
        var integrationTests = integrationTestsFunc.AsExecutor("IntegrationTests");

        var workflow = new WorkflowBuilder(coding)
            .AddEdge(coding, unitTests)
            .AddEdge(unitTests, integrationTests)
            .WithOutputFrom(integrationTests)
            .Build();

        WorkflowVisualizerTool.PrintAll(workflow, "Sub-Workflow 4: Implementation Phase");

        return workflow.ConfigureSubWorkflow("ImplementationPhase");
    }

    /// <summary>
    /// Builds the Quality Control Phase sub-workflow: Just QC evaluation
    /// </summary>
    private static ExecutorIsh BuildQualityControlSubWorkflow()
    {
        Console.WriteLine("\nBuilding QUALITY CONTROL sub-workflow...");

        var qualityControl = new QualityControlExecutor();

        var workflow = new WorkflowBuilder(qualityControl)
            .WithOutputFrom(qualityControl)
            .Build();

        WorkflowVisualizerTool.PrintAll(workflow, "Sub-Workflow 5: Quality Control Phase");

        return workflow.ConfigureSubWorkflow("QualityControlPhase");
    }

    /// <summary>
    /// Builds the Deployment Phase sub-workflow: Deploy to production
    /// </summary>
    private static ExecutorIsh BuildDeploymentSubWorkflow()
    {
        Console.WriteLine("\nBuilding DEPLOYMENT sub-workflow...");

        Func<QCResult, IWorkflowContext, CancellationToken, ValueTask<string>> deployFunc = 
            (QCResult input, IWorkflowContext ctx, CancellationToken ct) =>
            {
                var result = $"{input.Content}\n\n[🚀 DEPLOYED TO PRODUCTION]\n";
                Console.WriteLine("  → ✅ DEPLOYED TO PRODUCTION!");
                return ValueTask.FromResult(result);
            };
        var deployExecutor = deployFunc.AsExecutor("DeployToProduction");

        var workflow = new WorkflowBuilder(deployExecutor)
            .WithOutputFrom(deployExecutor)
            .Build();

        WorkflowVisualizerTool.PrintAll(workflow, "Sub-Workflow 6: Deployment Phase");

        return workflow.ConfigureSubWorkflow("DeploymentPhase");
    }
}

// Note: The class-based executors (ExpertReviewFanOutExecutor, ExpertReviewAggregator, 
// QualityControlExecutor, RejectionLoopBackExecutor, QCResult) are reused from Sample 14