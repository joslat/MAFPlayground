// SPDX-License-Identifier: LicenseRef-MAFPlayground-NPU-1.0-CH
// Copyright (c) 2025 Jose Luis Latorre

using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;

namespace MAFPlayground.Samples.Sample22_WorkshopPlanner.Executors;

/// <summary>
/// Executor that evaluates an enriched component against workshop requirements.
/// </summary>
public static class EvaluationExecutor
{
    public static async ValueTask<EvaluationResult> ExecuteAsync(
        (WorkshopRequirements Requirements, EnrichedComponent Component) input,
        IWorkflowContext ctx,
        CancellationToken ct,
        ChatClientAgent agent)
    {
        var (requirements, component) = input;
        
        Console.WriteLine($"\n⚖️  [Evaluation] Evaluating: {component.Title}");
        
        var prompt = $"""
            Workshop Requirements:
            - Goal: {requirements.Goal}
            - Audience Level: {requirements.AudienceLevel}
            - Duration: {requirements.DurationHours} hours
            - Focus Areas: {string.Join(", ", requirements.FocusAreas)}
            
            Enriched Component:
            - Title: {component.Title}
            - Source: {component.Source}
            - Topics: {string.Join(", ", component.TopicsCovered)}
            - Time: {component.EstimatedTimeMinutes} minutes
            - Difficulty: {component.DifficultyLevel}
            - Hands-on: {component.HandsOnExercises}
            - Last Updated: {component.LastUpdated}
            
            Evaluate whether this component should be included in the workshop.
            Provide:
            - component_title: exact title
            - component_url: the URL
            - approved: true/false
            - alignment_score: 0.0-1.0
            - reasoning: why approved or rejected
            - suggested_use: "Introduction" | "Deep Dive" | "Hands-on Lab" | "Reference Material" (null if rejected)
            """;
        
        var response = await agent.RunAsync<EvaluationResult>(prompt, cancellationToken: ct);
        var evaluation = response.Result;
        
        Console.WriteLine($"   {(evaluation.Approved ? "✅ APPROVED" : "❌ REJECTED")} - Score: {evaluation.AlignmentScore:F2}");
        Console.WriteLine($"   Reasoning: {evaluation.Reasoning}");
        if (evaluation.Approved && evaluation.SuggestedUse != null)
        {
            Console.WriteLine($"   Suggested Use: {evaluation.SuggestedUse}");
        }
        
        return evaluation;
    }
}
