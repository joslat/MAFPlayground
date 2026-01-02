// SPDX-License-Identifier: LicenseRef-MAFPlayground-NPU-1.0-CH
// Copyright (c) 2025 Jose Luis Latorre

using CourseSamples;
using Microsoft.Agents.AI;

namespace CourseSamples.Tests;

/// <summary>
/// Tests for Chapter 2 Sample 02: Feature Planning Copilot with Memory
/// 
/// Tests that the agent properly uses project context (tech stack, constraints, preferences)
/// </summary>
public class Chapter2_Sample02_Tests
{
    #region Test Cases

    /// <summary>
    /// Test cases with evaluation criteria for context-aware feature planning
    /// </summary>
    public static List<TestCase> GetTestCases()
    {
        var memory = Chapter2_Sample02_FeaturePlanningWithMemory.CreateDefaultProjectMemory();
        
        return new List<TestCase>
        {
            new TestCase
            {
                Name = "Context-Aware Feature - Tech Stack Alignment",
                Input = Chapter2_Sample02_FeaturePlanningWithMemory.FormatPromptWithContext(
                    "Add real-time notifications when tasks are assigned", memory),
                EvaluationCriteria = new List<string>
                {
                    "References or aligns with the provided tech stack (mentions .NET, Blazor, or SignalR)",
                    "Considers real-time requirements appropriately",
                    "Technical considerations match the project architecture"
                },
                PassingScore = 70
            },
            new TestCase
            {
                Name = "Context-Aware Feature - Constraint Compliance",
                Input = Chapter2_Sample02_FeaturePlanningWithMemory.FormatPromptWithContext(
                    "Users should be able to work on tasks without internet", memory),
                EvaluationCriteria = new List<string>
                {
                    "Addresses the offline mode constraint",
                    "Mentions sync or local storage considerations",
                    "Acceptance criteria account for offline scenarios"
                },
                PassingScore = 70
            },
            new TestCase
            {
                Name = "Context-Aware Feature - Accessibility",
                Input = Chapter2_Sample02_FeaturePlanningWithMemory.FormatPromptWithContext(
                    "Add a dashboard with charts and graphs", memory),
                EvaluationCriteria = new List<string>
                {
                    "Considers accessibility requirements (WCAG)",
                    "Mentions screen reader or keyboard navigation",
                    "Technical considerations address accessibility"
                },
                PassingScore = 65
            }
        };
    }

    #endregion

    /// <summary>
    /// Run all tests for Sample 02
    /// </summary>
    [Fact]
    public async Task RunAllTests()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        Console.WriteLine("\n╔═══════════════════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║     CHAPTER 2 - SAMPLE 02 TESTS: FEATURE PLANNING WITH MEMORY                ║");
        Console.WriteLine("╚═══════════════════════════════════════════════════════════════════════════════╝\n");

        if (!TestHarness.IsAzureConfigured)
        {
            TestHarness.PrintMissingCredentialsWarning();
            Assert.Fail("Azure OpenAI credentials not configured");
            return;
        }

        var harness = new TestHarness(verbose: true);

        // Create the agent from Sample 02
        Console.WriteLine("🤖 Creating Feature Planning Copilot with Memory agent...\n");
        AIAgent agent = Chapter2_Sample02_FeaturePlanningWithMemory.CreateAgent();

        // Get test cases from this test class
        var testCases = GetTestCases();

        // Run the test suite
        var summary = await harness.RunTestSuiteAsync(
            suiteName: "Sample 02 - Feature Planning with Memory",
            agent: agent,
            testCases: testCases,
            useSharedThread: true // Use shared thread to test memory accumulation
        );

        if (summary.AllPassed)
        {
            Console.WriteLine("🎉 All Sample 02 tests passed successfully!");
        }
        else
        {
            Console.WriteLine($"⚠️ {summary.FailedCount} test(s) failed. Review the output above for details.");
        }

