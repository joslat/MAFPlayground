// SPDX-License-Identifier: LicenseRef-MAFPlayground-NPU-1.0-CH
// Copyright (c) 2025 Jose Luis Latorre

using MAFPlayground.Utils;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MAFPlayground.Samples;

internal static class Sample21_FeatureComplianceReview
{
    public static async Task Execute()
    {
        Console.WriteLine("\n=== Sample 21: Feature Compliance Review (Executor-only) ===\n");
        Console.WriteLine("This sample now mirrors the full mermaid workflow loop: feature sampling, feature loop controller, per-node routing, fan-out/in reviewers, per-feature aggregation, features aggregation, and final portfolio reporting.\n");

        Func<string, IWorkflowContext, CancellationToken, ValueTask<string>> kickoff =
            (string input, IWorkflowContext context, CancellationToken cancellationToken) =>
            {
                Console.WriteLine("Starting the feature compliance review run (backlogs, time window, size 10, seed 420)...");
                return ValueTask.FromResult(input);
            };
        var kickoffExecutor = kickoff.BindAsExecutor("ReviewKickoff");

        var samplingExecutor = new SamplingExecutor();
        var featureLoopControllerExecutor = new FeatureLoopControllerExecutor();
        var graphBuilderExecutor = new GraphBuilderExecutor();
        var graphIteratorExecutor = new GraphIteratorExecutor();
        var nodeRouterExecutor = new NodeSliceRouterExecutor();

        var featureReviewerExecutor = new FeatureReviewerExecutor();
        var pbiReviewerExecutor = new PbiReviewerExecutor();
        var taskReviewerExecutor = new TaskReviewerExecutor();
        var testReviewerExecutor = new TestReviewerExecutor();
        var pullRequestReviewerExecutor = new PullRequestReviewerExecutor();
        var buildReviewerExecutor = new BuildReviewerExecutor();
        var releaseReviewerExecutor = new ReleaseReviewerExecutor();
        var approvalReviewerExecutor = new ApprovalReviewerExecutor();
        var incidentReviewerExecutor = new IncidentReviewerExecutor();
        var documentReviewerExecutor = new DocumentReviewerExecutor();
        const int reviewerCount = 10;
        var collectorExecutor = new NodeFindingsCollectorExecutor(reviewerCount);
        var featureAggregatorExecutor = new FeatureAggregatorExecutor();
        var featuresAggregatorExecutor = new FeaturesAggregatorExecutor();
        var portfolioReporterExecutor = new PortfolioReporterExecutor();

        Func<PortfolioReport, IWorkflowContext, CancellationToken, ValueTask<string>> reportingFunc =
            (PortfolioReport report, IWorkflowContext context, CancellationToken cancellationToken) =>
            {
                Console.WriteLine("\n--- Portfolio Report ---");
                Console.WriteLine(report.Narrative);
                Console.WriteLine($"Actions backlog size: {report.ActionBacklog.Length}");
                Console.WriteLine($"Sampling appendix: {report.SamplingSummary}");
                return ValueTask.FromResult("Feature Compliance review complete");
            };
        var reportingExecutor = reportingFunc.BindAsExecutor("FinalReporter");

        var workflow = new WorkflowBuilder(kickoffExecutor)
            .AddEdge(kickoffExecutor, samplingExecutor)
            .AddEdge(samplingExecutor, featureLoopControllerExecutor)
            .AddEdge(featureLoopControllerExecutor, graphBuilderExecutor)
            .AddEdge<GraphIterationCommand>(graphBuilderExecutor, graphIteratorExecutor)
            .AddEdge<GraphIteratorDecision>(graphIteratorExecutor, nodeRouterExecutor,
                condition: decision => decision is GraphIteratorDecision d && d.HasPendingNodes)
            .AddFanOutEdge(nodeRouterExecutor, targets: new ExecutorBinding[]
            {
                featureReviewerExecutor,
                pbiReviewerExecutor,
                taskReviewerExecutor,
                testReviewerExecutor,
                pullRequestReviewerExecutor,
                buildReviewerExecutor,
                releaseReviewerExecutor,
                approvalReviewerExecutor,
                incidentReviewerExecutor,
                documentReviewerExecutor
            })
            .AddFanInEdge(sources: new ExecutorBinding[]
            {
                featureReviewerExecutor,
                pbiReviewerExecutor,
                taskReviewerExecutor,
                testReviewerExecutor,
                pullRequestReviewerExecutor,
                buildReviewerExecutor,
                releaseReviewerExecutor,
                approvalReviewerExecutor,
                incidentReviewerExecutor,
                documentReviewerExecutor
            }, collectorExecutor)
            .AddEdge<GraphIterationCommand>(collectorExecutor, graphIteratorExecutor)
            .AddEdge<GraphIteratorDecision>(graphIteratorExecutor, featureAggregatorExecutor,
                condition: decision => decision is GraphIteratorDecision d && d.FeatureCompleted)
            .AddEdge(featureAggregatorExecutor, featuresAggregatorExecutor)
            .AddEdge<FeatureLoopSignal>(featuresAggregatorExecutor, featureLoopControllerExecutor,
                condition: signal => signal is FeatureLoopSignal loop && loop.FeaturesPending)
            .AddEdge<FeatureSubReportBundle>(featuresAggregatorExecutor, portfolioReporterExecutor,
                condition: bundle => bundle is FeatureSubReportBundle report && !report.FeaturesPending)
            .AddEdge(portfolioReporterExecutor, reportingExecutor)
            .WithOutputFrom(reportingExecutor)
            .Build();

        WorkflowVisualizerTool.PrintAll(workflow, "Sample 21: Feature Compliance Review");

        await using StreamingRun run = await InProcessExecution.StreamAsync(workflow, "StartReview");
        await foreach (WorkflowEvent evt in run.WatchStreamAsync().ConfigureAwait(false))
        {
            if (evt is WorkflowOutputEvent output)
            {
                Console.WriteLine("\n=== Final Result ===");
                Console.WriteLine(output.Data);
            }
        }
    }
}

