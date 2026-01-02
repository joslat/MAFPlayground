// SPDX-License-Identifier: LicenseRef-MAFPlayground-NPU-1.0-CH
// Copyright (c) 2025 Jose Luis Latorre

using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CourseSamples;

/// <summary>
/// Chapter 2 - Sample 03: Adding Power: Giving Tools to your Feature Planning Agent
/// 
/// This sample builds on Sample 02 by adding two local tools:
/// - StoryHealthCheckTool: Analyzes user stories for quality and completeness
/// - FeatureMetadataTool: Suggests components, tags, and priority based on the feature
/// 
/// The agent can now call these tools, react to their structured results, and 
/// produce enhanced feature specifications.
/// </summary>
public static class Chapter2_Sample03_FeaturePlanningWithTools
{
    #region Agent Configuration

    /// <summary>
    /// The name of the agent
    /// </summary>
    public const string AgentName = "FeaturePlanningCopilotWithTools";

    /// <summary>
    /// The system instructions for the Feature Planning Copilot with Tools
    /// </summary>
    public const string AgentInstructions = """
        You are the Feature Planning Copilot 📋 with Tools - an expert product manager 
        and software architect who transforms rough feature ideas into well-structured 
        feature specifications.

        You have access to TWO powerful tools:

        1. **StoryHealthCheckTool**: Call this FIRST to analyze the quality of the user's 
           feature request. It returns a health score and specific improvement suggestions.
           Always call this before creating the specification.

        2. **FeatureMetadataTool**: Call this to get intelligent suggestions for:
           - Which components/modules should own this feature
           - Relevant tags for categorization
           - Recommended priority based on keywords
           - Estimated complexity

        WORKFLOW:
        1. When you receive a feature request, FIRST call StoryHealthCheckTool
        2. If the health score is below 60, ask clarifying questions based on the suggestions
        3. Call FeatureMetadataTool to get component/tag/priority suggestions
        4. Create the feature specification incorporating tool results

        Output format should include the tool analysis:

        ## 🔍 Story Analysis
        [Show health check results and how you addressed any issues]

        ## 🏷️ Suggested Metadata
        [Show the metadata suggestions from the tool]

        ## 📋 Feature Specification
        [Full specification as before, enhanced by tool insights]
        
        Always use the tools to provide data-driven, high-quality specifications!
        """;

    /// <summary>
    /// Sample feature ideas for testing - varying quality levels
    /// </summary>
    public static readonly string[] SampleFeatureIdeas = new[]
    {
        // Low quality - vague
        "dark mode",
        // Medium quality - some detail
        "Users should be able to export their task lists to PDF for offline viewing",
        // High quality - well-defined
        "As a team lead, I want to see a real-time dashboard showing my team's task completion rates, active blockers, and workload distribution so I can identify bottlenecks and rebalance work effectively"
    };

    #endregion

    #region Agent Factory

    /// <summary>
    /// Creates a Feature Planning Copilot agent with tools using the default Azure OpenAI configuration
    /// </summary>
    /// <returns>A configured AIAgent with StoryHealthCheck and FeatureMetadata tools</returns>
    public static AIAgent CreateAgent()
    {
        var azureClient = new AzureOpenAIClient(AIConfig.Endpoint, AIConfig.KeyCredential);
        var chatClient = azureClient
            .GetChatClient(AIConfig.ModelDeployment)
            .AsIChatClient();

        return CreateAgent(chatClient);
    }

    /// <summary>
    /// Creates a Feature Planning Copilot agent with tools using a provided chat client
    /// </summary>
    /// <param name="chatClient">The chat client to use</param>
    /// <returns>A configured AIAgent with StoryHealthCheck and FeatureMetadata tools</returns>
    public static AIAgent CreateAgent(IChatClient chatClient)
    {
        return new ChatClientAgent(
            chatClient,
            new ChatClientAgentOptions
            {
                Name = AgentName,
                Instructions = AgentInstructions,
                ChatOptions = new ChatOptions
                {
                    Tools = GetToolsList()
                }
            });
    }

