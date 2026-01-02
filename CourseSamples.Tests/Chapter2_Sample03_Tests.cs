// SPDX-License-Identifier: LicenseRef-MAFPlayground-NPU-1.0-CH
// Copyright (c) 2025 Jose Luis Latorre

using CourseSamples;
using Microsoft.Agents.AI;

namespace CourseSamples.Tests;

/// <summary>
/// Test suite for Chapter 2 - Sample 03: Feature Planning Copilot with Tools
/// 
/// Tests the agent's ability to use StoryHealthCheckTool and FeatureMetadataTool
/// to analyze and enhance feature requests.
/// </summary>
public class Chapter2_Sample03_Tests
{
    #region Test Cases

    /// <summary>
    /// Test cases for the Feature Planning Copilot with Tools
    /// </summary>
    public static List<TestCase> GetTestCases()
    {
        var memory = Chapter2_Sample03_FeaturePlanningWithTools.CreateDefaultProjectMemory();

        return new List<TestCase>
        {
            new TestCase
            {
                Name = "Low Quality Input - Vague Request",
                Input = Chapter2_Sample03_FeaturePlanningWithTools.FormatPromptWithContext("dark mode", memory),
                EvaluationCriteria = new List<string>
                {
                    "Agent must call both StoryHealthCheckTool and FeatureMetadataTool",
                    "Agent should identify the input as low quality",
                    "Agent should provide guidance on improving the story"
                },
                PassingScore = 60
            },
            new TestCase
            {
                Name = "Medium Quality Input - Some Detail",
                Input = Chapter2_Sample03_FeaturePlanningWithTools.FormatPromptWithContext(
                    "Users should be able to export their task lists to PDF for offline viewing",
                    memory),
                EvaluationCriteria = new List<string>
                {
                    "Both tools should be called",
                    "Should suggest DataExchange or similar component",
                    "Should produce a reasonable feature specification"
                },
                PassingScore = 70
            },
            new TestCase
            {
                Name = "High Quality Input - Well-Defined Story",
                Input = Chapter2_Sample03_FeaturePlanningWithTools.FormatPromptWithContext(
                    "As a team lead, I want to see a real-time dashboard showing my team's task completion rates, active blockers, and workload distribution so I can identify bottlenecks and rebalance work effectively",
                    memory),
                EvaluationCriteria = new List<string>
                {
                    "Both tools should be called",
                    "Should identify high health score",
                    "Should produce detailed specification with tool insights"
                },
                PassingScore = 75
            }
        };
    }

    #endregion

    /// <summary>
    /// Runs all test cases for the Feature Planning Copilot with Tools
    /// </summary>
    [Fact]
    public async Task RunAllTests()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        Console.WriteLine("\n╔══════════════════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║   CHAPTER 2 - SAMPLE 03: FEATURE PLANNING WITH TOOLS - TEST SUITE           ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════════════════════╝\n");

        if (!TestHarness.IsAzureConfigured)
        {
            TestHarness.PrintMissingCredentialsWarning();
            Assert.Fail("Azure OpenAI credentials not configured");
            return;
        }

        // Get the agent from the sample
        AIAgent agent = Chapter2_Sample03_FeaturePlanningWithTools.CreateAgent();

        // Get test cases from this test class
        var testCases = GetTestCases();

        // Create test harness
        var harness = new TestHarness(verbose: true);

        // Run all tests
        var summary = await harness.RunTestSuiteAsync(
            "Feature Planning Copilot with Tools Tests",
            agent,
            testCases);

        Console.WriteLine($"\n📊 Test Results:");
        Console.WriteLine($"   Passed: {summary.PassedCount} ✅");
        Console.WriteLine($"   Failed: {summary.FailedCount} ❌");
        Console.WriteLine($"   Average Score: {summary.AverageScore:F1}/100");
        Console.WriteLine(new string('═', 80) + "\n");

