// SPDX-License-Identifier: LicenseRef-MAFPlayground-NPU-1.0-CH
// Copyright (c) 2025 Jose Luis Latorre

using CourseSamples;
using Microsoft.Agents.AI;

namespace CourseSamples.Tests;

/// <summary>
/// Tests for Chapter 2 Sample 01: Feature Planning Copilot (Basic Agent)
/// 
/// Uses the reusable TestHarness to test the agent defined in Chapter2_Sample01_FeaturePlanningCopilot
/// </summary>
public class Chapter2_Sample01_Tests
{
    #region Test Cases

    /// <summary>
    /// Test cases with evaluation criteria for the Feature Planning Copilot
    /// </summary>
    public static List<TestCase> GetTestCases() => new()
    {
        new TestCase
        {
            Name = "Vague Input - Single Feature",
            Input = "dark mode",
            EvaluationCriteria = new List<string>
            {
                "Contains a clear feature title",
                "Has a user story or at least explains the user benefit",
                "Includes at least 2 acceptance criteria",
                "Provides some technical considerations"
            },
            PassingScore = 60
        },
        new TestCase
        {
            Name = "Medium Detail Input",
            Input = "Users should be able to export their data as PDF",
            EvaluationCriteria = new List<string>
            {
                "Contains a clear feature title",
                "Has a properly formatted user story (As a... I want... so that...)",
                "Includes specific acceptance criteria",
                "Mentions technical considerations",
                "Specifies priority and effort"
            },
            PassingScore = 70
        },
        new TestCase
        {
            Name = "Well-Defined Input",
            Input = "As a team lead, I want to see a real-time dashboard showing my team's task completion rates, active blockers, and workload distribution so I can identify bottlenecks and rebalance work effectively",
            EvaluationCriteria = new List<string>
            {
                "Preserves the original user story intent",
                "Expands with detailed acceptance criteria",
                "Provides relevant technical considerations for real-time features",
                "Suggests appropriate priority and complexity"
            },
            PassingScore = 75
        }
    };

    #endregion

    /// <summary>
    /// Run all tests for Sample 01
    /// </summary>
    [Fact]
    public async Task RunAllTests()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        Console.WriteLine("\n╔═══════════════════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║     CHAPTER 2 - SAMPLE 01 TESTS: FEATURE PLANNING COPILOT (BASIC AGENT)      ║");
        Console.WriteLine("╚═══════════════════════════════════════════════════════════════════════════════╝\n");

        if (!TestHarness.IsAzureConfigured)
        {
            TestHarness.PrintMissingCredentialsWarning();
            Assert.Fail("Azure OpenAI credentials not configured");
            return;
        }

        // Create the test harness
        var harness = new TestHarness(verbose: true);

        // Create the agent from Sample 01
        Console.WriteLine("🤖 Creating Feature Planning Copilot agent...\n");
        AIAgent agent = Chapter2_Sample01_FeaturePlanningCopilot.CreateAgent();

        // Get test cases from this test class
        var testCases = GetTestCases();

        // Run the test suite
        var summary = await harness.RunTestSuiteAsync(
            suiteName: "Sample 01 - Basic Feature Planning Copilot",
            agent: agent,
            testCases: testCases,
            useSharedThread: false // Each test gets a fresh thread
        );

        // Additional assertions or reporting
        if (summary.AllPassed)
        {
            Console.WriteLine("🎉 All Sample 01 tests passed successfully!");
        }
        else
        {
            Console.WriteLine($"⚠️ {summary.FailedCount} test(s) failed. Review the output above for details.");
        }

        Assert.True(summary.AllPassed, $"{summary.FailedCount} test(s) failed");
    }

    /// <summary>
    /// Run a quick smoke test with a single input
    /// </summary>
    [Fact]
    public async Task RunSmokeTest()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        Console.WriteLine("\n🔥 SMOKE TEST: Sample 01 - Feature Planning Copilot\n");

        if (!TestHarness.IsAzureConfigured)
        {
            TestHarness.PrintMissingCredentialsWarning();
            Assert.Fail("Azure OpenAI credentials not configured");
            return;
        }

        var harness = new TestHarness(verbose: true);
        AIAgent agent = Chapter2_Sample01_FeaturePlanningCopilot.CreateAgent();

        var smokeTest = new TestCase
        {
            Name = "Smoke Test - Basic Input",
            Input = "Add user authentication",
            EvaluationCriteria = new List<string>
            {
                "Produces a structured output",
                "Contains a feature title",
                "Includes acceptance criteria"
            },
            PassingScore = 60
        };

        var result = await harness.RunTestAsync(agent, smokeTest);
        harness.PrintTestResult(result);

        Console.WriteLine(result.Passed ? "\n✅ Smoke test passed!" : "\n❌ Smoke test failed!");
        
        Assert.True(result.Passed, "Smoke test failed");
    }

    /// <summary>
    /// Test with custom inputs (for manual/exploratory testing)
    /// </summary>
    public async Task TestWithCustomInput(string featureIdea)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        Console.WriteLine($"\n🧪 Testing with custom input: \"{featureIdea}\"\n");

        if (!TestHarness.IsAzureConfigured)
        {
            TestHarness.PrintMissingCredentialsWarning();
            return;
        }

        var harness = new TestHarness(verbose: true);
        AIAgent agent = Chapter2_Sample01_FeaturePlanningCopilot.CreateAgent();

        var customTest = new TestCase
        {
            Name = "Custom Input Test",
            Input = featureIdea,
            EvaluationCriteria = new List<string>
            {
                "Contains a clear feature title",
                "Has a properly formatted user story",
                "Includes specific acceptance criteria",
                "Provides technical considerations",
                "Specifies priority and effort estimation"
            },
            PassingScore = 70
        };

        var result = await harness.RunTestAsync(agent, customTest);
        harness.PrintTestResult(result);

        // Print full output for review
        if (!string.IsNullOrEmpty(result.ActualOutput))
        {
            Console.WriteLine("\n📄 Full Agent Output:");
            Console.WriteLine(new string('-', 60));
            Console.WriteLine(result.ActualOutput);
        }
    }
}
