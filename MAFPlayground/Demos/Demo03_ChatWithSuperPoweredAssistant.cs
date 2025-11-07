// SPDX-License-Identifier: LicenseRef-MAFPlayground-NPU-1.0-CH
// Copyright (c) 2025 Jose Luis Latorre

using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace MAFPlayground.Demos;

/// <summary>
/// Demo 03: Interactive Chat with Super-Powered Personal Assistant
/// 
/// Demonstrates a conversational interaction with Johnny, the AI assistant.
/// Users can freely chat with the assistant and request help with various tasks
/// like checking weather, booking restaurants, managing calendar, and arranging transportation.
/// 
/// Type 'q' or 'quit' to exit the conversation.
/// </summary>
internal static class Demo03_ChatWithSuperPoweredAssistant
{
    public static async Task Execute()
    {
        // Set console encoding to UTF-8 to support emojis and special characters
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        Console.WriteLine("\n=== DEMO 03: CHAT WITH SUPER-POWERED PERSONAL ASSISTANT ===\n");

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
                    all to assist in the most perfect way, delivering solutions!!
                    
                    IMPORTANT:
                    - If you book a transport, book a restaurant, also of course update the agenda accordingly.
                    - If you update the agenda, always print the full updated agenda in markdown format afterwards.
                    - If the transport failed to be booked, try different options until successful with suitable timing (5-10 minutes, or different transport).
                    - Be creative, proactive and engaging in your responses.
                    - Take the initiative! in doing and if you think is important, then ask the user. But do not always ask, be smart about it.

                    Keep your responses conversational and engaging. Remember context from previous
                    messages in the conversation.
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

        // Create a new thread.
        AgentThread thread = assistant.GetNewThread();

        Console.WriteLine("🤖 Johnny (Personal Assistant) is ready to help!\n");
        Console.WriteLine("💡 You can ask Johnny to:");
        Console.WriteLine("   • Check your calendar and weather");
        Console.WriteLine("   • Find and book restaurants");
        Console.WriteLine("   • Arrange transportation (taxi, Uber, Lyft)");
        Console.WriteLine("   • Manage your schedule");
        Console.WriteLine();
        Console.WriteLine("Type 'q' or 'quit' to exit the conversation.\n");
        Console.WriteLine(new string('=', 80));
        Console.WriteLine();


        // Conversation loop
        while (true)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("You: ");
            Console.ResetColor();
            
            string? userInput = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(userInput))
            {
                continue;
            }

            // Check for quit command
            if (userInput.Equals("q", StringComparison.OrdinalIgnoreCase) || 
                userInput.Equals("quit", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("👋 Goodbye! Johnny is signing off.");
                Console.ResetColor();
                break;
            }

            // Get response from assistant
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("Johnny: ");
            Console.ResetColor();

            try
            {
                var response = await assistant.RunAsync(userInput, thread);
                Console.WriteLine(response.Text);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Oops! Something went wrong: {ex.Message}");
                Console.ResetColor();
            }

            Console.WriteLine();
        }

        Console.WriteLine();
        Console.WriteLine("✅ Demo Complete: Interactive chat session ended.");
    }
}