    /// <summary>
    /// Gets the list of tools available to the Feature Planning Copilot
    /// </summary>
    /// <returns>List of AI tools</returns>
    public static List<AITool> GetToolsList()
    {
        return new List<AITool>
        {
            AIFunctionFactory.Create(FeaturePlanningTools.StoryHealthCheckTool),
            AIFunctionFactory.Create(FeaturePlanningTools.FeatureMetadataTool)
        };
    }

    #endregion

    #region Sample Data

    /// <summary>
    /// Creates a default project memory for demos
    /// </summary>
    public static Chapter2_Sample02_FeaturePlanningWithMemory.ProjectMemory CreateDefaultProjectMemory()
    {
        return new Chapter2_Sample02_FeaturePlanningWithMemory.ProjectMemory
        {
            ProjectName = "TaskFlow - Team Collaboration App",
            TechStack = ".NET 9, Blazor WebAssembly, Azure SQL, SignalR",
            Constraints = new List<string>
            {
                "Must support offline mode",
                "WCAG 2.1 AA accessibility compliance required",
                "Maximum initial load time: 3 seconds"
            },
            TeamPreferences = new List<string>
            {
                "Prefer component-based architecture",
                "Use Tailwind CSS for styling",
                "Follow vertical slice architecture"
            }
        };
    }

    /// <summary>
    /// Formats a feature request with project context
    /// </summary>
    public static string FormatPromptWithContext(string featureRequest, Chapter2_Sample02_FeaturePlanningWithMemory.ProjectMemory memory)
    {
        return $"""
            Project Context:
            {memory.ToContextString()}

            Feature Request:
            {featureRequest}

            Use your tools to analyze this request and create an enhanced specification.
            """;
    }

    #endregion

    #region Execute Methods

    public static async Task Execute()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        Console.WriteLine("\n═══════════════════════════════════════════════════════════════════════════════");
        Console.WriteLine("   CHAPTER 2 - SAMPLE 03: FEATURE PLANNING COPILOT WITH TOOLS");
        Console.WriteLine("═══════════════════════════════════════════════════════════════════════════════\n");

        // Initialize project memory
        var memory = CreateDefaultProjectMemory();

        Console.WriteLine("📦 Project Context:");
        Console.WriteLine(memory.ToContextString());
        Console.WriteLine(new string('═', 80) + "\n");

        // Create agent using factory method
        AIAgent featurePlanningCopilot = CreateAgent();
        AgentThread thread = featurePlanningCopilot.GetNewThread();

        Console.WriteLine("🤖 Feature Planning Copilot with Tools is ready!\n");
        Console.WriteLine("🔧 Available Tools:");
        Console.WriteLine("   • StoryHealthCheckTool - Analyzes story quality");
        Console.WriteLine("   • FeatureMetadataTool - Suggests components, tags, priority\n");
        Console.WriteLine(new string('═', 80) + "\n");

