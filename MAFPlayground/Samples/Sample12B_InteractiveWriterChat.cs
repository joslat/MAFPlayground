// SPDX-License-Identifier: LicenseRef-MAFPlayground-NPU-1.0-CH
// Copyright (c) 2025 Jose Luis Latorre

using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MAFPlayground.Samples;

/// <summary>
/// This sample demonstrates an interactive chat session with a Writer agent.
/// 
/// Key concepts:
/// 1. Creating a ChatClientAgent with specific instructions
/// 2. Maintaining conversation history across multiple turns
/// 3. Interactive feedback loop where user provides feedback and writer refines
/// 4. Using RunAsync with message history for context-aware responses
/// 
/// This is useful for:
/// - Interactive content creation and refinement
/// - User-guided iterative improvement
/// - Testing agent behavior with real-time feedback
/// - Building conversational writing assistants
/// </summary>
/// <remarks>
/// Pre-requisites:
/// - An Azure OpenAI chat completion deployment must be configured.
/// - User can exit by typing: quit, q, exit, done, or approved
/// </remarks>
internal static class Sample12B_InteractiveWriterChat
{
    public static async Task Execute()
    {
        Console.WriteLine("=== Sample 12B: Interactive Writer Chat ===");
        Console.WriteLine("Collaborate with a Writer agent to create and refine a story.\n");

        // Set up the Azure OpenAI client using AIConfig
        var azureClient = new AzureOpenAIClient(AIConfig.Endpoint, AIConfig.KeyCredential);
        var deploymentName = AIConfig.ModelDeployment;
        IChatClient chatClient = azureClient.GetChatClient(deploymentName).AsIChatClient();

        // ====================================
        // Step 1: Create Writer agent
        // ====================================
        Console.WriteLine("Creating Writer agent...\n");

        ChatClientAgent writer = new(
            chatClient,
            name: "Writer",
            instructions: @"You are a creative writer who crafts engaging stories.
Focus on creating vivid descriptions, interesting characters, and compelling narratives.
When you receive feedback from the editor, carefully incorporate their suggestions to improve your work.
Build upon the previous version of the story while addressing the editor's critiques."
        );

        Console.WriteLine("✅ Writer agent created.\n");

        // ====================================
        // Step 2: Initial story generation
        // ====================================
        Console.WriteLine("--- Initial Story Generation ---\n");
        Console.WriteLine("Let's start by creating an initial story.\n");
        Console.Write("Enter your story prompt (or press Enter for default): ");
        
        string? userPrompt = Console.ReadLine();
        
        if (string.IsNullOrWhiteSpace(userPrompt))
        {
            userPrompt = "Write a short story about a mysterious library where books come to life at midnight. Keep it around 150-200 words.";
            Console.WriteLine($"Using default prompt: {userPrompt}");
        }

        Console.WriteLine();
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine();

        // Maintain conversation history
        var conversationHistory = new List<ChatMessage>
        {
            new(ChatRole.User, userPrompt)
        };

        AgentRunResponse response = await writer.RunAsync(conversationHistory);
        
        Console.WriteLine($"[Writer]:");
        Console.WriteLine(response.Text);
        Console.WriteLine();
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine();

        // Add writer's response to history
        conversationHistory.Add(new ChatMessage(ChatRole.Assistant, response.Text));

        // ====================================
        // Step 3: Interactive feedback loop
        // ====================================
        Console.WriteLine("--- Interactive Feedback Loop ---");
        Console.WriteLine("Provide feedback to refine the story.");
        Console.WriteLine("Type 'quit', 'q', 'exit', 'done', or 'approved' to finish.\n");

        int iteration = 1;

        while (true)
        {
            Console.WriteLine($"Iteration {iteration}:");
            Console.Write("Your feedback: ");
            
            string? feedback = Console.ReadLine();

            // Check for exit commands
            if (string.IsNullOrWhiteSpace(feedback) || IsExitCommand(feedback))
            {
                if (IsApprovalCommand(feedback))
                {
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("✅ Story approved! Great work!");
                    Console.ResetColor();
                }
                else
                {
                    Console.WriteLine();
                    Console.WriteLine("👋 Exiting interactive session.");
                }
                break;
            }

            // Add user feedback to conversation history
            conversationHistory.Add(new ChatMessage(ChatRole.User, feedback));

            Console.WriteLine();
            Console.WriteLine("=".PadRight(80, '='));
            Console.WriteLine();

            // Get writer's revised version
            AgentRunResponse revisedResponse = await writer.RunAsync(conversationHistory);
            
            Console.WriteLine($"[Writer - Revision {iteration}]:");
            Console.WriteLine(revisedResponse.Text);
            Console.WriteLine();
            Console.WriteLine("=".PadRight(80, '='));
            Console.WriteLine();

            // Add writer's revised response to history
            conversationHistory.Add(new ChatMessage(ChatRole.Assistant, revisedResponse.Text));

            iteration++;
        }

        // ====================================
        // Step 4: Summary
        // ====================================
        Console.WriteLine();
        Console.WriteLine("=== Session Summary ===");
        Console.WriteLine($"Total iterations: {iteration - 1}");
        Console.WriteLine($"Total messages in conversation: {conversationHistory.Count}");
        Console.WriteLine();

        Console.WriteLine("✅ Sample 12B Complete!");
        Console.WriteLine();
        Console.WriteLine("Key Takeaways:");
        Console.WriteLine("- Writer agent maintains context across multiple turns");
        Console.WriteLine("- User feedback guides iterative refinement");
        Console.WriteLine("- Conversation history enables context-aware responses");
        Console.WriteLine("- Interactive loop allows for collaborative content creation");
    }

    /// <summary>
    /// Checks if the user input is an exit command.
    /// </summary>
    private static bool IsExitCommand(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return false;

        string[] exitCommands = { "quit", "q", "exit", "done", "approved" };
        return exitCommands.Contains(input.Trim().ToLowerInvariant());
    }

    /// <summary>
    /// Checks if the user input is an approval command.
    /// </summary>
    private static bool IsApprovalCommand(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return false;

        string[] approvalCommands = { "done", "approved" };
        return approvalCommands.Contains(input.Trim().ToLowerInvariant());
    }
}