internal sealed record SamplingMetadata(DateTime WindowStart, DateTime WindowEnd, int SampleSize, int Seed, string[] Strata, DateTime GeneratedAt)
{
    public string Summary => $"Window={WindowStart:yyyy-MM-dd}/{WindowEnd:yyyy-MM-dd}, Seed={Seed}, Strata=[{string.Join(", ", Strata)}]";
}

internal sealed record FeatureSampleSet(string[] FeatureIds, SamplingMetadata Metadata);
internal sealed record FeatureLoopSignal(FeatureSampleSet? Seed, bool FeaturesPending);
internal sealed record FeatureWorkItem(string FeatureId, int FeatureIndex, int TotalFeatures, SamplingMetadata Metadata);
internal sealed record FeatureGraphDispatch(FeatureWorkItem WorkItem, FeatureGraph Graph);
internal sealed record GraphIterationCommand(FeatureGraphDispatch? Dispatch, NodeFindingsBundle? CompletedFindings);
internal sealed record GraphIteratorDecision(NodeSliceBundle? NextSlice, FeatureFindingsBundle? CompletedFeature, bool HasPendingNodes, bool FeatureCompleted);
internal sealed record NodeSliceBundle(NodeSlice[] Slices, SamplingMetadata Metadata, FeatureWorkItem WorkItem);
internal sealed record NodeFindingsBundle(NodeFindings[] Findings, SamplingMetadata Metadata, FeatureWorkItem WorkItem);
internal sealed record FeatureFindingsBundle(FeatureWorkItem WorkItem, IReadOnlyDictionary<string, IReadOnlyList<NodeFindings>> FindingsByFeature, SamplingMetadata Metadata);
internal sealed record FeatureLoopCursor(int CurrentFeatureIndex, int TotalFeatures, string FeatureId);
internal sealed record FeatureSubReportBundle(FeatureSubReport[] Reports, SamplingMetadata Metadata, FeatureLoopCursor Cursor, bool FeaturesPending);
internal enum NodeType
{
    Feature,
    Pbi,
    Task,
    TestRun,
    PullRequest,
    Build,
    Release,
    Approval,
    Incident,
    Document
}

internal sealed record GraphNode(string Id, NodeType Type, string Title, string Description, IReadOnlyDictionary<string, string> Metadata);
internal sealed record GraphEdge(string FromId, string ToId, string Relationship);
internal sealed record FeatureGraph(string FeatureId, string FeatureTitle, GraphNode[] Nodes, GraphEdge[] Edges);

internal sealed record NodeSlice(string FeatureId, GraphNode Node, IReadOnlyList<GraphNode> Neighbors, IReadOnlyDictionary<string, string> PolicySlice);
internal sealed record NodeFindings(string FeatureId, string NodeId, NodeType NodeType, string Summary, bool HasDeviation, string[] EvidenceReferences, ProposedAction[] Actions);
internal sealed record ProposedAction(string Title, string OwnerHint, string DueDateHint, string Severity, string[] RelatedNodeIds);
internal sealed record FeatureSubReport(string FeatureId, string ChecklistSummary, int Deviations, double CycleTimeHours, ProposedAction[] Actions)
{
    public int ActionCount => Actions?.Length ?? 0;
}
internal sealed record PortfolioReport(string Narrative, string SamplingSummary, int FeaturesReviewed, int TotalDeviations, ProposedAction[] ActionBacklog);