        // Test with different quality inputs
        foreach (var feature in SampleFeatureIdeas)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"📝 INPUT: {feature}");
            Console.ResetColor();
            Console.WriteLine(new string('-', 80));

            try
            {
                string contextualPrompt = FormatPromptWithContext(feature, memory);

                var response = await featurePlanningCopilot.RunAsync(contextualPrompt, thread);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\n🎯 ENHANCED OUTPUT (with tool insights):");
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
        Console.WriteLine("💡 Notice how the tools provide structured analysis and metadata suggestions.");
    }

    /// <summary>
    /// Interactive mode with tools
    /// </summary>
    public static async Task ExecuteInteractive()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        Console.WriteLine("\n═══════════════════════════════════════════════════════════════════════════════");
        Console.WriteLine("   CHAPTER 2 - SAMPLE 03: FEATURE PLANNING COPILOT WITH TOOLS (INTERACTIVE)");
        Console.WriteLine("═══════════════════════════════════════════════════════════════════════════════\n");

        // Create agent using factory method
        AIAgent featurePlanningCopilot = CreateAgent();
        AgentThread thread = featurePlanningCopilot.GetNewThread();

        Console.WriteLine("🤖 Feature Planning Copilot with Tools is ready!\n");
        Console.WriteLine("🔧 Tools: StoryHealthCheckTool, FeatureMetadataTool");
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
        Console.WriteLine("   CHAPTER 2 - SAMPLE 03: FEATURE PLANNING WITH TOOLS (DEVUI MODE)");
        Console.WriteLine("═══════════════════════════════════════════════════════════════════════════════\n");

        var agentSpec = new AgentSpec(AgentName, AgentInstructions, GetToolsList());
        DevUIHelper.RunWithDevUI(agentSpec);
    }

    #endregion
}

/// <summary>
/// Tools for the Feature Planning Copilot
/// </summary>
public static class FeaturePlanningTools
{
    /// <summary>
    /// Analyzes a user story or feature request for quality and completeness.
    /// Returns a health score and improvement suggestions.
    /// </summary>
    [Description("Analyzes a user story or feature request for quality and completeness. Returns health score (0-100) and improvement suggestions.")]
    public static StoryHealthResult StoryHealthCheckTool(
        [Description("The user story or feature request to analyze")] string storyText)
    {
        Console.WriteLine($"\n🔧 Tool called: StoryHealthCheckTool(\"{TruncateForDisplay(storyText)}\")");

        var result = new StoryHealthResult();
        var suggestions = new List<string>();

        // Check for user story format
        bool hasUserStoryFormat = storyText.Contains("As a", StringComparison.OrdinalIgnoreCase) &&
                                  storyText.Contains("I want", StringComparison.OrdinalIgnoreCase);
        
        bool hasBenefit = storyText.Contains("so that", StringComparison.OrdinalIgnoreCase) ||
                          storyText.Contains("because", StringComparison.OrdinalIgnoreCase);

        // Length analysis
        var wordCount = storyText.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        
        // Calculate health score
        int score = 50; // Base score

        if (hasUserStoryFormat)
        {
            score += 20;
            result.HasUserStoryFormat = true;
        }
        else
        {
            suggestions.Add("Consider using 'As a [user], I want [goal] so that [benefit]' format");
        }

        if (hasBenefit)
        {
            score += 15;
            result.HasClearBenefit = true;
        }
        else
        {
            suggestions.Add("Add the business value: why does the user need this?");
        }

        if (wordCount >= 10)
        {
            score += 10;
            result.HasSufficientDetail = true;
        }
        else
        {
            suggestions.Add("Add more detail about the expected behavior");
        }

        // Check for specificity
        var specificityKeywords = new[] { "when", "should", "must", "display", "allow", "enable", "show", "create", "update", "delete" };
        var hasSpecificity = specificityKeywords.Any(k => storyText.Contains(k, StringComparison.OrdinalIgnoreCase));
        
        if (hasSpecificity)
        {
            score += 10;
            result.HasActionableLanguage = true;
        }
        else
        {
            suggestions.Add("Use specific action verbs (show, create, enable, display, etc.)");
        }

        // Check for acceptance criteria hints
        var hasCriteriaHints = storyText.Contains("accept", StringComparison.OrdinalIgnoreCase) ||
                               storyText.Contains("criteria", StringComparison.OrdinalIgnoreCase) ||
                               storyText.Contains("requirement", StringComparison.OrdinalIgnoreCase);
        if (hasCriteriaHints)
        {
            score += 5;
        }

        result.HealthScore = Math.Min(100, score);
        result.ImprovementSuggestions = suggestions;
        result.QualityLevel = result.HealthScore >= 80 ? "High" :
                              result.HealthScore >= 60 ? "Medium" : "Low";

        Console.WriteLine($"   → Health Score: {result.HealthScore}/100 ({result.QualityLevel} quality)");
        
        return result;
    }

