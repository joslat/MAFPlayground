// SPDX-License-Identifier: LicenseRef-MAFPlayground-NPU-1.0-CH
// Copyright (c) 2025 Jose Luis Latorre

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace MAFPlayground.Samples.Sample22_WorkshopPlanner.Agents;

/// <summary>
/// Agent responsible for evaluating enriched training components against workshop requirements.
/// </summary>
public static class EvaluatorAgent
{
    public static ChatClientAgent Create(IChatClient chatClient)
    {
        return chatClient.CreateAIAgent(new ChatClientAgentOptions(
            name: "EvaluatorAgent",
            instructions: """
                You are a workshop content evaluator with expertise in curriculum design.
                
                Your task is to evaluate enriched training components against workshop requirements.
                
                Evaluation Criteria:
                1. Alignment with workshop goal and focus areas (0.0-1.0 score)
                2. Appropriate difficulty level for target audience
                3. Time estimate fits within workshop duration constraints
                4. Quality and completeness of content
                5. Presence of hands-on exercises or practical elements
                
                Decision Process:
                - APPROVE: Component strongly aligns with requirements and adds value
                - REJECT: Component is off-topic, wrong difficulty level, or low quality
                
                For approved components:
                - Provide clear reasoning for acceptance
                - Suggest specific use: "Introduction", "Deep Dive", "Hands-on Lab", "Reference Material", etc.
                
                For rejected components:
                - Explain why it doesn't fit the workshop needs
                - Set suggested_use to null
                
                Be selective - only approve components that genuinely contribute to the workshop goals.
                """)
        {
            ChatOptions = new()
            {
                ResponseFormat = Microsoft.Extensions.AI.ChatResponseFormat.ForJsonSchema<EvaluationResult>()
            }
        });
    }
}
