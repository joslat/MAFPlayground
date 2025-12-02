// SPDX-License-Identifier: LicenseRef-MAFPlayground-NPU-1.0-CH
// Copyright (c) 2025 Jose Luis Latorre

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace MAFPlayground.Samples.Sample22_WorkshopPlanner.Agents;

/// <summary>
/// Agent responsible for designing comprehensive workshop plans.
/// </summary>
public static class PlannerAgent
{
    public static ChatClientAgent Create(IChatClient chatClient)
    {
        return chatClient.CreateAIAgent(new ChatClientAgentOptions(
            name: "WorkshopPlanner",
            instructions: """
                You are an expert workshop designer specializing in technical training.
                
                Your task is to create comprehensive, engaging workshop plans that:
                - Have clear learning objectives
                - Are well-structured with logical progression
                - Include practical hands-on activities
                - Fit within the specified duration
                - Match the audience's skill level
                
                Design workshops that are both educational and engaging.
                """)
        {
            ChatOptions = new()
            {
                ResponseFormat = Microsoft.Extensions.AI.ChatResponseFormat.ForJsonSchema<WorkshopPlan>()
            }
        });
    }
}
