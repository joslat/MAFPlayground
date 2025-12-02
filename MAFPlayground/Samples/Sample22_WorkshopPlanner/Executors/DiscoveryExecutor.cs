// SPDX-License-Identifier: LicenseRef-MAFPlayground-NPU-1.0-CH
// Copyright (c) 2025 Jose Luis Latorre

using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace MAFPlayground.Samples.Sample22_WorkshopPlanner.Executors;

/// <summary>
/// Executor that discovers training candidates from MCP servers (GitHub and Microsoft Learn).
/// </summary>
public static class DiscoveryExecutor
{
    public static async ValueTask<DiscoveryResult> ExecuteAsync(
        WorkshopRequirements requirements,
        IWorkflowContext ctx,
        CancellationToken ct,
        ChatClientAgent agent,
        IChatClient chatClient)
    {
        Console.WriteLine("\n🔍 [Discovery] Searching for training materials...");
        Console.WriteLine($"   Searching for: {requirements.Goal}");
        Console.WriteLine($"   Focus areas: {string.Join(", ", requirements.FocusAreas)}");

        var prompt = $"""
            Workshop Requirements:
            - Goal: {requirements.Goal}
            - Audience Level: {requirements.AudienceLevel}
            - Duration: {requirements.DurationHours} hours
            - Focus Areas: {string.Join(", ", requirements.FocusAreas)}
            - Prerequisites: {string.Join(", ", requirements.Prerequisites)}
            
            Search for relevant training materials using the available MCP tools.
            Look for repositories, documentation, tutorials, and learning modules that align with these requirements.
            
            Return a list of 5-7 high-quality candidates found from GitHub and Microsoft Learn.
            """;

        try
        {
            var response = await agent.RunAsync<List<TrainingCandidate>>(prompt, cancellationToken: ct);
            var candidates = response.Result ?? [];
            
            var result = new DiscoveryResult(candidates, candidates.Count);
            
            Console.WriteLine($"   ✅ Discovered {result.TotalDiscovered} training candidates");
            foreach (var candidate in candidates.OrderByDescending(c => c.RelevanceScore))
            {
                Console.WriteLine($"   - [{candidate.RelevanceScore:F2}] {candidate.Title} ({candidate.Source})");
            }
            
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ⚠️  Discovery failed: {ex.Message}");
            // Return empty result on failure
            return new DiscoveryResult([], 0);
        }
    }
}