        Assert.True(summary.AllPassed, $"{summary.FailedCount} test(s) failed");
    }

    /// <summary>
    /// Test that project memory context is respected
    /// </summary>
    [Fact]
    public async Task TestProjectContextIntegration()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        Console.WriteLine("\n🧪 Testing Project Context Integration...\n");

        if (!TestHarness.IsAzureConfigured)
        {
            TestHarness.PrintMissingCredentialsWarning();
            Assert.Fail("Azure OpenAI credentials not configured");
            return;
        }

        var harness = new TestHarness(verbose: true);
        AIAgent agent = Chapter2_Sample02_FeaturePlanningWithMemory.CreateAgent();

        // Create a minimal project memory for targeted testing
        var memory = new Chapter2_Sample02_FeaturePlanningWithMemory.ProjectMemory
        {
            ProjectName = "TestProject",
            TechStack = "Python, FastAPI, PostgreSQL",
            Constraints = new List<string> { "Must be GDPR compliant" }
        };

        var testCase = new TestCase
        {
            Name = "Tech Stack Detection",
            Input = Chapter2_Sample02_FeaturePlanningWithMemory.FormatPromptWithContext(
                "Add user profile management", memory),
            EvaluationCriteria = new List<string>
            {
                "Mentions Python or FastAPI in technical considerations",
                "References PostgreSQL for data storage",
                "Considers GDPR compliance in acceptance criteria"
            },
            PassingScore = 65
        };

        var result = await harness.RunTestAsync(agent, testCase);
        harness.PrintTestResult(result);

        if (!string.IsNullOrEmpty(result.ActualOutput))
        {
            Console.WriteLine("\n📄 Full Agent Output:");
            Console.WriteLine(new string('-', 60));
            Console.WriteLine(result.ActualOutput);
        }

        Assert.True(result.Passed, "Project context integration test failed");
    }

    /// <summary>
    /// Test memory accumulation across requests
    /// </summary>
    [Fact]
    public async Task TestMemoryAccumulation()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        Console.WriteLine("\n🧪 Testing Memory Accumulation...\n");

        if (!TestHarness.IsAzureConfigured)
        {
            TestHarness.PrintMissingCredentialsWarning();
            Assert.Fail("Azure OpenAI credentials not configured");
            return;
        }

        var harness = new TestHarness(verbose: true);
        AIAgent agent = Chapter2_Sample02_FeaturePlanningWithMemory.CreateAgent();
        AgentThread thread = agent.GetNewThread();

        var memory = Chapter2_Sample02_FeaturePlanningWithMemory.CreateDefaultProjectMemory();

        // First request
        var request1 = Chapter2_Sample02_FeaturePlanningWithMemory.FormatPromptWithContext(
            "Add task creation feature", memory);

        Console.WriteLine("📝 Request 1: Add task creation feature");
        var response1 = await agent.RunAsync(request1, thread);
        Console.WriteLine($"✅ Response received ({response1.Text.Length} chars)\n");

        // Add to memory
        memory.PreviousFeatures.Add(new Chapter2_Sample02_FeaturePlanningWithMemory.ProjectMemory.FeatureRecord(
            "Task Creation", "High", "M", DateTime.Now));

        // Second request should reference the first
        var request2 = Chapter2_Sample02_FeaturePlanningWithMemory.FormatPromptWithContext(
            "Add task assignment to team members", memory);

        Console.WriteLine("📝 Request 2: Add task assignment (should reference task creation)");

        var testCase = new TestCase
        {
            Name = "Memory Reference Test",
            Input = request2,
            EvaluationCriteria = new List<string>
            {
                "Acknowledges or references the existing task creation feature",
                "Builds upon previous work in dependencies or considerations",
                "Maintains consistency with project context"
            },
            PassingScore = 60
        };

        var result = await harness.RunTestAsync(agent, testCase, thread);
        harness.PrintTestResult(result);

        Assert.True(result.Passed, "Memory accumulation test failed");
    }
}