internal sealed class GraphBuilderExecutor : ReflectingExecutor<GraphBuilderExecutor>, IMessageHandler<FeatureWorkItem, GraphIterationCommand>
{
    private readonly FeatureGraphBuilder _builder = new();

    public GraphBuilderExecutor() : base("GraphBuilder") { }

    public ValueTask<GraphIterationCommand> HandleAsync(FeatureWorkItem workItem, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        FeatureGraph graph = _builder.Build(workItem.FeatureId);
        Console.WriteLine($"GraphBuilder -> built dependency graph for feature {workItem.FeatureId}");
        var dispatch = new FeatureGraphDispatch(workItem, graph);
        return ValueTask.FromResult(new GraphIterationCommand(dispatch, null));
    }
}

internal sealed class GraphIteratorExecutor : ReflectingExecutor<GraphIteratorExecutor>, IMessageHandler<GraphIterationCommand, GraphIteratorDecision>
{
    private sealed class FeatureIterationState
    {
        public FeatureIterationState(FeatureWorkItem workItem, NodeSlice[] slices)
        {
            WorkItem = workItem;
            Slices = slices;
        }

        public FeatureWorkItem WorkItem { get; }
        public NodeSlice[] Slices { get; }
        public int NextIndex { get; set; }
        public List<NodeFindings> Accumulated { get; } = new();
    }

    private readonly GraphIterator _iterator = new();
    private readonly Dictionary<string, FeatureIterationState> _states = new();

    public GraphIteratorExecutor() : base("GraphIterator") { }

    public ValueTask<GraphIteratorDecision> HandleAsync(GraphIterationCommand command, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        if (command.Dispatch is not null)
        {
            FeatureWorkItem workItem = command.Dispatch.WorkItem;
            NodeSlice[] slices = _iterator.Iterate(command.Dispatch.Graph).ToArray();
            var state = new FeatureIterationState(workItem, slices);
            _states[workItem.FeatureId] = state;
            Console.WriteLine($"GraphIterator -> feature {workItem.FeatureId} initialized with {slices.Length} nodes");
            return ValueTask.FromResult(DispatchNext(state));
        }

        if (command.CompletedFindings is not null)
        {
            FeatureWorkItem workItem = command.CompletedFindings.WorkItem;
            if (!_states.TryGetValue(workItem.FeatureId, out FeatureIterationState? state))
            {
                Console.WriteLine($"GraphIterator -> received findings for unknown feature {workItem.FeatureId}, ignoring");
                return ValueTask.FromResult<GraphIteratorDecision>(null!);
            }

            if (command.CompletedFindings.Findings.Length > 0)
            {
                state.Accumulated.AddRange(command.CompletedFindings.Findings);
            }

            return ValueTask.FromResult(DispatchNext(state));
        }

        return ValueTask.FromResult<GraphIteratorDecision>(null!);
    }

    private GraphIteratorDecision DispatchNext(FeatureIterationState state)
    {
        if (state.NextIndex < state.Slices.Length)
        {
            NodeSlice slice = state.Slices[state.NextIndex];
            state.NextIndex++;
            var bundle = new NodeSliceBundle(new[] { slice }, state.WorkItem.Metadata, state.WorkItem);
            Console.WriteLine($"GraphIterator -> dispatching slice {state.NextIndex}/{state.Slices.Length} ({slice.Node.Type}) for {state.WorkItem.FeatureId}");
            return new GraphIteratorDecision(bundle, null, HasPendingNodes: true, FeatureCompleted: false);
        }

        _states.Remove(state.WorkItem.FeatureId);

        var grouped = state.Accumulated
            .GroupBy(result => result.FeatureId)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<NodeFindings>)group.ToList());

        if (!grouped.ContainsKey(state.WorkItem.FeatureId))
        {
            grouped[state.WorkItem.FeatureId] = Array.Empty<NodeFindings>();
        }

        Console.WriteLine($"GraphIterator -> completed feature {state.WorkItem.FeatureId}, forwarding findings to aggregator");
        var findingsBundle = new FeatureFindingsBundle(state.WorkItem, grouped, state.WorkItem.Metadata);
        return new GraphIteratorDecision(null, findingsBundle, HasPendingNodes: false, FeatureCompleted: true);
    }
}

internal sealed class NodeSliceRouterExecutor : ReflectingExecutor<NodeSliceRouterExecutor>, IMessageHandler<GraphIteratorDecision, NodeSliceBundle>
{
    public NodeSliceRouterExecutor() : base("NodeSliceRouter") { }

