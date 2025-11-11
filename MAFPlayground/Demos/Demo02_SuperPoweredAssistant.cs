// SPDX-License-Identifier: LicenseRef-MAFPlayground-NPU-1.0-CH
// Copyright (c) 2025 Jose Luis Latorre

using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace MAFPlayground.Demos;

/// <summary>
/// Demo 02: The Super-Powered Personal Assistant
/// 
/// Demonstrates an AI agent that can autonomously orchestrate multiple tools to accomplish complex tasks.
/// The agent can check weather, search restaurants, make bookings, and manage your calendar - all from
/// a single natural language request.
/// 
/// Story: "I want to have lunch today. Help me find a restaurant, book it, and update my calendar."
/// </summary>
internal static class Demo02_SuperPoweredAssistant
{
    public static async Task Execute()
    {
        // Set console encoding to UTF-8 to support emojis and special characters
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        Console.WriteLine("\n=== DEMO 02: THE SUPER-POWERED PERSONAL ASSISTANT ===\n");

        // Create the AzureOpenAIClient using the lazy config from AIConfig
        var azureClient = new AzureOpenAIClient(AIConfig.Endpoint, AIConfig.KeyCredential);

        // Get the chat client
        var chatClient = azureClient
            .GetChatClient(AIConfig.ModelDeployment)
            .AsIChatClient();

        // Create the agent with all tools
        AIAgent assistant = new ChatClientAgent(
            chatClient,
            new ChatClientAgentOptions
            {
                Name = "PersonalAssistant",
                Instructions = """
                    You are Johnny, a fun, friendly and helpful personal assistant. 
                    
                    You can:
                    - Check weather and calendar
                    - Find and book restaurants
                    - Update the calendar
                    - Show the full schedule
                    - Book transportation (taxi, Uber, Lyft)
                    
                    Be proactive and helpful. When booking restaurants, consider the weather
                    and existing calendar to suggest good times. Don't forget to offer transportation
                    if the user needs to get to their destination!
                    Also do not just try once, if something fails, try again with different parameters - 
                    all to assist in the most perfect way, delivering solutions!!.
                    """,
                ChatOptions = new ChatOptions
                {
                    Tools = [
                        AIFunctionFactory.Create(SuperPoweredAssistantTools.GetCurrentDate),
                        AIFunctionFactory.Create(SuperPoweredAssistantTools.GetWeather),
                        AIFunctionFactory.Create(SuperPoweredAssistantTools.GetTodayAgenda),
                        AIFunctionFactory.Create(SuperPoweredAssistantTools.SearchRestaurants),
                        AIFunctionFactory.Create(SuperPoweredAssistantTools.BookRestaurant),
                        AIFunctionFactory.Create(SuperPoweredAssistantTools.AddToCalendar),
                        AIFunctionFactory.Create(SuperPoweredAssistantTools.PrintCalendarAsMarkdown),
                        AIFunctionFactory.Create(SuperPoweredAssistantTools.BookTransport)
                    ]
                }
            });

        Console.WriteLine("🤖 Personal Assistant is ready!\n");
        Console.WriteLine("📝 Your request:");
        Console.WriteLine("   'I want to have lunch outside today since the weather is nice.");
        Console.WriteLine("    Find me an Italian restaurant with outdoor seating, book it for 12:30,");
        Console.WriteLine("    add it to my calendar, and show me my updated schedule.'\n");
        Console.WriteLine(new string('=', 80));
        Console.WriteLine();

        var response = await assistant.RunAsync(
            "I want to have lunch outside today since the weather is nice. " +
            "Find me an Italian restaurant with outdoor seating, book it for 12:30, " +
            "book me an Uber to get there at 12:15, " +
            "add it to my calendar, and show me my updated schedule.");

        Console.WriteLine("\n" + new string('=', 80));
        Console.WriteLine("\n🎉 ASSISTANT RESPONSE:\n");
        Console.WriteLine(response.Text);

        Console.WriteLine("\n✅ Demo Complete: The agent autonomously orchestrated multiple tools in the right sequence!");
    }
}