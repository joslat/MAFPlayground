// SPDX-License-Identifier: LicenseRef-MAFPlayground-NPU-1.0-CH
// Copyright (c) 2025 Jose Luis Latorre

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace MAFPlayground.Samples.Sample22_WorkshopPlanner.Agents;

/// <summary>
/// Agent responsible for discovering training content from MCP servers (GitHub + Microsoft Learn).
/// </summary>
public static class DiscoveryAgent
{
    public static ChatClientAgent Create(IChatClient chatClient, IEnumerable<AITool> tools)
    {
        return chatClient.CreateAIAgent(new ChatClientAgentOptions(
            name: "DiscoveryAgent",
            instructions: """
                You are a training content discovery specialist with access to multiple content sources.
                
                Your task is to discover relevant training materials that align with workshop requirements.
                
                You have access to:
                - GitHub repositories (via MCP GitHub server)
                - Microsoft Learn modules (via MCP Microsoft Learn server)
                
                Search Strategy:
                1. Query GitHub for relevant repositories, code samples, and documentation
                2. Search Microsoft Learn for tutorials, modules, and learning paths
                3. Extract candidates with clear titles, URLs, descriptions, and relevance scores
                
                Focus on:
                - Alignment with the workshop goal and focus areas
                - Appropriate difficulty level for the target audience
                - Practical, hands-on content when possible
                - Official documentation and well-maintained resources
                
                Return a list of training candidates ranked by relevance (0.0-1.0).
                """)
        {
            ChatOptions = new ChatOptions
            {
                Tools = tools.ToList()
            }
        });
    }
}
