// SPDX-License-Identifier: LicenseRef-MAFPlayground-NPU-1.0-CH
// Copyright (c) 2025 Jose Luis Latorre

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace MAFPlayground.Samples.Sample22_WorkshopPlanner.Agents;

/// <summary>
/// Agent responsible for enriching training content with detailed metadata.
/// </summary>
public static class EnricherAgent
{
    public static ChatClientAgent Create(IChatClient chatClient, IEnumerable<AITool> tools)
    {
        return chatClient.CreateAIAgent(new ChatClientAgentOptions(
            name: "EnricherAgent",
            instructions: """
                You are a content enrichment specialist.
                
                Your task is to analyze a specific training resource (GitHub repo or MS Learn module) 
                and extract detailed metadata to enrich the workshop plan.
                
                You have access to tools to read file content, get repository details, etc.
                
                For each candidate:
                1. Use tools to fetch the README, description, or module summary.
                2. Analyze the content to determine:
                   - Topics covered (list of keywords)
                   - Estimated time to complete (in minutes)
                   - Difficulty level (Beginner, Intermediate, Advanced)
                   - Whether it includes hands-on exercises
                
                Return the enriched metadata in a structured format.
                """)
        {
            ChatOptions = new ChatOptions
            {
                Tools = tools.ToList()
            }
        });
    }
}
