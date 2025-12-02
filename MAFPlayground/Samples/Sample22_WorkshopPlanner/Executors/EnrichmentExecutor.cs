// SPDX-License-Identifier: LicenseRef-MAFPlayground-NPU-1.0-CH
// Copyright (c) 2025 Jose Luis Latorre

using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;

namespace MAFPlayground.Samples.Sample22_WorkshopPlanner.Executors;

/// <summary>
/// Executor that enriches a single training candidate with detailed metadata using MCP tools.
/// </summary>
public static class EnrichmentExecutor
{
    public static async ValueTask<EnrichedComponent> ExecuteAsync(
        ComponentEvaluationState state,
        IWorkflowContext ctx,
        CancellationToken ct,
        ChatClientAgent agent)
    {
        if (state.CurrentCandidate == null)
        {
            throw new InvalidOperationException("No current candidate to enrich");
        }
        
        var candidate = state.CurrentCandidate;
        
        Console.WriteLine($"\n📝 [Enrichment] Enriching: {candidate.Title}");
        Console.WriteLine($"   Source: {candidate.Source}");
        Console.WriteLine($"   URL: {candidate.Url}");
        
        var prompt = $"""
            Enrich this training candidate:
            - Title: {candidate.Title}
            - URL: {candidate.Url}
            - Source: {candidate.Source}
            - Description: {candidate.Description}
            
            Use available tools to fetch more details (e.g. README, module summary).
            Extract topics, estimate time, difficulty, and check for hands-on exercises.
            """;

        try 
        {
             var response = await agent.RunAsync<EnrichedComponent>(prompt, cancellationToken: ct);
             var enriched = response.Result;
             
             if (enriched == null) throw new Exception("Failed to enrich candidate");

            Console.WriteLine($"   Topics: {string.Join(", ", enriched.TopicsCovered)}");
            Console.WriteLine($"   Time: {enriched.EstimatedTimeMinutes} min | Difficulty: {enriched.DifficultyLevel}");
            Console.WriteLine($"   Hands-on: {enriched.HandsOnExercises}");
            
            return enriched;
        }
        catch (Exception ex)
        {
             Console.WriteLine($"   ⚠️ Enrichment failed: {ex.Message}. Using basic info.");
             // Fallback to basic info if enrichment fails
             return new EnrichedComponent(
                Title: candidate.Title,
                Url: candidate.Url,
                Source: candidate.Source,
                Description: candidate.Description,
                TopicsCovered: ["General"],
                EstimatedTimeMinutes: 60,
                DifficultyLevel: "Intermediate",
                HandsOnExercises: false,
                LastUpdated: DateTime.UtcNow.ToString("yyyy-MM-dd")
             );
        }
    }
}
