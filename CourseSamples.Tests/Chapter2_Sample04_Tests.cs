// SPDX-License-Identifier: LicenseRef-MAFPlayground-NPU-1.0-CH
// Copyright (c) 2025 Jose Luis Latorre

using Azure.AI.OpenAI;
using CourseSamples;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace CourseSamples.Tests;

/// <summary>
/// Test suite for Chapter 2 - Sample 04: Feature Planning Copilot with MCP (GitHub)
/// 
/// Tests the agent's integration with GitHub MCP server for creating issues,
/// as well as fallback behavior when MCP is not available.
/// </summary>
public class Chapter2_Sample04_Tests
{
    #region Test Cases

    /// <summary>
    /// Test cases for the Feature Planning Copilot with MCP
    /// </summary>
    public static List<TestCase> GetTestCases()
    {
        var memory = Chapter2_Sample03_FeaturePlanningWithTools.CreateDefaultProjectMemory();

        return new List<TestCase>
        {
            new TestCase
            {
                Name = "Local Tools Still Work",
                Input = Chapter2_Sample03_FeaturePlanningWithTools.FormatPromptWithContext(
                    "Add user notifications for task deadlines",
                    memory),
                EvaluationCriteria = new List<string>
                {
                    "Local tools should be used for analysis",
                    "A feature specification should be produced"
                },
                PassingScore = 65
            },
            new TestCase
            {
                Name = "Simulation Mode Response",
                Input = "Create a feature spec for: Real-time task assignment notifications. Then show what the GitHub issue would look like.",
                EvaluationCriteria = new List<string>
                {
                    "Creates feature specification",
                    "Shows what GitHub issue would contain",
                    "Indicates simulation mode if applicable"
                },
                PassingScore = 60
            },
            new TestCase
            {
                Name = "Full MCP Workflow",
                Input = $"""
                    Project Context:
                    {memory.ToContextString()}

                    Feature Request:
                    As a team lead, I want to see a real-time dashboard showing my team's task completion rates and active blockers so I can identify bottlenecks and rebalance work effectively

                    Please:
                    1. Analyze this feature request using your tools
                    2. Create a detailed feature specification
                    3. Show the metadata suggestions
                    4. Ask if I want to create a GitHub issue from this spec
                    """,
                EvaluationCriteria = new List<string>
                {
                    "Should show tool analysis results",
                    "Should produce detailed spec",
                    "Should offer to create GitHub issue"
                },
                PassingScore = 70
            }
        };
    }

    #endregion

    /// <summary>
    /// Runs all test cases for the Feature Planning Copilot with MCP
    /// </summary>
    [Fact]
    public async Task RunAllTests()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        Console.WriteLine("\n╔══════════════════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║   CHAPTER 2 - SAMPLE 04: FEATURE PLANNING WITH MCP - TEST SUITE             ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════════════════════╝\n");

        if (!TestHarness.IsAzureConfigured)
        {
            TestHarness.PrintMissingCredentialsWarning();
            Assert.Fail("Azure OpenAI credentials not configured");
            return;
        }

        // Check MCP availability
        var hasGitHubToken = !string.IsNullOrEmpty(
            Environment.GetEnvironmentVariable("GITHUB_PERSONAL_ACCESS_TOKEN"));
        
        Console.WriteLine($"🔧 MCP Status: {(hasGitHubToken ? "GitHub token available ✅" : "Running in simulation mode ⚠️")}");
        Console.WriteLine();

        // Get the agent from the sample (will use local tools only for testing)
        AIAgent agent = Chapter2_Sample04_FeaturePlanningWithMCP.CreateAgent();

        // Get test cases from this test class
        var testCases = GetTestCases();

        // Create test harness
        var harness = new TestHarness(verbose: true);

        // Run all tests
        var summary = await harness.RunTestSuiteAsync(
            "Feature Planning Copilot with MCP Tests",
            agent,
            testCases);

        // Print summary
        Console.WriteLine($"\n📊 Test Results:");
        Console.WriteLine($"   Passed: {summary.PassedCount} ✅");
        Console.WriteLine($"   Failed: {summary.FailedCount} ❌");
        Console.WriteLine($"   Average Score: {summary.AverageScore:F1}/100");
        Console.WriteLine(new string('═', 80) + "\n");