    public ValueTask<NodeSliceBundle> HandleAsync(GraphIteratorDecision decision, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        if (decision.NextSlice is null)
        {
            Console.WriteLine("Router -> received decision without slice payload, ignoring");
            return ValueTask.FromResult<NodeSliceBundle>(null!);
        }

        var slice = decision.NextSlice.Slices.FirstOrDefault();
        string label = slice is null ? "<empty>" : slice.Node.Type.ToString();
        Console.WriteLine($"Router -> broadcasting slice ({label}) for feature {decision.NextSlice.WorkItem.FeatureId} to all reviewers");
        return ValueTask.FromResult(decision.NextSlice);
    }
}

internal sealed class NodeFindingsCollectorExecutor : ReflectingExecutor<NodeFindingsCollectorExecutor>, IMessageHandler<NodeFindingsBundle, GraphIterationCommand>
{
    private readonly int _expectedBatches;
    private readonly List<NodeFindingsBundle> _pending = new();

    public NodeFindingsCollectorExecutor(int expectedBatches) : base("FindingsCollector")
        => _expectedBatches = expectedBatches;

    public ValueTask<GraphIterationCommand> HandleAsync(NodeFindingsBundle bundle, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        _pending.Add(bundle);
        Console.WriteLine($"Collector -> received batch {_pending.Count}/{_expectedBatches}");

        if (_pending.Count < _expectedBatches)
        {
            return ValueTask.FromResult<GraphIterationCommand>(null!);
        }

        var combinedFindings = _pending.SelectMany(batch => batch.Findings).ToArray();
        _pending.Clear();
        Console.WriteLine($"Collector -> consolidated node findings for feature {bundle.WorkItem.FeatureId}");
        var aggregate = new NodeFindingsBundle(combinedFindings, bundle.Metadata, bundle.WorkItem);
        return ValueTask.FromResult(new GraphIterationCommand(null, aggregate));
    }
}

internal abstract class NodeReviewerExecutorBase<TExecutor> : ReflectingExecutor<TExecutor>, IMessageHandler<NodeSliceBundle, NodeFindingsBundle>
    where TExecutor : NodeReviewerExecutorBase<TExecutor>
{
    private readonly NodeType _nodeType;
    private readonly string _label;

    protected NodeReviewerExecutorBase(string executorLabel, NodeType nodeType) : base(executorLabel)
    {
        _label = executorLabel;
        _nodeType = nodeType;
    }

    public ValueTask<NodeFindingsBundle> HandleAsync(NodeSliceBundle bundle, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        NodeFindings[] findings = bundle.Slices
            .Where(slice => slice.Node.Type == _nodeType)
            .Select(slice => Review(slice.FeatureId, slice))
            .ToArray();

        Console.WriteLine($"{_label} -> processed {findings.Length} slices for feature {bundle.WorkItem.FeatureId}");
        return ValueTask.FromResult(new NodeFindingsBundle(findings, bundle.Metadata, bundle.WorkItem));
    }

    protected abstract NodeFindings Review(string featureId, NodeSlice slice);

    protected NodeFindings CreateFindings(string featureId, NodeSlice slice, string summary, bool hasDeviation, string[] evidence, ProposedAction[] actions)
        => new(featureId, slice.Node.Id, slice.Node.Type, summary, hasDeviation, evidence, actions);
}

internal sealed class FeatureReviewerExecutor : NodeReviewerExecutorBase<FeatureReviewerExecutor>
{
    public FeatureReviewerExecutor() : base("Feature Reviewer Executor", NodeType.Feature) { }

    protected override NodeFindings Review(string featureId, NodeSlice slice)
    {
        var evidence = new[] { slice.Node.Metadata.GetValueOrDefault("Links", "Feature definition document missing") };
        var actions = new[]
        {
            new ProposedAction("Confirm emergency playbook", "Feature Owner", "In next sprint", "Medium", new[] { slice.Node.Id })
        };

        return CreateFindings(featureId, slice, "Feature-level policies satisfied", false, evidence, actions);
    }
}

internal sealed class PbiReviewerExecutor : NodeReviewerExecutorBase<PbiReviewerExecutor>
{
    public PbiReviewerExecutor() : base("PBI Reviewer Executor", NodeType.Pbi) { }

    protected override NodeFindings Review(string featureId, NodeSlice slice)
    {
        var hasDeviation = slice.Node.Metadata.GetValueOrDefault("PermissibleData", string.Empty) != "Compliant";
        var actions = new[]
        {
            new ProposedAction("Validate DoR/DoD evidence", "Product Owner", "Within 2 days", "Medium", new[] { slice.Node.Id })
        };
        var evidence = new[] { "Trace: DoR/DoD checklist" };

        return CreateFindings(featureId, slice, hasDeviation ? "PBI needs attention" : "PBI passes DoR/DoD", hasDeviation, evidence, actions);
    }
}

internal sealed class TaskReviewerExecutor : NodeReviewerExecutorBase<TaskReviewerExecutor>
{
    public TaskReviewerExecutor() : base("Task Reviewer Executor", NodeType.Task) { }

