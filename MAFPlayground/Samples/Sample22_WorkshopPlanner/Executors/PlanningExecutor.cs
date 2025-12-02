// SPDX-License-Identifier: LicenseRef-MAFPlayground-NPU-1.0-CH
// Copyright (c) 2025 Jose Luis Latorre

using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;

namespace MAFPlayground.Samples.Sample22_WorkshopPlanner.Executors;

/// <summary>
/// Executor that creates a comprehensive workshop plan from requirements and approved components.
/// </summary>
public static class WorkshopPlannerExecutor
{
    public static async ValueTask<WorkshopPlan> ExecuteAsync(
        (WorkshopRequirements Requirements, AggregatedComponents Components) input,
        IWorkflowContext ctx,
        CancellationToken ct,
        ChatClientAgent agent)
    {
        var (requirements, components) = input;
        
        Console.WriteLine("\n📋 [WorkshopPlanner] Designing workshop structure...");
        
        var componentsSummary = string.Join("\n", components.ApprovedComponents.Select((pair, i) =>
        {
            var (component, eval) = pair;
            return $"{i + 1}. {component.Title} ({component.Source})\n" +
                   $"   Topics: {string.Join(", ", component.TopicsCovered)}\n" +
                   $"   Time: {component.EstimatedTimeMinutes} min | Difficulty: {component.DifficultyLevel}\n" +
                   $"   Suggested Use: {eval.SuggestedUse}\n" +
                   $"   URL: {component.Url}";
        }));
        
        var prompt = $"""
            Create a comprehensive workshop plan using these requirements and approved components:
            
            Workshop Requirements:
            - Goal: {requirements.Goal}
            - Audience Level: {requirements.AudienceLevel}
            - Duration: {requirements.DurationHours} hours
            - Focus Areas: {string.Join(", ", requirements.FocusAreas)}
            - Prerequisites: {string.Join(", ", requirements.Prerequisites)}
            
            Approved Training Components ({components.ApprovedComponents.Count}):
            {componentsSummary}
            
            Design a workshop that:
            1. Organizes these components into logical modules (3-5 modules)
            2. Creates a cohesive learning narrative
            3. Ensures total duration matches the requirement
            4. Includes clear learning objectives for each module
            5. Incorporates the approved components as content sources
            6. Adds hands-on activities and exercises
            7. Defines measurable success criteria
            
            Create an engaging, well-structured workshop with an executive summary.
            """;

        var response = await agent.RunAsync<WorkshopPlan>(prompt, cancellationToken: ct);
        var plan = response.Result;
        
        Console.WriteLine($"   Workshop: {plan.WorkshopTitle}");
        Console.WriteLine($"   Modules: {plan.Modules.Count}");
        Console.WriteLine($"   Total Duration: {plan.TotalDurationHours} hours");
        Console.WriteLine($"   Using {components.ApprovedComponents.Count} approved components");
        
        return plan;
    }
}
