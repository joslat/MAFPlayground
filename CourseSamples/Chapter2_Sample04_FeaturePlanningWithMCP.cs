// SPDX-License-Identifier: LicenseRef-MAFPlayground-NPU-1.0-CH
// Copyright (c) 2025 Jose Luis Latorre

using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;

namespace CourseSamples;

/// <summary>
/// Chapter 2 - Sample 04: Going MCP: Integrate an MCP Tool
/// 
/// This sample builds on Sample 03 by connecting to GitHub's official MCP server.
/// The Feature Planning Copilot can now:
/// - Turn refined feature specs into real GitHub issues
/// - Search existing issues to avoid duplicates
/// - Link related issues together
/// 
/// Run this in DevUI to see the MCP calls and created issues in your demo repository.
/// 
/// Prerequisites:
/// - Node.js installed (for npx)
/// - GITHUB_PERSONAL_ACCESS_TOKEN environment variable set
/// - A GitHub repository to create issues in
/// </summary>
public static class Chapter2_Sample04_FeaturePlanningWithMCP
{
    #region Agent Configuration

    /// <summary>
    /// Default repository for demo purposes - change this to your repo
    /// </summary>
    public const string DefaultRepository = "joslat/MAFPlayground";

    /// <summary>
    /// The name of the agent
    /// </summary>
    public const string AgentName = "FeaturePlanningCopilotWithMCP";

    /// <summary>
    /// Gets the system instructions for the Feature Planning Copilot with MCP
    /// </summary>
    public static string GetAgentInstructions(string repository = DefaultRepository)
    {
        return $"""
            You are the Feature Planning Copilot 📋 with GitHub Integration - an expert product manager 
            and software architect who can create real GitHub issues from feature specifications.

            You have access to:

            LOCAL TOOLS:
            1. **StoryHealthCheckTool**: Analyze the quality of feature requests
            2. **FeatureMetadataTool**: Get component/tag/priority suggestions

            GITHUB MCP TOOLS (if available):
            - create_issue: Create GitHub issues from feature specs
            - search_issues: Search for existing issues to avoid duplicates
            - get_issue: Get details of a specific issue
            - list_issues: List issues in a repository
            - And more...

            DEFAULT REPOSITORY: {repository}
            (Use this repo unless the user specifies a different one)

            WORKFLOW:
            1. Analyze the feature request with StoryHealthCheckTool
            2. Get metadata suggestions with FeatureMetadataTool
            3. Create the feature specification
            4. If the user asks to create an issue:
               a. First search for similar existing issues
               b. If no duplicates found, create the GitHub issue
               c. Use labels based on the suggested tags
               d. Include the full specification in the issue body

            ISSUE BODY FORMAT:
            When creating GitHub issues, format the body as:
            
            ## Summary
            [Feature summary]
            
            ## User Story
            [User story]
            
            ## Acceptance Criteria
            - [ ] Criterion 1
            - [ ] Criterion 2
            
            ## Technical Considerations
            - [Technical notes]
            
            ## Metadata
            - Priority: [priority]
            - Complexity: [complexity]
            - Components: [components]

            Always confirm with the user before creating GitHub issues.
            """;
    }

    /// <summary>
    /// Sample feature ideas for testing MCP integration
    /// </summary>
    public static readonly string[] SampleFeatureIdeas = new[]
    {
        "As a team lead, I want to see a real-time dashboard showing my team's task completion rates and active blockers so I can identify bottlenecks and rebalance work effectively",
        "Add real-time notifications when tasks are assigned",
        "Users should be able to link related tasks together with dependency tracking"
    };

    #endregion

    #region Agent Factory

    /// <summary>
    /// Creates a Feature Planning Copilot agent with MCP integration using the default Azure OpenAI configuration.
    /// Note: This version runs synchronously and creates MCP connection. For async version with MCP, use CreateAgentWithMCPAsync.
    /// </summary>
    /// <returns>A configured AIAgent with local tools only (no MCP)</returns>
    public static AIAgent CreateAgent()
    {
        var azureClient = new AzureOpenAIClient(AIConfig.Endpoint, AIConfig.KeyCredential);
        var chatClient = azureClient
            .GetChatClient(AIConfig.ModelDeployment)
            .AsIChatClient();

        return CreateAgentWithLocalToolsOnly(chatClient);
    }