    protected override NodeFindings Review(string featureId, NodeSlice slice)
    {
        var evidence = new[] { "PR references", "Documentation updates" };
        var actions = new[]
        {
            new ProposedAction("Double-check merged PR docs", "Engineering Lead", "Before next release", "Low", new[] { slice.Node.Id })
        };

        return CreateFindings(featureId, slice, "Task review confirmed PR coverage", false, evidence, actions);
    }
}

internal sealed class TestReviewerExecutor : NodeReviewerExecutorBase<TestReviewerExecutor>
{
    public TestReviewerExecutor() : base("Test Reviewer Executor", NodeType.TestRun) { }

    protected override NodeFindings Review(string featureId, NodeSlice slice)
    {
        var coverage = slice.Node.Metadata.GetValueOrDefault("Coverage", "0%");
        var hasDeviation = coverage.StartsWith("0");
        var evidence = new[] { $"Coverage: {coverage}" };
        var actions = new[]
        {
            new ProposedAction("Assess regression coverage", "QA Lead", "Immediate", "High", new[] { slice.Node.Id })
        };

        return CreateFindings(featureId, slice, hasDeviation ? "Regression coverage low" : "Regression coverage OK", hasDeviation, evidence, actions);
    }
}

internal sealed class PullRequestReviewerExecutor : NodeReviewerExecutorBase<PullRequestReviewerExecutor>
{
    public PullRequestReviewerExecutor() : base("PR Reviewer Executor", NodeType.PullRequest) { }

    protected override NodeFindings Review(string featureId, NodeSlice slice)
    {
        var evidence = new[] { "CI checks summary", "Trace links" };
        var actions = new[]
        {
            new ProposedAction("Archive PR checklist", "Release Engineer", "ASAP", "Low", new[] { slice.Node.Id })
        };

        return CreateFindings(featureId, slice, "PR is merged with trace links", false, evidence, actions);
    }
}

internal sealed class BuildReviewerExecutor : NodeReviewerExecutorBase<BuildReviewerExecutor>
{
    public BuildReviewerExecutor() : base("Build Reviewer Executor", NodeType.Build) { }

    protected override NodeFindings Review(string featureId, NodeSlice slice)
    {
        var evidence = new[] { slice.Node.Metadata.GetValueOrDefault("Pipeline", "unknown pipeline") };
        var hasDeviation = slice.Node.Metadata.GetValueOrDefault("Status", "Failed") != "Succeeded";
        var actions = new[]
        {
            new ProposedAction("Confirm CI stability", "CI Owner", "Within 3 days", "Medium", new[] { slice.Node.Id })
        };

        return CreateFindings(featureId, slice, hasDeviation ? "Build pipeline failing" : "Build pipeline stable", hasDeviation, evidence, actions);
    }
}

internal sealed class ReleaseReviewerExecutor : NodeReviewerExecutorBase<ReleaseReviewerExecutor>
{
    public ReleaseReviewerExecutor() : base("Release Reviewer Executor", NodeType.Release) { }

    protected override NodeFindings Review(string featureId, NodeSlice slice)
    {
        var evidence = new[] { slice.Node.Metadata.GetValueOrDefault("Window", "Unknown window") };
        var actions = new[]
        {
            new ProposedAction("Document release window", "Release Manager", "For next release", "Low", new[] { slice.Node.Id })
        };

        return CreateFindings(featureId, slice, "Release approved with CAB", false, evidence, actions);
    }
}

internal sealed class ApprovalReviewerExecutor : NodeReviewerExecutorBase<ApprovalReviewerExecutor>
{
    public ApprovalReviewerExecutor() : base("Approval Reviewer Executor", NodeType.Approval) { }

    protected override NodeFindings Review(string featureId, NodeSlice slice)
    {
        var blackout = slice.Node.Metadata.GetValueOrDefault("Blackout", "Unknown");
        var hasDeviation = blackout != "None";
        var evidence = new[] { "CAB-42 logs" };
        var actions = new[]
        {
            new ProposedAction("Confirm blackout alignment", "Change Manager", "Next blackout window", "High", new[] { slice.Node.Id })
        };

        return CreateFindings(featureId, slice, hasDeviation ? "Approval requires blackout review" : "Approval aligns with blackout", hasDeviation, evidence, actions);
    }
}

internal sealed class IncidentReviewerExecutor : NodeReviewerExecutorBase<IncidentReviewerExecutor>
{
    public IncidentReviewerExecutor() : base("Incident Reviewer Executor", NodeType.Incident) { }

