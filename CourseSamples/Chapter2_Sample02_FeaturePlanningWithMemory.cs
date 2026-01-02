// SPDX-License-Identifier: LicenseRef-MAFPlayground-NPU-1.0-CH
// Copyright (c) 2025 Jose Luis Latorre

using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using System.Text.Json;

namespace CourseSamples;

/// <summary>
/// Chapter 2 - Sample 02: Giving Memory to our Feature Planning Agent
/// 
/// This sample builds on Sample 01 by adding project memory to the Feature Planning Copilot.
/// The agent now remembers context across requests.
/// </summary>
public static class Chapter2_Sample02_FeaturePlanningWithMemory
{
    #region Agent Configuration

    public const string AgentName = "FeaturePlanningCopilotWithMemory";

    public const string AgentInstructions = """
        You are the Feature Planning Copilot 📋 with Project Memory - an expert product manager 
        and software architect who transforms rough feature ideas into well-structured feature 
        specifications that are ALIGNED with the project context.

        CRITICAL: You will receive project context including:
        - Project name and tech stack
        - Constraints and team preferences
        - Previous features and key decisions

        You MUST use this context to:
        1. Ensure technical considerations match the tech stack
        2. Respect all constraints in your acceptance criteria
        3. Align with team preferences in your recommendations
        4. Reference or build upon previous features when relevant
        5. Stay consistent with recorded decisions

        Output format - Feature Specification:

        ## 📋 Feature Specification

        ### Feature Title
        [Clear, concise title]

        ### Summary
        [One paragraph - mention how this fits the project context]

        ### User Story
        As a [type of user], I want [goal] so that [benefit].

        ### Acceptance Criteria
        - [ ] [Criterion that respects project constraints]
        - [ ] [Criterion aligned with tech stack]
        (Add more as needed)

        ### Technical Considerations
        - [Must align with project's tech stack]
        - [Respect architectural decisions]

        ### Dependencies
        - [May reference previous features]

        ### Priority & Effort
        [Priority] | [T-Shirt size] - [Reasoning based on context]

        ---

        Always produce consistent, project-aware specifications!
        """;

    #endregion

    #region Project Memory

    /// <summary>
    /// Represents the project memory that persists across requests
    /// </summary>
    public class ProjectMemory
    {
        public string ProjectName { get; set; } = "";
        public string TechStack { get; set; } = "";
        public List<string> Constraints { get; set; } = new();
        public List<string> TeamPreferences { get; set; } = new();
        public List<FeatureRecord> PreviousFeatures { get; set; } = new();
        public Dictionary<string, string> Decisions { get; set; } = new();

        public record FeatureRecord(string Title, string Priority, string Effort, DateTime CreatedAt);

        public string ToContextString()
        {
            var sb = new System.Text.StringBuilder();
            
            if (!string.IsNullOrEmpty(ProjectName))
                sb.AppendLine($"📦 Project: {ProjectName}");
            
            if (!string.IsNullOrEmpty(TechStack))
                sb.AppendLine($"🔧 Tech Stack: {TechStack}");
            
            if (Constraints.Any())
            {
                sb.AppendLine("⚠️ Constraints:");
                foreach (var c in Constraints)
                    sb.AppendLine($"   - {c}");
            }
            
            if (TeamPreferences.Any())
            {
                sb.AppendLine("💡 Team Preferences:");
                foreach (var p in TeamPreferences)
                    sb.AppendLine($"   - {p}");
            }
            
            if (PreviousFeatures.Any())
            {
                sb.AppendLine("📋 Previous Features:");
                foreach (var f in PreviousFeatures.TakeLast(5))
                    sb.AppendLine($"   - {f.Title} (Priority: {f.Priority}, Effort: {f.Effort})");
            }
            
            if (Decisions.Any())
            {
                sb.AppendLine("✅ Key Decisions:");
                foreach (var d in Decisions.TakeLast(5))
                    sb.AppendLine($"   - {d.Key}: {d.Value}");
            }

            return sb.ToString();
        }
    }

    #endregion

    #region Sample Data

    /// <summary>
    /// Creates the default project memory for demos
    /// </summary>
    public static ProjectMemory CreateDefaultProjectMemory() => new()
    {
        ProjectName = "TaskFlow - Team Collaboration App",
        TechStack = ".NET 9, Blazor WebAssembly, Azure SQL, SignalR",
        Constraints = new List<string>
        {
            "Must support offline mode",
            "WCAG 2.1 AA accessibility compliance required",
            "Maximum initial load time: 3 seconds",
            "Must integrate with existing Azure AD"
        },
        TeamPreferences = new List<string>
        {
            "Prefer component-based architecture",
            "Use Tailwind CSS for styling",
            "Follow vertical slice architecture",
            "Minimize third-party dependencies"
        },
        Decisions = new Dictionary<string, string>
        {
            { "State Management", "Fluxor for client-side state" },
            { "API Style", "REST with OpenAPI specification" },
            { "Testing Strategy", "Unit + Integration + E2E with Playwright" }
        }
    };

    /// <summary>
    /// Sample feature ideas for demonstration
    /// </summary>
    public static readonly string[] SampleFeatureIdeas = new[]
    {
        "Users need to share tasks with team members",
        "Add real-time notifications when tasks are assigned or completed",
        "We need a dashboard showing team productivity metrics"
    };

    /// <summary>
    /// Formats a feature request with project context
    /// </summary>
    public static string FormatPromptWithContext(string featureRequest, ProjectMemory memory)
    {
        return $"""
            Current Project Context:
            {memory.ToContextString()}

            New Feature Request:
            {featureRequest}

            Please create a feature specification that aligns with the project context above.
            """;
    }