    /// <summary>
    /// Creates a Feature Planning Copilot agent with local tools only (no MCP)
    /// </summary>
    public static AIAgent CreateAgentWithLocalToolsOnly(IChatClient chatClient)
    {
        return new ChatClientAgent(
            chatClient,
            new ChatClientAgentOptions
            {
                Name = AgentName,
                Instructions = GetAgentInstructions(),
                ChatOptions = new ChatOptions
                {
                    Tools = Chapter2_Sample03_FeaturePlanningWithTools.GetToolsList()
                }
            });
    }

    /// <summary>
    /// Creates a Feature Planning Copilot agent with simulated MCP (for when GitHub token is not available)
    /// </summary>
    public static AIAgent CreateAgentWithSimulatedMCP(IChatClient chatClient)
    {
        var tools = new List<AITool>(Chapter2_Sample03_FeaturePlanningWithTools.GetToolsList())
        {
            AIFunctionFactory.Create(SimulateCreateGitHubIssue)
        };

        return new ChatClientAgent(
            chatClient,
            new ChatClientAgentOptions
            {
                Name = "FeaturePlanningCopilotSimulated",
                Instructions = """
                    You are the Feature Planning Copilot (Simulation Mode).
                    
                    GitHub MCP is not available, so you cannot create real issues.
                    However, you can still:
                    1. Analyze feature requests with StoryHealthCheckTool
                    2. Get metadata with FeatureMetadataTool
                    3. Create feature specifications
                    4. Show what the GitHub issue WOULD look like if MCP were connected

                    When asked to create an issue, use the SimulateCreateGitHubIssue tool to show a preview.
                    """,
                ChatOptions = new ChatOptions
                {
                    Tools = tools
                }
            });
    }

    /// <summary>
    /// Creates a Feature Planning Copilot agent with full MCP integration.
    /// Requires GITHUB_PERSONAL_ACCESS_TOKEN environment variable.
    /// </summary>
    /// <param name="chatClient">The chat client to use</param>
    /// <param name="mcpClient">An already-connected MCP client</param>
    /// <param name="mcpTools">Tools retrieved from the MCP server</param>
    /// <returns>A configured AIAgent with local tools + MCP tools</returns>
    public static AIAgent CreateAgentWithMCP(IChatClient chatClient, IReadOnlyList<AITool> mcpTools)
    {
        var allTools = new List<AITool>(Chapter2_Sample03_FeaturePlanningWithTools.GetToolsList());
        allTools.AddRange(mcpTools);

        return new ChatClientAgent(
            chatClient,
            new ChatClientAgentOptions
            {
                Name = AgentName,
                Instructions = GetAgentInstructions(),
                ChatOptions = new ChatOptions
                {
                    Tools = allTools
                }
            });
    }

    /// <summary>
    /// Creates and connects an MCP client to the GitHub server
    /// </summary>
    /// <returns>A connected McpClient, or null if connection failed</returns>
    public static async Task<McpClient?> CreateMCPClientAsync()
    {
        var githubToken = Environment.GetEnvironmentVariable("GITHUB_PERSONAL_ACCESS_TOKEN");
        if (string.IsNullOrEmpty(githubToken))
        {
            return null;
        }

        try
        {
            return await McpClient.CreateAsync(new StdioClientTransport(new()
            {
                Name = "GitHubMCPServer",
                Command = "npx",
                Arguments = new[] { "-y", "@modelcontextprotocol/server-github" },
                EnvironmentVariables = new Dictionary<string, string?>
                {
                    { "GITHUB_PERSONAL_ACCESS_TOKEN", githubToken }
                }
            }));
        }
        catch
        {
            return null;
        }
    }

    #endregion

    #region Sample Data

    /// <summary>
    /// Formats a prompt for demonstrations
    /// </summary>
    public static string FormatPromptWithMCPContext(string featureRequest, bool askForIssue = false)
    {
        var memory = Chapter2_Sample03_FeaturePlanningWithTools.CreateDefaultProjectMemory();
        
        var basePrompt = Chapter2_Sample03_FeaturePlanningWithTools.FormatPromptWithContext(featureRequest, memory);
        
        if (askForIssue)
        {
            return basePrompt + "\n\nAfter creating the specification, please show what a GitHub issue would look like.";
        }
        
        return basePrompt;
    }

    #endregion

    #region Simulation Tool

    [System.ComponentModel.Description("Simulates creating a GitHub issue (for demo when MCP is not available)")]
    public static string SimulateCreateGitHubIssue(
        [System.ComponentModel.Description("Issue title")] string title,
        [System.ComponentModel.Description("Issue body/description")] string body,
        [System.ComponentModel.Description("Repository in format owner/repo")] string repository = DefaultRepository)
    {
        Console.WriteLine($"\n🔧 [SIMULATION] Would create GitHub issue in {repository}:");
        Console.WriteLine($"   Title: {title}");
        
        return $"""
            ✅ [SIMULATED] GitHub Issue Created:
            
            Repository: {repository}
            Issue #: 42 (simulated)
            Title: {title}
            URL: https://github.com/{repository}/issues/42 (simulated)
            
            ⚠️ This is a simulation. Set GITHUB_PERSONAL_ACCESS_TOKEN to create real issues.
            """;
    }