    protected override NodeFindings Review(string featureId, NodeSlice slice)
    {
        var severity = slice.Node.Metadata.GetValueOrDefault("Severity", "None");
        var hasDeviation = severity != "None";
        var evidence = new[] { "Incident timeline" };
        var actions = new[]
        {
            new ProposedAction("Log incident response", "SRE", "Weekly", "Medium", new[] { slice.Node.Id })
        };

        return CreateFindings(featureId, slice, hasDeviation ? "Incident follow-up required" : "No incidents", hasDeviation, evidence, actions);
    }
}

internal sealed class DocumentReviewerExecutor : NodeReviewerExecutorBase<DocumentReviewerExecutor>
{
    public DocumentReviewerExecutor() : base("Document Reviewer Executor", NodeType.Document) { }

    protected override NodeFindings Review(string featureId, NodeSlice slice)
    {
        var evidence = new[] { slice.Node.Metadata.GetValueOrDefault("Links", "http://docs") };
        var actions = new[]
        {
            new ProposedAction("Confirm runbook parity", "Doc Owner", "Before deployment", "Low", new[] { slice.Node.Id })
        };

        return CreateFindings(featureId, slice, "Documentation linked", false, evidence, actions);
    }
}

internal sealed class FeatureAggregatorExecutor : ReflectingExecutor<FeatureAggregatorExecutor>, IMessageHandler<GraphIteratorDecision, FeatureSubReportBundle>
{
    public FeatureAggregatorExecutor() : base("FeatureAggregator") { }

    public ValueTask<FeatureSubReportBundle> HandleAsync(GraphIteratorDecision decision, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        FeatureFindingsBundle bundle = decision.CompletedFeature ?? throw new InvalidOperationException("FeatureAggregator received decision without completed findings.");
        IReadOnlyList<NodeFindings> nodeFindings = bundle.FindingsByFeature.TryGetValue(bundle.WorkItem.FeatureId, out var findings)
            ? findings
            : Array.Empty<NodeFindings>();

        int deviations = nodeFindings.Count(f => f.HasDeviation);
        string summary = deviations == 0 ? "All checklist items satisfied" : $"{deviations} checklist deviations";
        double cycleTime = 24 + nodeFindings.Count * 3;
        ProposedAction[] actions = nodeFindings.SelectMany(f => f.Actions).ToArray();
        Console.WriteLine($"Per-feature aggregator -> {bundle.WorkItem.FeatureId}: {summary} (deviations {deviations}, actions {actions.Length})");

        var report = new FeatureSubReport(bundle.WorkItem.FeatureId, summary, deviations, cycleTime, actions);
        var cursor = new FeatureLoopCursor(bundle.WorkItem.FeatureIndex, bundle.WorkItem.TotalFeatures, bundle.WorkItem.FeatureId);
        bool featuresPending = bundle.WorkItem.FeatureIndex < bundle.WorkItem.TotalFeatures;
        return ValueTask.FromResult(new FeatureSubReportBundle(new[] { report }, bundle.Metadata, cursor, featuresPending));
    }
}

internal sealed class FeaturesAggregatorExecutor : ReflectingExecutor<FeaturesAggregatorExecutor>, IMessageHandler<FeatureSubReportBundle, object>
{
    private readonly List<FeatureSubReport> _portfolioReports = new();
    private SamplingMetadata? _metadata;

    public FeaturesAggregatorExecutor() : base("FeaturesAggregator") { }

    public ValueTask<object> HandleAsync(FeatureSubReportBundle bundle, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        _portfolioReports.AddRange(bundle.Reports);
        _metadata = bundle.Metadata;
        Console.WriteLine($"FeaturesAggregator -> recorded feature {bundle.Cursor.FeatureId} ({bundle.Cursor.CurrentFeatureIndex}/{bundle.Cursor.TotalFeatures})");

        if (bundle.Cursor.CurrentFeatureIndex < bundle.Cursor.TotalFeatures)
        {
            Console.WriteLine("FeaturesAggregator -> requesting next feature from loop controller");
            return ValueTask.FromResult<object>(new FeatureLoopSignal(null, FeaturesPending: true));
        }

        var consolidatedCursor = new FeatureLoopCursor(bundle.Cursor.TotalFeatures, bundle.Cursor.TotalFeatures, "ALL");
        var consolidatedBundle = new FeatureSubReportBundle(_portfolioReports.ToArray(), _metadata!, consolidatedCursor, FeaturesPending: false);
        _portfolioReports.Clear();
        _metadata = null;
        Console.WriteLine("FeaturesAggregator -> all features processed, sending portfolio bundle to reporter");
        return ValueTask.FromResult<object>(consolidatedBundle);
    }
}

internal sealed class PortfolioReporterExecutor : ReflectingExecutor<PortfolioReporterExecutor>, IMessageHandler<FeatureSubReportBundle, PortfolioReport>
{
    public PortfolioReporterExecutor() : base("PortfolioReporter") { }

