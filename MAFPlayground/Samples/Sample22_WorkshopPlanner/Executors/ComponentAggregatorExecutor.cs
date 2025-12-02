// SPDX-License-Identifier: LicenseRef-MAFPlayground-NPU-1.0-CH
// Copyright (c) 2025 Jose Luis Latorre

using Microsoft.Agents.AI.Workflows;

namespace MAFPlayground.Samples.Sample22_WorkshopPlanner.Executors;

/// <summary>
/// Executor that aggregates all evaluation results and filters approved components.
/// </summary>
public static class ComponentAggregatorExecutor
{
    // Store enriched components during the loop (thread-safe for concurrent scenarios)
    // Use Title as key since URLs can vary slightly between enrichment and evaluation
    private static readonly object _cacheLock = new();
    private static readonly Dictionary<string, EnrichedComponent> _enrichedCache = new();

    public static void CacheEnrichedComponent(EnrichedComponent component)
    {
        lock (_cacheLock)
        {
            _enrichedCache[component.Title] = component;
        }
    }

    public static ValueTask<AggregatedComponents> ExecuteAsync(
        ComponentEvaluationState finalState,
        IWorkflowContext ctx,
        CancellationToken ct)
    {
        Console.WriteLine("\n📊 [Aggregation] Aggregating evaluation results...");
        
        var approvedResults = finalState.EvaluationResults.Where(e => e.Approved).ToList();
        
        List<(EnrichedComponent Component, EvaluationResult Evaluation)> approvedComponents;
        lock (_cacheLock)
        {
            approvedComponents = approvedResults
                .Select(eval => 
                {
                    if (_enrichedCache.TryGetValue(eval.ComponentTitle, out var component))
                        return (component, eval);
                    else
                    {
                        Console.WriteLine($"   ⚠️  Warning: Component '{eval.ComponentTitle}' not found in cache");
                        return ((EnrichedComponent?)null, eval);
                    }
                })
                .Where(pair => pair.Item1 != null)
                .Select(pair => (pair.Item1!, pair.eval))
                .ToList();
        }
        
        var aggregated = new AggregatedComponents(
            ApprovedComponents: approvedComponents,
            TotalEvaluated: finalState.EvaluationResults.Count,
            AcceptanceRate: finalState.EvaluationResults.Count > 0
                ? (double)approvedResults.Count / finalState.EvaluationResults.Count
                : 0.0
        );
        
        Console.WriteLine($"   Total evaluated: {aggregated.TotalEvaluated}");
        Console.WriteLine($"   Approved: {aggregated.ApprovedComponents.Count}");
        Console.WriteLine($"   Acceptance rate: {aggregated.AcceptanceRate:P0}");
        
        // Group by suggested use
        var grouped = aggregated.ApprovedComponents
            .GroupBy(pair => pair.Evaluation.SuggestedUse ?? "Other")
            .OrderBy(g => g.Key);
        
        foreach (var group in grouped)
        {
            Console.WriteLine($"   - {group.Key}: {group.Count()} components");
        }
        
        // Clear cache for next run
        lock (_cacheLock)
        {
            _enrichedCache.Clear();
        }
        
        return ValueTask.FromResult(aggregated);
    }
}