    #endregion

    #region Execute Methods

    public static async Task Execute()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        Console.WriteLine("\n═══════════════════════════════════════════════════════════════════════════════");
        Console.WriteLine("   CHAPTER 2 - SAMPLE 04: FEATURE PLANNING COPILOT WITH MCP (GITHUB)");
        Console.WriteLine("═══════════════════════════════════════════════════════════════════════════════\n");

        // Check for GitHub token
        var githubToken = Environment.GetEnvironmentVariable("GITHUB_PERSONAL_ACCESS_TOKEN");
        if (string.IsNullOrEmpty(githubToken))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("⚠️  GITHUB_PERSONAL_ACCESS_TOKEN not set.");
            Console.WriteLine("   Set this environment variable to enable GitHub issue creation.");
            Console.WriteLine("   For now, running in simulation mode (MCP tools won't be available).\n");
            Console.ResetColor();
            
            // Fall back to simulation mode
            await ExecuteWithoutMCP();
            return;
        }

        // Initialize project memory
        var memory = Chapter2_Sample03_FeaturePlanningWithTools.CreateDefaultProjectMemory();

        Console.WriteLine("📦 Project Context:");
        Console.WriteLine(memory.ToContextString());
        Console.WriteLine(new string('═', 80) + "\n");

        Console.WriteLine("🔌 Connecting to GitHub MCP Server...");

        McpClient? mcpClient = null;
        AIAgent featurePlanningCopilot;

        // Create the Azure OpenAI chat client
        var azureClient = new AzureOpenAIClient(AIConfig.Endpoint, AIConfig.KeyCredential);
        var chatClient = azureClient
            .GetChatClient(AIConfig.ModelDeployment)
            .AsIChatClient();

        try
        {
            // Try to create MCP client
            mcpClient = await CreateMCPClientAsync();

            if (mcpClient != null)
            {
                Console.WriteLine("✅ Connected to GitHub MCP Server!\n");

                // Get available tools from MCP
                Console.WriteLine("🔍 Discovering GitHub tools...");
                var mcpTools = await mcpClient.ListToolsAsync();
                Console.WriteLine($"✅ Found {mcpTools.Count} GitHub tools!\n");

                Console.WriteLine("🔧 Available Tools:");
                Console.WriteLine("   Local Tools:");
                Console.WriteLine("   • StoryHealthCheckTool - Analyzes story quality");
                Console.WriteLine("   • FeatureMetadataTool - Suggests components, tags, priority");
                Console.WriteLine("   GitHub MCP Tools:");
                foreach (var tool in mcpTools.Take(5))
                {
                    Console.WriteLine($"   • {tool.Name}");
                }
                if (mcpTools.Count > 5)
                {
                    Console.WriteLine($"   • ... and {mcpTools.Count - 5} more");
                }
                Console.WriteLine();

                // Create agent with MCP tools using factory method
                featurePlanningCopilot = CreateAgentWithMCP(chatClient, mcpTools.Cast<AITool>().ToList());
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("⚠️  Could not connect to GitHub MCP.");
                Console.WriteLine("   Continuing with local tools only.\n");
                Console.ResetColor();

                // Create agent with local tools only
                featurePlanningCopilot = CreateAgentWithLocalToolsOnly(chatClient);
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"⚠️  Could not connect to GitHub MCP: {ex.Message}");
            Console.WriteLine("   Continuing with local tools only.\n");
            Console.ResetColor();

            // Create agent with local tools only
            featurePlanningCopilot = CreateAgentWithLocalToolsOnly(chatClient);
        }

        AgentThread thread = featurePlanningCopilot.GetNewThread();

        Console.WriteLine("🤖 Feature Planning Copilot with GitHub MCP is ready!\n");
        Console.WriteLine(new string('═', 80) + "\n");

        // Demo: Create a feature spec and optionally a GitHub issue
        var featureRequest = SampleFeatureIdeas[0];

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"📝 FEATURE REQUEST:");
        Console.WriteLine($"   {featureRequest}\n");
        Console.ResetColor();

        try
        {
            string contextualPrompt = $"""
                Project Context:
                {memory.ToContextString()}

                Feature Request:
                {featureRequest}

                Please:
                1. Analyze this feature request using your tools
                2. Create a detailed feature specification
                3. Show the metadata suggestions
                4. Ask if I want to create a GitHub issue from this spec
                """;

            var response = await featurePlanningCopilot.RunAsync(contextualPrompt, thread);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("🎯 COPILOT RESPONSE:");
            Console.ResetColor();
            Console.WriteLine(response.Text);

            Console.WriteLine("\n" + new string('═', 80) + "\n");

            // Interactive follow-up
            Console.WriteLine("💡 You can now ask the agent to create a GitHub issue or refine the spec.\n");

            while (true)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write("You: ");
                Console.ResetColor();

                string? input = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(input))
                    continue;

                if (input.Equals("q", StringComparison.OrdinalIgnoreCase) ||
                    input.Equals("quit", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                Console.WriteLine();

                var followUp = await featurePlanningCopilot.RunAsync(input, thread);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("📋 Copilot: ");
                Console.ResetColor();
                Console.WriteLine(followUp.Text);
                Console.WriteLine();
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"❌ Error: {ex.Message}");
            Console.ResetColor();
        }
        finally
        {
            // Clean up MCP client
            if (mcpClient != null)
            {
                await mcpClient.DisposeAsync();
            }
        }

        Console.WriteLine("\n✅ Sample Complete!");
        Console.WriteLine("💡 The Feature Planning Copilot can now create real GitHub issues from specs.");
    }

    /// <summary>
    /// Fallback execution without MCP (when GitHub token is not available)
    /// </summary>
    private static async Task ExecuteWithoutMCP()
    {
        Console.WriteLine("Running in simulation mode (no GitHub MCP)...\n");

        var memory = Chapter2_Sample03_FeaturePlanningWithTools.CreateDefaultProjectMemory();

        var azureClient = new AzureOpenAIClient(AIConfig.Endpoint, AIConfig.KeyCredential);
        var chatClient = azureClient
            .GetChatClient(AIConfig.ModelDeployment)
            .AsIChatClient();

        // Use factory method for simulated agent
        AIAgent featurePlanningCopilot = CreateAgentWithSimulatedMCP(chatClient);
        AgentThread thread = featurePlanningCopilot.GetNewThread();

        var featureRequest = SampleFeatureIdeas[1];

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"📝 INPUT: {featureRequest}\n");
        Console.ResetColor();

        try
        {
            var response = await featurePlanningCopilot.RunAsync(
                $"Create a feature spec for: {featureRequest}\nThen show what a GitHub issue would look like.",
                thread);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("🎯 COPILOT RESPONSE:");
            Console.ResetColor();
            Console.WriteLine(response.Text);
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"❌ Error: {ex.Message}");
            Console.ResetColor();
        }

        Console.WriteLine("\n✅ Simulation complete. Set GITHUB_PERSONAL_ACCESS_TOKEN to enable real GitHub integration.");
    }

    /// <summary>
    /// Interactive mode with full MCP capabilities
    /// </summary>
    public static async Task ExecuteInteractive()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        Console.WriteLine("\n═══════════════════════════════════════════════════════════════════════════════");
        Console.WriteLine("   CHAPTER 2 - SAMPLE 04: FEATURE PLANNING WITH MCP (INTERACTIVE)");
        Console.WriteLine("═══════════════════════════════════════════════════════════════════════════════\n");

        await Execute(); // Reuse the main Execute which has interactive follow-up
    }

    /// <summary>
    /// Runs the agent in DevUI mode - a web-based interactive interface
    /// Open http://localhost:5000/devui in your browser
    /// Note: DevUI mode uses local tools only. For full MCP integration, use Execute() or ExecuteInteractive().
    /// </summary>
    public static void ExecuteWithDevUI()
    {
        Console.WriteLine("\n═══════════════════════════════════════════════════════════════════════════════");
        Console.WriteLine("   CHAPTER 2 - SAMPLE 04: FEATURE PLANNING WITH MCP (DEVUI MODE)");
        Console.WriteLine("═══════════════════════════════════════════════════════════════════════════════\n");

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("⚠️  Note: DevUI mode runs with local tools (StoryHealthCheck, FeatureMetadata).");
        Console.WriteLine("   For full GitHub MCP integration, use Execute() or ExecuteInteractive().");
        Console.ResetColor();
        Console.WriteLine();

        var agentSpec = new AgentSpec(
            AgentName,
            GetAgentInstructions(),
            Chapter2_Sample03_FeaturePlanningWithTools.GetToolsList());

        DevUIHelper.RunWithDevUI(agentSpec);
    }

    #endregion
}
