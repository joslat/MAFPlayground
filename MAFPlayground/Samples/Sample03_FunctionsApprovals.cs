// Copyright (c) Microsoft. All rights reserved.
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

namespace MAFPlayground.Samples;

internal static class Sample03_FunctionsApprovals
{
    // Simple function tool that fakes getting the weather for a given location.
    [Description("Get the weather for a given location.")]
    private static string GetWeather([Description("The location to get the weather for.")] string location)
        => $"The weather in {location} is cloudy with a high of 15�C.";

    public static async Task Execute()
    {
        // Create the AzureOpenAIClient using the lazy config from AIConfig
        AzureOpenAIClient azureClient = new AzureOpenAIClient(AIConfig.Endpoint, AIConfig.KeyCredential);

        // Create an AIFunction and wrap it in an ApprovalRequiredAIFunction
        AIFunction weatherFunction = AIFunctionFactory.Create(GetWeather);
#pragma warning disable MEAI001
        ApprovalRequiredAIFunction approvalRequiredWeatherFunction = new ApprovalRequiredAIFunction(weatherFunction);
#pragma warning restore MEAI001

        // Create the agent and pass the approval-requiring tool to it
        AIAgent agent = azureClient
            .GetChatClient(AIConfig.ModelDeployment)
            .AsAIAgent(
                instructions: "You are a helpful assistant that may request human approval before running tools.",
                tools: new[] { approvalRequiredWeatherFunction });

        // Start a new session for the agent run
        var session = await agent.CreateSessionAsync();

        // Initial prompt that will likely trigger a function call requiring approval
        string prompt = "What is the weather like in Amsterdam?";
        AgentResponse response = await agent.RunAsync(prompt, session);

        // Loop until no more function approval requests are present
        while (true)
        {
#pragma warning disable MEAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates.
            var functionApprovalRequests = response.Messages
                .SelectMany(x => x.Contents)
                .OfType<ToolApprovalRequestContent>()
                .ToList();

            if (functionApprovalRequests.Count == 0)
            {
                // No pending approvals; break out and print final text output if any
                var finalText = response.Messages
                    .SelectMany(m => m.Contents)
                    .OfType<TextContent>()
                    .Select(t => t.Text)
                    .FirstOrDefault();

                Console.WriteLine("Agent result:");
                Console.WriteLine(finalText ?? "(no text content in response)");
                break;
            }

            // For simplicity assume one approval request; show details and ask operator
            ToolApprovalRequestContent requestContent = functionApprovalRequests.First();
            var functionCall = (FunctionCallContent)requestContent.ToolCall!;
            Console.WriteLine($"Agent requests approval to execute function '{functionCall.Name}'");

            // Print arguments iteratively if possible
            PrintArguments(functionCall.Arguments);

            Console.Write("Approve this function call? (y/N): ");
            var key = Console.ReadKey(intercept: true);
            Console.WriteLine();
            bool approved = char.ToLowerInvariant(key.KeyChar) == 'y';

            // Create a ToolApprovalResponseContent and pass it back as a new user message on the same session
            ToolApprovalResponseContent approvalResponse = requestContent.CreateResponse(approved);
            ChatMessage approvalMessage = new ChatMessage(ChatRole.User, new[] { approvalResponse });

            // Run the agent again with the approval/rejection on the same session
            response = await agent.RunAsync(approvalMessage, session);

            if (!approved)
            {
                Console.WriteLine("Function call was rejected by the operator. Agent run continues (if applicable).");
            }
#pragma warning restore MEAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates.
        }

        static void PrintArguments(object? argsObj)
        {
            if (argsObj == null)
            {
                Console.WriteLine("Function call arguments: (no args)");
                return;
            }

            // If the SDK provides a dictionary-like object, enumerate it
            if (argsObj is IDictionary<string, object> dict && dict.Count > 0)
            {
                Console.WriteLine("Function call arguments:");
                foreach (var kv in dict)
                {
                    Console.WriteLine($"- {kv.Key}: {FormatArgValue(kv.Value)}");
                }

                return;
            }

            // If the SDK returns a JsonElement, enumerate it
            if (argsObj is JsonElement je)
            {
                if (je.ValueKind == JsonValueKind.Object && je.EnumerateObject().Any())
                {
                    Console.WriteLine("Function call arguments:");
                    foreach (var prop in je.EnumerateObject())
                    {
                        Console.WriteLine($"- {prop.Name}: {prop.Value.GetRawText()}");
                    }

                    return;
                }

                // Fallback to raw text for other JsonKinds
                Console.WriteLine("Function call arguments:");
                Console.WriteLine(je.GetRawText());
                return;
            }

            // If it's a string, try to parse as JSON and then print nicely; otherwise print raw string
            if (argsObj is string s)
            {
                if (TryParseJson(s, out var parsed))
                {
                    PrintArguments(parsed);
                    return;
                }

                Console.WriteLine("Function call arguments (raw string):");
                Console.WriteLine(s);
                return;
            }

            // Generic fallback
            Console.WriteLine("Function call arguments:");
            Console.WriteLine(argsObj.ToString() ?? "(no args)");
        }

        static bool TryParseJson(string s, out JsonElement element)
        {
            element = default;
            try
            {
                using var doc = JsonDocument.Parse(s);
                element = doc.RootElement.Clone();
                return true;
            }
            catch
            {
                return false;
            }
        }

        static string FormatArgValue(object? value)
        {
            if (value == null) return "(null)";

            if (value is JsonElement j)
            {
                return j.ValueKind switch
                {
                    JsonValueKind.String => j.GetString() ?? "(empty)",
                    JsonValueKind.Number => j.GetRawText(),
                    JsonValueKind.True or JsonValueKind.False => j.GetRawText(),
                    JsonValueKind.Object or JsonValueKind.Array => j.GetRawText(),
                    _ => j.ToString() ?? "(unknown)"
                };
            }

            if (value is IDictionary<string, object> nestedDict)
            {
                return "{" + string.Join(", ", nestedDict.Select(kv => $"{kv.Key}: {FormatArgValue(kv.Value)}")) + "}";
            }

            return value.ToString() ?? "(empty)";
        }
    }
}