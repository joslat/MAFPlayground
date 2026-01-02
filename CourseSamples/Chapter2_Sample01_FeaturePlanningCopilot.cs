// SPDX-License-Identifier: LicenseRef-MAFPlayground-NPU-1.0-CH
// Copyright (c) 2025 Jose Luis Latorre

using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace CourseSamples;

/// <summary>
/// Chapter 2 - Sample 01: Your First Feature Planning Copilot Agent
/// 
/// This sample demonstrates the simplest possible AI agent:
/// - Single system prompt defining the agent's behavior
/// - Input → Processing → Structured Output
/// - No memory, no tools, no external integrations
/// 
/// The Feature Planning Copilot transforms rough feature ideas into
/// well-structured feature specifications ready for development planning.
/// </summary>
public static class Chapter2_Sample01_FeaturePlanningCopilot
{
    #region Agent Configuration

    /// <summary>
    /// The name of the agent
    /// </summary>
    public const string AgentName = "FeaturePlanningCopilot";

    /// <summary>
    /// The system instructions for the Feature Planning Copilot
    /// </summary>
    public const string AgentInstructions = """
        You are the Feature Planning Copilot 📋 - an expert product manager and software 
        architect who transforms rough feature ideas into well-structured feature specifications.

        When given a feature idea, you produce a structured specification in this format:

        ## 📋 Feature Specification

        ### Feature Title
        [Clear, concise title]

        ### Summary
        [One paragraph describing the feature and its value]

        ### User Story
        As a [type of user], I want [goal] so that [benefit].

        ### Acceptance Criteria
        - [ ] [Testable criterion 1]
        - [ ] [Testable criterion 2]
        - [ ] [Testable criterion 3]
        (Add more as needed)

        ### Technical Considerations
        - [Key technical aspect 1]
        - [Key technical aspect 2]

        ### Dependencies
        - [Any prerequisite features or systems]

        ### Priority & Effort
        [Priority: High/Medium/Low] | [Effort: T-Shirt size] - [Brief reasoning]

        ---

        Be concise but thorough. Always produce consistent, well-formatted specifications.
        """;

    /// <summary>
    /// Sample feature ideas for testing - varying levels of clarity
    /// </summary>
    public static readonly string[] SampleFeatureIdeas = new[]
    {
        // Vague input
        "dark mode",
        // Medium detail input
        "Users should be able to export their data as PDF",
        // Well-defined input
        "As a team lead, I want to see a real-time dashboard showing my team's task completion rates, active blockers, and workload distribution so I can identify bottlenecks and rebalance work effectively"
    };

    #endregion

    #region Agent Factory

    /// <summary>
    /// Creates a Feature Planning Copilot agent using the default Azure OpenAI configuration.
    /// </summary>
    /// <returns>A configured AIAgent ready to process feature ideas</returns>
    public static AIAgent CreateAgent()
    {
        var azureClient = new AzureOpenAIClient(AIConfig.Endpoint, AIConfig.KeyCredential);
        var chatClient = azureClient
            .GetChatClient(AIConfig.ModelDeployment)
            .AsIChatClient();

        return CreateAgent(chatClient);
    }

    /// <summary>
    /// Creates a Feature Planning Copilot agent using the provided chat client.
    /// </summary>
    /// <param name="chatClient">The chat client to use for the agent</param>
    /// <returns>A configured AIAgent ready to process feature ideas</returns>
    public static AIAgent CreateAgent(IChatClient chatClient)
    {
        return new ChatClientAgent(
            chatClient,
            new ChatClientAgentOptions
            {
                Name = AgentName,
                Instructions = AgentInstructions
            });
    }

    #endregion

    #region Execution Methods

    /// <summary>
    /// Main execution - demonstrates the agent with sample inputs
    /// </summary>
    public static async Task Execute()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        Console.WriteLine("\n═══════════════════════════════════════════════════════════════════════════════");
        Console.WriteLine("   CHAPTER 2 - SAMPLE 01: YOUR FIRST FEATURE PLANNING COPILOT AGENT");
        Console.WriteLine("═══════════════════════════════════════════════════════════════════════════════\n");

        // Create agent using factory method
        AIAgent featurePlanningCopilot = CreateAgent();

        Console.WriteLine("🤖 Agent created successfully!\n");
        Console.WriteLine($"   Name: {AgentName}");
        Console.WriteLine("   Type: Basic ChatClientAgent (no memory, no tools)\n");

        // Process each sample feature idea
        foreach (var featureIdea in SampleFeatureIdeas)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"📝 INPUT: {featureIdea}");
            Console.ResetColor();
            Console.WriteLine(new string('-', 80));

            AgentThread thread = featurePlanningCopilot.GetNewThread();

            try
            {
                var response = await featurePlanningCopilot.RunAsync(featureIdea, thread);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\n🎯 STRUCTURED OUTPUT:");
                Console.ResetColor();
                Console.WriteLine(response.Text);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Error: {ex.Message}");
                Console.ResetColor();
            }

            Console.WriteLine("\n" + new string('═', 80) + "\n");
        }

        Console.WriteLine("✅ Sample Complete!");
        Console.WriteLine("💡 Notice how the agent transforms vague ideas into structured specifications.\n");
    }

    /// <summary>
    /// Interactive mode - lets you enter your own feature ideas
    /// </summary>
    public static async Task ExecuteInteractive()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        Console.WriteLine("\n═══════════════════════════════════════════════════════════════════════════════");
        Console.WriteLine("   CHAPTER 2 - SAMPLE 01: FEATURE PLANNING COPILOT (INTERACTIVE MODE)");
        Console.WriteLine("═══════════════════════════════════════════════════════════════════════════════\n");

        // Create agent using factory method
        AIAgent featurePlanningCopilot = CreateAgent();

        Console.WriteLine("🤖 Feature Planning Copilot is ready!");
        Console.WriteLine("   Enter your feature ideas and get structured specifications.");
        Console.WriteLine("   Type 'q' or 'quit' to exit.\n");

        while (true)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("Your Feature Idea: ");
            Console.ResetColor();

            string? input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input))
                continue;

            if (input.Equals("q", StringComparison.OrdinalIgnoreCase) ||
                input.Equals("quit", StringComparison.OrdinalIgnoreCase))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("\n👋 Goodbye!");
                Console.ResetColor();
                break;
            }

            Console.WriteLine();

            AgentThread thread = featurePlanningCopilot.GetNewThread();

            try
            {
                var response = await featurePlanningCopilot.RunAsync(input, thread);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("📋 Copilot: ");
                Console.ResetColor();
                Console.WriteLine(response.Text);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Error: {ex.Message}");
                Console.ResetColor();
            }

            Console.WriteLine();
        }
    }

    /// <summary>
    /// Runs the agent in DevUI mode - a web-based interactive interface
    /// Open http://localhost:5000/devui in your browser
    /// </summary>
    public static void ExecuteWithDevUI()
    {
        Console.WriteLine("\n═══════════════════════════════════════════════════════════════════════════════");
        Console.WriteLine("   CHAPTER 2 - SAMPLE 01: FEATURE PLANNING COPILOT (DEVUI MODE)");
        Console.WriteLine("═══════════════════════════════════════════════════════════════════════════════\n");

        DevUIHelper.RunWithDevUI(AgentName, AgentInstructions);
    }

    #endregion
}
