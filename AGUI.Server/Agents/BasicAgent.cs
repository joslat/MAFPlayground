// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
// Modified for MAFPlayground by Jose Luis Latorre

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace AGUI.Server.Agents;

/// <summary>
/// Basic AI agent without tools - simple conversational assistant.
/// </summary>
public static class BasicAgent
{
    public static AIAgent Create(IChatClient chatClient)
    {
        return chatClient.CreateAIAgent(
            name: "BasicAssistant",
            instructions: "You are a helpful assistant that provides clear and concise answers.");
    }

    public static string GetDescription()
    {
        return "Basic conversational assistant (no tools)";
    }
}