    public ValueTask<PortfolioReport> HandleAsync(FeatureSubReportBundle bundle, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        PortfolioReport report = PortfolioReporter.Build(bundle);
        Console.WriteLine("PortfolioReporter -> final report assembled");
        return ValueTask.FromResult(report);
    }
}

internal sealed class SamplingExecutor : ReflectingExecutor<SamplingExecutor>, IMessageHandler<string, FeatureLoopSignal>
{
    public SamplingExecutor() : base("SamplingExecutor") { }

    public ValueTask<FeatureLoopSignal> HandleAsync(string message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        var strata = new[] { "Area", "Risk", "Emergency" };
        var metadata = new SamplingMetadata(
            WindowStart: DateTime.UtcNow.AddMonths(-3).Date,
            WindowEnd: DateTime.UtcNow.Date.AddDays(-1),
            SampleSize: 10,
            Seed: 420,
            Strata: strata,
            GeneratedAt: DateTime.UtcNow);

        var featureIds = Enumerable.Range(1, metadata.SampleSize)
            .Select(i => $"FEATURE-{i:000}")
            .ToArray();

        Console.WriteLine($"Sampling {metadata.SampleSize} features (seed {metadata.Seed})...");

        return ValueTask.FromResult(new FeatureLoopSignal(new FeatureSampleSet(featureIds, metadata), FeaturesPending: true));
    }
}

internal sealed class FeatureLoopControllerExecutor : ReflectingExecutor<FeatureLoopControllerExecutor>, IMessageHandler<FeatureLoopSignal, FeatureWorkItem>
{
    private readonly Queue<string> _pendingFeatures = new();
    private SamplingMetadata? _metadata;
    private int _totalFeatures;
    private int _currentIndex;

    public FeatureLoopControllerExecutor() : base("FeatureLoopController") { }

    public ValueTask<FeatureWorkItem> HandleAsync(FeatureLoopSignal signal, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        if (signal.Seed is null && !signal.FeaturesPending)
        {
            Console.WriteLine("FeatureLoopController -> received idle signal, ignoring");
            return ValueTask.FromResult<FeatureWorkItem>(null!);
        }

        if (signal.Seed is not null)
        {
            _pendingFeatures.Clear();
            foreach (string featureId in signal.Seed.FeatureIds)
            {
                _pendingFeatures.Enqueue(featureId);
            }

            _metadata = signal.Seed.Metadata;
            _totalFeatures = signal.Seed.FeatureIds.Length;
            _currentIndex = 0;
            Console.WriteLine($"FeatureLoopController -> seeded {_totalFeatures} features for review");
        }

        if (_metadata is null || _pendingFeatures.Count == 0)
        {
            Console.WriteLine("FeatureLoopController -> no more features to dispatch");
            return ValueTask.FromResult<FeatureWorkItem>(null!);
        }

        _currentIndex++;
        string nextFeatureId = _pendingFeatures.Dequeue();
        var workItem = new FeatureWorkItem(nextFeatureId, _currentIndex, _totalFeatures, _metadata);
        Console.WriteLine($"FeatureLoopController -> dispatching feature {_currentIndex}/{_totalFeatures}: {nextFeatureId}");
        return ValueTask.FromResult(workItem);
    }
}