    #endregion

    #region Agent Factory

    /// <summary>
    /// Creates the Feature Planning Copilot with Memory agent
    /// </summary>
    public static AIAgent CreateAgent()
    {
        var azureClient = new AzureOpenAIClient(AIConfig.Endpoint, AIConfig.KeyCredential);
        var chatClient = azureClient
            .GetChatClient(AIConfig.ModelDeployment)
            .AsIChatClient();

        return CreateAgent(chatClient);
    }

    /// <summary>
    /// Creates the agent with a custom chat client
    /// </summary>
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

    private static ProjectMemory _memory = new();

    public static async Task Execute()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        Console.WriteLine("\n═══════════════════════════════════════════════════════════════════════════════");
        Console.WriteLine("   CHAPTER 2 - SAMPLE 02: FEATURE PLANNING COPILOT WITH MEMORY");
        Console.WriteLine("═══════════════════════════════════════════════════════════════════════════════\n");

        // Initialize project memory with context
        _memory = CreateDefaultProjectMemory();

        Console.WriteLine("📦 Project Memory Initialized:\n");
        Console.WriteLine(_memory.ToContextString());
        Console.WriteLine(new string('═', 80) + "\n");

        // Create agent using factory method
        AIAgent agent = CreateAgent();
        AgentThread thread = agent.GetNewThread();

        Console.WriteLine("🤖 Feature Planning Copilot with Memory is ready!\n");

        foreach (var feature in SampleFeatureIdeas)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"📝 INPUT: {feature}");
            Console.ResetColor();
            Console.WriteLine(new string('-', 80));

            try
            {
                string contextualPrompt = FormatPromptWithContext(feature, _memory);
                var response = await agent.RunAsync(contextualPrompt, thread);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\n🎯 STRUCTURED OUTPUT:");
                Console.ResetColor();
                Console.WriteLine(response.Text);

                // Store in memory
                var featureTitle = ExtractFeatureTitle(feature);
                _memory.PreviousFeatures.Add(new ProjectMemory.FeatureRecord(
                    featureTitle, "Medium", "M", DateTime.Now));
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Error: {ex.Message}");
                Console.ResetColor();
            }

            Console.WriteLine("\n" + new string('═', 80) + "\n");
        }

        Console.WriteLine("📊 Final Project Memory State:");
        Console.WriteLine(_memory.ToContextString());
        Console.WriteLine("\n✅ Sample Complete!");
    }

    public static async Task ExecuteInteractive()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        Console.WriteLine("\n═══════════════════════════════════════════════════════════════════════════════");
        Console.WriteLine("   CHAPTER 2 - SAMPLE 02: FEATURE PLANNING COPILOT WITH MEMORY (INTERACTIVE)");
        Console.WriteLine("═══════════════════════════════════════════════════════════════════════════════\n");

        Console.WriteLine("📦 Let's set up your project context first.\n");

        Console.Write("Project Name: ");
        _memory.ProjectName = Console.ReadLine() ?? "My Project";

        Console.Write("Tech Stack (e.g., '.NET 9, React, PostgreSQL'): ");
        _memory.TechStack = Console.ReadLine() ?? "";

        Console.WriteLine("\nEnter constraints (one per line, empty line to finish):");
        while (true)
        {
            Console.Write("  Constraint: ");
            var constraint = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(constraint)) break;
            _memory.Constraints.Add(constraint);
        }

        Console.WriteLine("\n📊 Project Context Set:");
        Console.WriteLine(_memory.ToContextString());
        Console.WriteLine(new string('═', 80) + "\n");

        AIAgent agent = CreateAgent();
        AgentThread thread = agent.GetNewThread();

        Console.WriteLine("🤖 Feature Planning Copilot with Memory is ready!\n");
        Console.WriteLine("Commands: 'memory' - Show memory, 'q' - Exit\n");

        while (true)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("Your Feature Idea: ");
            Console.ResetColor();

            string? input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input)) continue;

            if (input.Equals("q", StringComparison.OrdinalIgnoreCase) ||
                input.Equals("quit", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("\n👋 Goodbye!");
                break;
            }

            if (input.Equals("memory", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("\n📊 Current Project Memory:");
                Console.WriteLine(_memory.ToContextString());
                continue;
            }

            Console.WriteLine();

            try
            {
                string contextualPrompt = FormatPromptWithContext(input, _memory);
                var response = await agent.RunAsync(contextualPrompt, thread);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("📋 Copilot: ");
                Console.ResetColor();
                Console.WriteLine(response.Text);

                _memory.PreviousFeatures.Add(new ProjectMemory.FeatureRecord(
                    ExtractFeatureTitle(input), "Medium", "M", DateTime.Now));
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Error: {ex.Message}");
                Console.ResetColor();
            }

            Console.WriteLine();
        }

        Console.WriteLine("\n✅ Interactive session complete.");
    }

    /// <summary>
    /// Runs the agent in DevUI mode - a web-based interactive interface
    /// Open http://localhost:5000/devui in your browser
    /// </summary>
    public static void ExecuteWithDevUI()
    {
        Console.WriteLine("\n═══════════════════════════════════════════════════════════════════════════════");
        Console.WriteLine("   CHAPTER 2 - SAMPLE 02: FEATURE PLANNING WITH MEMORY (DEVUI MODE)");
        Console.WriteLine("═══════════════════════════════════════════════════════════════════════════════\n");

        DevUIHelper.RunWithDevUI(AgentName, AgentInstructions);
    }

    private static string ExtractFeatureTitle(string input)
    {
        var words = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length <= 5) return input;
        return string.Join(' ', words.Take(5)) + "...";
    }

    #endregion
}