    /// <summary>
    /// Suggests components, tags, and priority based on the feature description.
    /// </summary>
    [Description("Analyzes a feature and suggests components, tags, priority, and complexity estimation.")]
    public static FeatureMetadataResult FeatureMetadataTool(
        [Description("The feature title or description to analyze")] string featureDescription,
        [Description("The project's tech stack (optional, for better suggestions)")] string? techStack = null)
    {
        Console.WriteLine($"\n🔧 Tool called: FeatureMetadataTool(\"{TruncateForDisplay(featureDescription)}\")");

        var result = new FeatureMetadataResult();
        var description = featureDescription.ToLowerInvariant();

        // Component detection based on keywords
        var componentMapping = new Dictionary<string[], string>
        {
            { new[] { "dashboard", "chart", "graph", "analytics", "metric", "report" }, "Analytics" },
            { new[] { "notification", "alert", "email", "message", "notify" }, "Notifications" },
            { new[] { "user", "profile", "account", "settings", "preference" }, "UserManagement" },
            { new[] { "task", "todo", "assignment", "work item", "ticket" }, "TaskManagement" },
            { new[] { "export", "import", "pdf", "csv", "download", "upload" }, "DataExchange" },
            { new[] { "search", "filter", "find", "query" }, "Search" },
            { new[] { "auth", "login", "password", "security", "permission", "role" }, "Security" },
            { new[] { "api", "integration", "webhook", "sync" }, "Integration" },
            { new[] { "ui", "theme", "dark mode", "light mode", "style", "appearance" }, "UI/UX" },
            { new[] { "real-time", "live", "instant", "signalr", "websocket" }, "RealTime" },
            { new[] { "offline", "cache", "local storage", "sync" }, "OfflineSupport" },
            { new[] { "team", "collaborate", "share", "invite", "member" }, "Collaboration" }
        };

        foreach (var mapping in componentMapping)
        {
            if (mapping.Key.Any(k => description.Contains(k)))
            {
                result.SuggestedComponents.Add(mapping.Value);
            }
        }

        // Tag detection
        var tagMapping = new Dictionary<string[], string>
        {
            { new[] { "security", "auth", "password", "permission" }, "security" },
            { new[] { "performance", "fast", "speed", "optimize" }, "performance" },
            { new[] { "accessibility", "a11y", "wcag", "screen reader" }, "accessibility" },
            { new[] { "mobile", "responsive", "tablet", "phone" }, "mobile" },
            { new[] { "api", "rest", "graphql", "endpoint" }, "api" },
            { new[] { "ux", "ui", "design", "user experience" }, "ux-improvement" },
            { new[] { "bug", "fix", "issue", "broken" }, "bug-fix" },
            { new[] { "new", "feature", "add", "create" }, "new-feature" },
            { new[] { "refactor", "clean", "improve", "technical debt" }, "tech-debt" }
        };

        foreach (var mapping in tagMapping)
        {
            if (mapping.Key.Any(k => description.Contains(k)))
            {
                result.SuggestedTags.Add(mapping.Value);
            }
        }

        // Priority detection
        var highPriorityKeywords = new[] { "critical", "urgent", "asap", "blocker", "security", "crash", "broken" };
        var lowPriorityKeywords = new[] { "nice to have", "maybe", "could", "minor", "cosmetic", "eventually" };

        if (highPriorityKeywords.Any(k => description.Contains(k)))
        {
            result.SuggestedPriority = "High";
            result.PriorityReason = "Contains high-priority keywords indicating urgency";
        }
        else if (lowPriorityKeywords.Any(k => description.Contains(k)))
        {
            result.SuggestedPriority = "Low";
            result.PriorityReason = "Contains low-priority keywords; consider for future sprints";
        }
        else
        {
            result.SuggestedPriority = "Medium";
            result.PriorityReason = "Standard feature request without urgency indicators";
        }

        // Complexity estimation
        var complexityFactors = 0;
        if (description.Contains("integration") || description.Contains("api")) complexityFactors++;
        if (description.Contains("real-time") || description.Contains("live")) complexityFactors++;
        if (description.Contains("security") || description.Contains("auth")) complexityFactors++;
        if (description.Contains("offline") || description.Contains("sync")) complexityFactors++;
        if (description.Contains("migrate") || description.Contains("refactor")) complexityFactors++;
        if (result.SuggestedComponents.Count > 2) complexityFactors++;

        result.EstimatedComplexity = complexityFactors switch
        {
            0 => "XS",
            1 => "S",
            2 => "M",
            3 => "L",
            _ => "XL"
        };

        result.ComplexityReason = $"Based on {complexityFactors} complexity factors and {result.SuggestedComponents.Count} affected components";

        // Default values if nothing detected
        if (!result.SuggestedComponents.Any())
            result.SuggestedComponents.Add("Core");
        if (!result.SuggestedTags.Any())
            result.SuggestedTags.Add("new-feature");

        Console.WriteLine($"   → Components: [{string.Join(", ", result.SuggestedComponents)}]");
        Console.WriteLine($"   → Priority: {result.SuggestedPriority}, Complexity: {result.EstimatedComplexity}");

        return result;
    }

