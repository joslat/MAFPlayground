// SPDX-License-Identifier: LicenseRef-MAFPlayground-NPU-1.0-CH
// Copyright (c) 2025 Jose Luis Latorre

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace MAFPlayground.Samples.Sample22_WorkshopPlanner.Agents;

/// <summary>
/// Agent responsible for extracting structured workshop requirements from natural language.
/// </summary>
public static class RequirementsAgent
{
    public static ChatClientAgent Create(IChatClient chatClient)
    {
        return chatClient.CreateAIAgent(new ChatClientAgentOptions(
            name: "RequirementsAnalyst",
            instructions: """
                Extract structured workshop requirements from the user's request.
                Identify: goal, audience level, duration, focus areas, and prerequisites.
                Be specific and realistic in your extraction.
                """)
        {
            ChatOptions = new()
            {
                ResponseFormat = Microsoft.Extensions.AI.ChatResponseFormat.ForJsonSchema<WorkshopRequirements>()
            }
        });
    }
}