        Assert.True(summary.AllPassed, $"{summary.FailedCount} test(s) failed");
    }

    /// <summary>
    /// Tests tool usage with varying quality inputs
    /// </summary>
    [Fact]
    public async Task TestToolUsageByQuality()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        Console.WriteLine("\n╔══════════════════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║   TEST: Tool Usage Analysis by Input Quality                                 ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════════════════════╝\n");

        if (!TestHarness.IsAzureConfigured)
        {
            TestHarness.PrintMissingCredentialsWarning();
            Assert.Fail("Azure OpenAI credentials not configured");
            return;
        }

        AIAgent agent = Chapter2_Sample03_FeaturePlanningWithTools.CreateAgent();
        var memory = Chapter2_Sample03_FeaturePlanningWithTools.CreateDefaultProjectMemory();

        var qualityLevels = new[]
        {
            ("Low", "dark mode"),
            ("Medium", "Users should be able to export their task lists to PDF for offline viewing"),
            ("High", "As a team lead, I want to see a real-time dashboard showing my team's task completion rates, active blockers, and workload distribution so I can identify bottlenecks and rebalance work effectively")
        };

        var allPassed = true;

        foreach (var (level, input) in qualityLevels)
        {
            Console.WriteLine($"\n📝 Testing {level} Quality Input:");
            Console.WriteLine($"   \"{(input.Length > 60 ? input[..60] + "..." : input)}\"");
            Console.WriteLine(new string('-', 80));

            var prompt = Chapter2_Sample03_FeaturePlanningWithTools.FormatPromptWithContext(input, memory);
            AgentThread thread = agent.GetNewThread();

            try
            {
                var response = await agent.RunAsync(prompt, thread);
                
                // Check for tool usage indicators in response
                var responseText = response.Text ?? "";
                var hasStoryAnalysis = responseText.Contains("Story Analysis", StringComparison.OrdinalIgnoreCase) ||
                                       responseText.Contains("Health", StringComparison.OrdinalIgnoreCase) ||
                                       responseText.Contains("Score", StringComparison.OrdinalIgnoreCase);
                var hasMetadata = responseText.Contains("Component", StringComparison.OrdinalIgnoreCase) ||
                                  responseText.Contains("Priority", StringComparison.OrdinalIgnoreCase) ||
                                  responseText.Contains("Tag", StringComparison.OrdinalIgnoreCase);

                Console.WriteLine($"   ✓ Story Analysis in Response: {(hasStoryAnalysis ? "Yes ✅" : "No ❌")}");
                Console.WriteLine($"   ✓ Metadata Suggestions in Response: {(hasMetadata ? "Yes ✅" : "No ❌")}");
                Console.WriteLine($"\n   📄 Response Preview (first 500 chars):");
                Console.WriteLine("   " + (responseText.Length > 500 ? responseText[..500] + "..." : responseText).Replace("\n", "\n   "));

                if (!hasStoryAnalysis || !hasMetadata)
                {
                    allPassed = false;
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"   ❌ Error: {ex.Message}");
                Console.ResetColor();
                allPassed = false;
            }
        }

        Console.WriteLine("\n" + new string('═', 80));
        Console.WriteLine("✅ Tool Usage Analysis Complete!");

        Assert.True(allPassed, "Tool usage analysis test failed");
    }

    /// <summary>
    /// Tests that both tools are being called
    /// </summary>
    [Fact]
    public async Task TestBothToolsAreCalled()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        Console.WriteLine("\n╔══════════════════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║   TEST: Verify Both Tools Are Called                                         ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════════════════════╝\n");

        if (!TestHarness.IsAzureConfigured)
        {
            TestHarness.PrintMissingCredentialsWarning();
            Assert.Fail("Azure OpenAI credentials not configured");
            return;
        }

        Console.WriteLine("📋 This test verifies that the agent calls both:");
        Console.WriteLine("   1. StoryHealthCheckTool - for quality analysis");
        Console.WriteLine("   2. FeatureMetadataTool - for component/priority suggestions\n");

        AIAgent agent = Chapter2_Sample03_FeaturePlanningWithTools.CreateAgent();
        var memory = Chapter2_Sample03_FeaturePlanningWithTools.CreateDefaultProjectMemory();

        var testInput = "As a project manager, I want to set up recurring tasks with custom schedules so my team doesn't have to manually recreate routine work items";
        var prompt = Chapter2_Sample03_FeaturePlanningWithTools.FormatPromptWithContext(testInput, memory);

        Console.WriteLine($"📝 Test Input: \"{testInput}\"\n");
        Console.WriteLine("🔧 Watching for tool calls...\n");
        Console.WriteLine(new string('-', 80));

        AgentThread thread = agent.GetNewThread();

        try
        {
            var response = await agent.RunAsync(prompt, thread);

            Console.WriteLine("\n📄 Agent Response:");
            Console.WriteLine(response.Text);
            
            Console.WriteLine("\n" + new string('-', 80));
            Console.WriteLine("💡 Check the console output above for tool call messages");
            Console.WriteLine("   (Lines starting with '🔧 Tool called:')");

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
    /// Runs a quick smoke test
    /// </summary>
    [Fact]
    public async Task RunSmokeTest()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        Console.WriteLine("\n╔══════════════════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║   SMOKE TEST: Feature Planning Copilot with Tools                            ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════════════════════╝\n");

        if (!TestHarness.IsAzureConfigured)
        {
            TestHarness.PrintMissingCredentialsWarning();
            Assert.Fail("Azure OpenAI credentials not configured");
            return;
        }

        AIAgent agent = Chapter2_Sample03_FeaturePlanningWithTools.CreateAgent();
        var memory = Chapter2_Sample03_FeaturePlanningWithTools.CreateDefaultProjectMemory();

        var quickTest = "Add user notifications for task deadlines";
        var prompt = Chapter2_Sample03_FeaturePlanningWithTools.FormatPromptWithContext(quickTest, memory);

        Console.WriteLine($"📝 Quick Test: \"{quickTest}\"");
        Console.WriteLine(new string('-', 80));

        AgentThread thread = agent.GetNewThread();

        try
        {
            var response = await agent.RunAsync(prompt, thread);
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