    private static string TruncateForDisplay(string text, int maxLength = 50)
    {
        if (text.Length <= maxLength) return text;
        return text[..(maxLength - 3)] + "...";
    }
}

/// <summary>
/// Result from the StoryHealthCheckTool
/// </summary>
public class StoryHealthResult
{
    [JsonPropertyName("healthScore")]
    [Description("Quality score from 0-100")]
    public int HealthScore { get; set; }

    [JsonPropertyName("qualityLevel")]
    [Description("Quality level: High, Medium, or Low")]
    public string QualityLevel { get; set; } = "Low";

    [JsonPropertyName("hasUserStoryFormat")]
    [Description("Whether the story follows 'As a... I want... so that...' format")]
    public bool HasUserStoryFormat { get; set; }

    [JsonPropertyName("hasClearBenefit")]
    [Description("Whether the story explains the business value")]
    public bool HasClearBenefit { get; set; }

    [JsonPropertyName("hasSufficientDetail")]
    [Description("Whether the story has enough detail")]
    public bool HasSufficientDetail { get; set; }

    [JsonPropertyName("hasActionableLanguage")]
    [Description("Whether the story uses specific action verbs")]
    public bool HasActionableLanguage { get; set; }

    [JsonPropertyName("improvementSuggestions")]
    [Description("List of suggestions to improve the story")]
    public List<string> ImprovementSuggestions { get; set; } = new();
}

/// <summary>
/// Result from the FeatureMetadataTool
/// </summary>
public class FeatureMetadataResult
{
    [JsonPropertyName("suggestedComponents")]
    [Description("List of components/modules that should own this feature")]
    public List<string> SuggestedComponents { get; set; } = new();

    [JsonPropertyName("suggestedTags")]
    [Description("Relevant tags for categorization")]
    public List<string> SuggestedTags { get; set; } = new();

    [JsonPropertyName("suggestedPriority")]
    [Description("Recommended priority: High, Medium, or Low")]
    public string SuggestedPriority { get; set; } = "Medium";

    [JsonPropertyName("priorityReason")]
    [Description("Explanation for the priority recommendation")]
    public string PriorityReason { get; set; } = "";

    [JsonPropertyName("estimatedComplexity")]
    [Description("T-Shirt size complexity: XS, S, M, L, XL")]
    public string EstimatedComplexity { get; set; } = "M";

    [JsonPropertyName("complexityReason")]
    [Description("Explanation for the complexity estimation")]
    public string ComplexityReason { get; set; } = "";
}
