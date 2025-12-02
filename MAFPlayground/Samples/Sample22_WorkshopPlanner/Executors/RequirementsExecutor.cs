// SPDX-License-Identifier: LicenseRef-MAFPlayground-NPU-1.0-CH
// Copyright (c) 2025 Jose Luis Latorre

using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;

namespace MAFPlayground.Samples.Sample22_WorkshopPlanner.Executors;

/// <summary>
/// Executor that transforms a natural language workshop request into structured requirements.
/// </summary>
public static class RequirementsExecutor
{
    public static async ValueTask<WorkshopRequirements> ExecuteAsync(
        string input, 
        IWorkflowContext ctx, 
        CancellationToken ct,
        ChatClientAgent agent)
    {
        Console.WriteLine("🎯 [Requirements] Analyzing workshop request...");
        
        var response = await agent.RunAsync<WorkshopRequirements>(input, cancellationToken: ct);
        var requirements = response.Result;
        
        Console.WriteLine($"   Goal: {requirements.Goal}");
        Console.WriteLine($"   Audience: {requirements.AudienceLevel}");
        Console.WriteLine($"   Duration: {requirements.DurationHours} hours");
        Console.WriteLine($"   Focus Areas: {string.Join(", ", requirements.FocusAreas)}");
        
        return requirements;
    }
}