internal sealed class FeatureGraphBuilder
{
    public FeatureGraph Build(string featureId)
    {
        var featureTitle = "Feature Review - " + featureId;
        var nodes = new List<GraphNode>
        {
            CreateNode(featureId, NodeType.Feature, featureTitle, "Feature compliance review target", new Dictionary<string, string>
            {
                ["State"] = "Ready",
                ["Area"] = "Core Platform",
                ["Owner"] = "Platform Team"
            }),
            CreateNode(featureId + "-PBI01", NodeType.Pbi, "PBI - Security Controls", "Description for DoR/DoD", new Dictionary<string, string>
            {
                ["State"] = "Closed",
                ["DoR"] = "Satisfied",
                ["DoD"] = "Satisfied"
            }),
            CreateNode(featureId + "-PBI02", NodeType.Pbi, "PBI - Emergency Handling", "Emergency protocols", new Dictionary<string, string>
            {
                ["State"] = "Closed",
                ["PermissibleData"] = "Compliant"
            }),
            CreateNode(featureId + "-Task01", NodeType.Task, "Task - Code Review", "Enforce PR workflow", new Dictionary<string, string>
            {
                ["PRs"] = "Merged",
                ["Docs"] = "Updated"
            }),
            CreateNode(featureId + "-Test01", NodeType.TestRun, "Test Run - Regression", "Tests covering back-office flows", new Dictionary<string, string>
            {
                ["Coverage"] = "95%",
                ["Outcomes"] = "Pass"
            }),
            CreateNode(featureId + "-PR01", NodeType.PullRequest, "PR - Feature Merge", "Merged into main", new Dictionary<string, string>
            {
                ["Checks"] = "Passed",
                ["TraceId"] = "TRACE-" + featureId
            }),
            CreateNode(featureId + "-Build01", NodeType.Build, "Build - CI", "CI build snapshot", new Dictionary<string, string>
            {
                ["Status"] = "Succeeded",
                ["Pipeline"] = "release-candidate"
            }),
            CreateNode(featureId + "-Release01", NodeType.Release, "Release - Production", "Deployed to prod", new Dictionary<string, string>
            {
                ["Window"] = "Night",
                ["Approval"] = "CAB-42"
            }),
            CreateNode(featureId + "-Approval01", NodeType.Approval, "CAB Approval", "CAB sign-off", new Dictionary<string, string>
            {
                ["CAB"] = "42",
                ["Blackout"] = "None"
            }),
            CreateNode(featureId + "-Incident01", NodeType.Incident, "Incident - None", "No post-release incidents", new Dictionary<string, string>
            {
                ["Severity"] = "None",
                ["Resolved"] = "N/A"
            }),
            CreateNode(featureId + "-Doc01", NodeType.Document, "Runbook", "Updated runbook", new Dictionary<string, string>
            {
                ["Links"] = "https://dev.azure.com/docs/" + featureId,
                ["Version"] = "v1.2"
            })
        };

        var edges = new List<GraphEdge>
        {
            new(featureId, featureId + "-PBI01", "Contains"),
            new(featureId, featureId + "-PBI02", "Contains"),
            new(featureId + "-PBI01", featureId + "-Task01", "Implements"),
            new(featureId + "-PBI02", featureId + "-Task01", "Implements"),
            new(featureId + "-Task01", featureId + "-Test01", "ValidatedBy"),
            new(featureId + "-Task01", featureId + "-PR01", "ProposedBy"),
            new(featureId + "-PR01", featureId + "-Build01", "Builds"),
            new(featureId + "-Build01", featureId + "-Release01", "ReleasedAs"),
            new(featureId + "-Release01", featureId + "-Approval01", "Approvals"),
            new(featureId + "-Release01", featureId + "-Incident01", "MonitoredBy"),
            new(featureId + "-Doc01", featureId + "-Task01", "ReferencedBy")
        };

        return new FeatureGraph(featureId, featureTitle, nodes.ToArray(), edges.ToArray());
    }

    private static GraphNode CreateNode(string id, NodeType type, string title, string description, Dictionary<string, string> metadata)
    {
        metadata["Description"] = description;
        return new GraphNode(id, type, title, description, metadata);
    }
}

internal sealed class GraphIterator
{
    private static readonly NodeType[] _orderedTypes =
    {
        NodeType.Feature,
        NodeType.Pbi,
        NodeType.Task,
        NodeType.PullRequest,
        NodeType.TestRun,
        NodeType.Build,
        NodeType.Release,
        NodeType.Approval,
        NodeType.Incident,
        NodeType.Document
    };

    public IEnumerable<NodeSlice> Iterate(FeatureGraph graph)
    {
        foreach (NodeType type in _orderedTypes)
        {
            foreach (GraphNode node in graph.Nodes.Where(n => n.Type == type))
            {
                var neighbors = graph.Edges
                    .Where(edge => edge.FromId == node.Id)
                    .Select(edge => graph.Nodes.Single(n => n.Id == edge.ToId))
                    .ToList();

                var policySlice = new Dictionary<string, string>(node.Metadata);
                policySlice["Neighbors"] = string.Join(",", neighbors.Select(n => n.Id));

                yield return new NodeSlice(graph.FeatureId, node, neighbors, policySlice);
            }
        }
    }
}

internal static class PortfolioReporter
{
    public static PortfolioReport Build(FeatureSubReportBundle bundle)
    {
        FeatureSubReport[] reportList = bundle.Reports;
        int totalDeviations = reportList.Sum(r => r.Deviations);
        int featuresReviewed = reportList.Length;
        var allActions = reportList.SelectMany(r => r.Actions).ToArray();
        var uniqueActions = allActions
            .GroupBy(action => action.Title)
            .Select(group => group.First())
            .ToArray();

        string narrative = new StringBuilder()
            .AppendLine("Feature compliance review complete")
            .AppendLine($"Features reviewed: {featuresReviewed}")
            .AppendLine($"Deviations observed: {totalDeviations}")
            .AppendLine($"Actions deduplicated: {uniqueActions.Length}")
            .ToString();

        Console.WriteLine("PortfolioReporter -> received consolidated bundle, compiling final portfolio narrative");
        return new PortfolioReport(narrative, bundle.Metadata.Summary, featuresReviewed, totalDeviations, uniqueActions);
    }
}
