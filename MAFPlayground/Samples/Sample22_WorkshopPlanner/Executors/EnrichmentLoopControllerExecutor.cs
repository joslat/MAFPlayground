// SPDX-License-Identifier: LicenseRef-MAFPlayground-NPU-1.0-CH
// Copyright (c) 2025 Jose Luis Latorre

using Microsoft.Agents.AI.Workflows;

namespace MAFPlayground.Samples.Sample22_WorkshopPlanner.Executors;

/// <summary>
/// Stateful executor that manages the enrichment and evaluation loop.
/// Similar to Sample21's FeatureLoopController pattern.
/// </summary>
public static class EnrichmentLoopControllerExecutor
{
    public static ValueTask<ComponentEvaluationState> ExecuteAsync(
        DiscoveryResult discoveryResult,
        IWorkflowContext ctx,
        CancellationToken ct)
    {
        Console.WriteLine("\n🔄 [EnrichmentLoopController] Initializing evaluation queue...");
        
        if (discoveryResult.Candidates.Count == 0)
        {
            Console.WriteLine("   ⚠️  No candidates discovered - skipping loop");
            return ValueTask.FromResult(new ComponentEvaluationState(
                PendingCandidates: [],
                CurrentCandidate: null,
                EvaluationResults: [],
                HasPendingComponents: false
            ));
        }
        
        var state = new ComponentEvaluationState(
            PendingCandidates: new List<TrainingCandidate>(discoveryResult.Candidates),
            CurrentCandidate: discoveryResult.Candidates.First(),
            EvaluationResults: [],
            HasPendingComponents: true
        );
        
        Console.WriteLine($"   Queue initialized with {state.PendingCandidates.Count} candidates");
        Console.WriteLine($"   📋 All candidates in queue:");
        for (int i = 0; i < state.PendingCandidates.Count; i++)
        {
            var c = state.PendingCandidates[i];
            Console.WriteLine($"      [{i+1}] {c.Title}");
        }
        Console.WriteLine($"   ▶️  Processing first: {state.CurrentCandidate!.Title}");
        
        return ValueTask.FromResult(state);
    }

    public static ValueTask<ComponentEvaluationState> UpdateStateAsync(
        ComponentEvaluationState currentState,
        EvaluationResult evaluationResult,
        IWorkflowContext ctx,
        CancellationToken ct)
    {
        Console.WriteLine("\n🔄 [EnrichmentLoopController] Updating queue state...");
        
        // Add evaluation result to history
        var updatedResults = new List<EvaluationResult>(currentState.EvaluationResults) { evaluationResult };
        
        // Remove processed candidate and get next one
        var remainingCandidates = currentState.PendingCandidates.Skip(1).ToList();
        var nextCandidate = remainingCandidates.FirstOrDefault();
        
        var updatedState = new ComponentEvaluationState(
            PendingCandidates: remainingCandidates,
            CurrentCandidate: nextCandidate,
            EvaluationResults: updatedResults,
            HasPendingComponents: remainingCandidates.Count > 0
        );
        
        Console.WriteLine($"   Processed: {evaluationResult.ComponentTitle}");
        Console.WriteLine($"   Remaining candidates: {remainingCandidates.Count}");
        Console.WriteLine($"   HasPendingComponents: {updatedState.HasPendingComponents}");
        
        if (nextCandidate != null)
        {
            Console.WriteLine($"   ▶️  Next: {nextCandidate.Title}");
        }
        else
        {
            Console.WriteLine($"   ✅ All candidates processed - exiting loop");
        }
        
        return ValueTask.FromResult(updatedState);
    }
}