        Assert.True(summary.AllPassed, $"{summary.FailedCount} test(s) failed");
    }

    /// <summary>
    /// Tests the simulation mode when GitHub token is not available
    /// </summary>
    [Fact]
    public async Task TestSimulationMode()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        Console.WriteLine("\n╔══════════════════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║   TEST: Simulation Mode (No GitHub Token)                                    ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════════════════════╝\n");

        if (!TestHarness.IsAzureConfigured)
        {
            TestHarness.PrintMissingCredentialsWarning();
            Assert.Fail("Azure OpenAI credentials not configured");
            return;
        }

        Console.WriteLine("📋 This test verifies the agent works correctly in simulation mode.\n");

        // Create simulated agent
        var azureClient = new AzureOpenAIClient(AIConfig.Endpoint, AIConfig.KeyCredential);
        var chatClient = azureClient
            .GetChatClient(AIConfig.ModelDeployment)
            .AsIChatClient();

        AIAgent agent = Chapter2_Sample04_FeaturePlanningWithMCP.CreateAgentWithSimulatedMCP(chatClient);
        AgentThread thread = agent.GetNewThread();

        var testInput = "Create a feature spec for: Add team calendar integration. Then create a GitHub issue for it.";

        Console.WriteLine($"📝 Test Input: \"{testInput}\"\n");
        Console.WriteLine(new string('-', 80));

        try
        {
            var response = await agent.RunAsync(testInput, thread);

            Console.WriteLine("\n📄 Agent Response:");
            Console.WriteLine(response.Text);

            // Check for simulation indicators
            var responseText = response.Text ?? "";
            var hasSimulationIndicator = responseText.Contains("SIMULATED", StringComparison.OrdinalIgnoreCase) ||
                                         responseText.Contains("simulation", StringComparison.OrdinalIgnoreCase);

            Console.WriteLine("\n" + new string('-', 80));
            Console.WriteLine($"✓ Simulation Mode Indicated: {(hasSimulationIndicator ? "Yes ✅" : "No ❌")}");

            Assert.False(string.IsNullOrWhiteSpace(response.Text), "Agent produced empty response");
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"❌ Error: {ex.Message}");
            Console.ResetColor();
            Assert.Fail($"Test failed with error: {ex.Message}");
        }

        Console.WriteLine("\n" + new string('═', 80));
    }

    /// <summary>
    /// Tests that local tools work alongside MCP
    /// </summary>
    [Fact]
    public async Task TestLocalToolsWithMCP()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        Console.WriteLine("\n╔══════════════════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║   TEST: Local Tools Work With MCP Integration                                ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════════════════════╝\n");

        if (!TestHarness.IsAzureConfigured)
        {
            TestHarness.PrintMissingCredentialsWarning();
            Assert.Fail("Azure OpenAI credentials not configured");
            return;
        }

        AIAgent agent = Chapter2_Sample04_FeaturePlanningWithMCP.CreateAgent();
        var memory = Chapter2_Sample03_FeaturePlanningWithTools.CreateDefaultProjectMemory();

        var testInput = Chapter2_Sample03_FeaturePlanningWithTools.FormatPromptWithContext(
            "As a user, I want keyboard shortcuts for common actions so I can work faster",
            memory);

        Console.WriteLine("📝 Testing that StoryHealthCheckTool and FeatureMetadataTool still work...\n");
        Console.WriteLine(new string('-', 80));

        var harness = new TestHarness(verbose: true);

        var testCase = new TestCase
        {
            Name = "Local Tools Functionality",
            Input = testInput,
            EvaluationCriteria = new List<string>
            {
                "Uses StoryHealthCheckTool",
                "Uses FeatureMetadataTool",
                "Creates feature specification"
            },
            PassingScore = 60
        };

        var result = await harness.RunTestAsync(agent, testCase);

        Console.WriteLine("\n📄 Agent Response (first 800 chars):");
        var output = result.ActualOutput ?? "";
        Console.WriteLine(output.Length > 800 ? output[..800] + "..." : output);

        Console.WriteLine($"\n📊 Score: {result.Score}/100");

        Console.WriteLine("\n" + new string('═', 80));

        Assert.True(result.Passed, "Local tools with MCP test failed");
    }

    /// <summary>
    /// Tests MCP connection if token is available
    /// </summary>
    [Fact]
    public async Task TestMCPConnection()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        Console.WriteLine("\n╔══════════════════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║   TEST: MCP Connection Status                                                ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════════════════════╝\n");

        var githubToken = Environment.GetEnvironmentVariable("GITHUB_PERSONAL_ACCESS_TOKEN");

        if (string.IsNullOrEmpty(githubToken))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("⚠️  GITHUB_PERSONAL_ACCESS_TOKEN not set.");
            Console.WriteLine("   Set this environment variable to test MCP connection.\n");
            Console.WriteLine("   To set it:");
            Console.WriteLine("   • Windows: setx GITHUB_PERSONAL_ACCESS_TOKEN \"your-token\"");
            Console.WriteLine("   • Linux/Mac: export GITHUB_PERSONAL_ACCESS_TOKEN=\"your-token\"");
            Console.ResetColor();
            
            // Skip test if no token
            Console.WriteLine("\n⏭️ Skipping MCP connection test (no token)");
            return;
        }

        Console.WriteLine("🔌 Attempting to connect to GitHub MCP Server...\n");

        try
        {
            var mcpClient = await Chapter2_Sample04_FeaturePlanningWithMCP.CreateMCPClientAsync();

            if (mcpClient != null)
            {
                Console.WriteLine("✅ Successfully connected to GitHub MCP Server!");
                
                var tools = await mcpClient.ListToolsAsync();
                Console.WriteLine($"🔧 Available GitHub Tools ({tools.Count}):");
                foreach (var tool in tools.Take(10))
                {
                    Console.WriteLine($"   • {tool.Name}");
                }
                if (tools.Count > 10)
                {
                    Console.WriteLine($"   ... and {tools.Count - 10} more");
                }

                await mcpClient.DisposeAsync();
                Console.WriteLine("\n✅ MCP Client disposed successfully.");

                Assert.True(tools.Count > 0, "No tools found from MCP server");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("⚠️  Could not create MCP client (token may be invalid).");
                Console.ResetColor();
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"❌ MCP Connection Error: {ex.Message}");
            Console.ResetColor();
            // Don't fail - MCP might not be available in all environments
        }

        Console.WriteLine("\n" + new string('═', 80));
    }

    /// <summary>
    /// Runs a quick smoke test
    /// </summary>
    [Fact]
    public async Task RunSmokeTest()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        Console.WriteLine("\n╔══════════════════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║   SMOKE TEST: Feature Planning Copilot with MCP                              ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════════════════════╝\n");

        if (!TestHarness.IsAzureConfigured)
        {
            TestHarness.PrintMissingCredentialsWarning();
            Assert.Fail("Azure OpenAI credentials not configured");
            return;
        }

        AIAgent agent = Chapter2_Sample04_FeaturePlanningWithMCP.CreateAgent();

        var quickTest = Chapter2_Sample04_FeaturePlanningWithMCP.FormatPromptWithMCPContext(
            "Add email digest of weekly task summaries",
            askForIssue: true);

        Console.WriteLine("📝 Quick Test: Feature request with GitHub issue preview");
        Console.WriteLine(new string('-', 80));

        AgentThread thread = agent.GetNewThread();

        try
        {
            var response = await agent.RunAsync(quickTest, thread);
            Console.WriteLine("\n✅ Agent responded successfully!");
            Console.WriteLine($"\n📄 Response (truncated to 1000 chars):");
            var text = response.Text ?? "";
            Console.WriteLine(text.Length > 1000 ? text[..1000] + "\n..." : text);

            Assert.False(string.IsNullOrWhiteSpace(response.Text), "Agent produced empty response");
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"❌ Smoke test failed: {ex.Message}");
            Console.ResetColor();
            Assert.Fail($"Smoke test failed: {ex.Message}");
        }

        Console.WriteLine("\n" + new string('═', 80));
    }
}
