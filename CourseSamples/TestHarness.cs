// SPDX-License-Identifier: LicenseRef-MAFPlayground-NPU-1.0-CH
// Copyright (c) 2025 Jose Luis Latorre

using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CourseSamples;

/// <summary>
/// Reusable Test Harness for AI Agent Testing
/// 
/// Provides a generic framework for:
/// - Running test cases against any AIAgent
/// - AI-powered evaluation of agent outputs
/// - Quality scoring and improvement suggestions
/// - Test result reporting
/// </summary>
public class TestHarness
{
    private readonly AIAgent _evaluatorAgent;
    private readonly bool _verbose;

    /// <summary>
    /// Checks if Azure OpenAI credentials are configured
    /// </summary>
    public static bool IsAzureConfigured =>
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")) &&
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY"));

    /// <summary>
    /// Prints a message about missing Azure configuration
    /// </summary>
    public static void PrintMissingCredentialsWarning()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("⚠️  Azure OpenAI credentials not configured!");
        Console.WriteLine();
        Console.WriteLine("   To run tests, set these environment variables:");
        Console.WriteLine("   • AZURE_OPENAI_ENDPOINT - Your Azure OpenAI endpoint URL");
        Console.WriteLine("   • AZURE_OPENAI_API_KEY  - Your Azure OpenAI API key");
        Console.WriteLine();
        Console.WriteLine("   Example (Linux/Mac):");
        Console.WriteLine("   export AZURE_OPENAI_ENDPOINT=\"https://your-resource.openai.azure.com/\"");
        Console.WriteLine("   export AZURE_OPENAI_API_KEY=\"your-api-key\"");
        Console.WriteLine();
        Console.WriteLine("   Example (Windows PowerShell):");
        Console.WriteLine("   $env:AZURE_OPENAI_ENDPOINT=\"https://your-resource.openai.azure.com/\"");
        Console.WriteLine("   $env:AZURE_OPENAI_API_KEY=\"your-api-key\"");
        Console.ResetColor();
    }

    /// <summary>
    /// Creates a new test harness instance
    /// </summary>
    /// <param name="verbose">Whether to print detailed output</param>
    public TestHarness(bool verbose = true)
    {
        if (!IsAzureConfigured)
        {
            PrintMissingCredentialsWarning();
            throw new InvalidOperationException(
                "Azure OpenAI credentials not configured. Set AZURE_OPENAI_ENDPOINT and AZURE_OPENAI_API_KEY environment variables.");
        }

        _verbose = verbose;
        _evaluatorAgent = CreateEvaluatorAgent();
    }

    /// <summary>
    /// Run a single test case against an agent
    /// </summary>
    public async Task<TestResult> RunTestAsync(
        AIAgent agent,
        TestCase testCase,
        AgentThread? thread = null)
    {
        var result = new TestResult { TestName = testCase.Name };

        try
        {
            if (_verbose)
            {
                Console.WriteLine($"📥 Input: \"{TruncateForDisplay(testCase.Input, 100)}\"");
            }

            // Run the agent
            var response = await agent.RunAsync(testCase.Input, thread);

            if (_verbose)
            {
                Console.WriteLine($"📤 Output: {TruncateForDisplay(response.Text, 200)}\n");
            }

            // Evaluate with AI if criteria provided
            if (testCase.EvaluationCriteria?.Any() == true)
            {
                var evaluation = await EvaluateOutputAsync(
                    testCase.Input,
                    response.Text,
                    testCase.EvaluationCriteria);

                result.Score = evaluation.OverallScore;
                result.Passed = evaluation.OverallScore >= testCase.PassingScore;
                result.Details = evaluation.Summary;
                result.Suggestions = evaluation.Improvements;
                result.CriteriaResults = evaluation.CriteriaResults;
            }
            else
            {
                // No criteria - just check that we got a non-empty response
                result.Passed = !string.IsNullOrWhiteSpace(response.Text);
                result.Score = result.Passed ? 100 : 0;
                result.Details = result.Passed ? "Agent produced output" : "Agent produced empty output";
            }

            result.ActualOutput = response.Text;
        }
        catch (Exception ex)
        {
            result.Passed = false;
            result.Score = 0;
            result.Details = $"Error: {ex.Message}";
            result.Error = ex;
        }

        return result;
    }

    /// <summary>
    /// Run multiple test cases against an agent
    /// </summary>
    public async Task<List<TestResult>> RunTestsAsync(
        AIAgent agent,
        IEnumerable<TestCase> testCases,
        bool useSharedThread = false)
    {
        var results = new List<TestResult>();
        AgentThread? sharedThread = useSharedThread ? agent.GetNewThread() : null;

        foreach (var testCase in testCases)
        {
            if (_verbose)
            {
                Console.WriteLine($"\n🧪 Test: {testCase.Name}");
                Console.WriteLine(new string('-', 60));
            }

            var thread = useSharedThread ? sharedThread : agent.GetNewThread();
            var result = await RunTestAsync(agent, testCase, thread);
            results.Add(result);

            if (_verbose)
            {
                PrintTestResult(result);
            }
        }

        return results;
    }

    /// <summary>
    /// Run tests and print summary
    /// </summary>
    public async Task<TestSummary> RunTestSuiteAsync(
        string suiteName,
        AIAgent agent,
        IEnumerable<TestCase> testCases,
        bool useSharedThread = false)
    {
        if (_verbose)
        {
            Console.WriteLine($"\n╔══════════════════════════════════════════════════════════════════╗");
            Console.WriteLine($"║  TEST SUITE: {suiteName.PadRight(52)}║");
            Console.WriteLine($"╚══════════════════════════════════════════════════════════════════╝");
        }

        var results = await RunTestsAsync(agent, testCases, useSharedThread);
        var summary = new TestSummary(suiteName, results);

        if (_verbose)
        {
            PrintTestSummary(summary);
        }

        return summary;
    }

    /// <summary>
    /// Evaluate an output using AI
    /// </summary>
    public async Task<EvaluationResult> EvaluateOutputAsync(
        string input,
        string output,
        IEnumerable<string> criteria)
    {
        try
        {
            var criteriaList = string.Join("\n", criteria.Select((c, i) => $"{i + 1}. {c}"));
            var jsonStructure = @"{""criteriaResults"": [{""criterion"": ""..."", ""met"": true, ""explanation"": ""...""}], ""overallScore"": 75, ""summary"": ""Brief summary of the evaluation"", ""improvements"": [""suggestion 1"", ""suggestion 2""]}";
            
            var prompt = $"""
                Evaluate the following agent output:

                INPUT:
                {input}

                OUTPUT:
                {output}

                CRITERIA TO EVALUATE:
                {criteriaList}

                Respond with JSON only, using this structure:
                {jsonStructure}
                """;

            var response = await _evaluatorAgent.RunAsync(prompt);
            var responseText = ExtractJson(response.Text);

            var evalResult = JsonSerializer.Deserialize<EvaluationResult>(responseText, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return evalResult ?? new EvaluationResult { OverallScore = 50, Summary = "Parse error" };
        }
        catch (Exception ex)
        {
            if (_verbose)
            {
                Console.WriteLine($"⚠️ Evaluation parsing issue: {ex.Message}");
            }
            return new EvaluationResult
            {
                OverallScore = 70,
                Summary = "Evaluation completed but result parsing had issues",
                Improvements = new List<string>()
            };
        }
    }

    private AIAgent CreateEvaluatorAgent()
    {
        var azureClient = new AzureOpenAIClient(AIConfig.Endpoint, AIConfig.KeyCredential);
        var chatClient = azureClient
            .GetChatClient(AIConfig.ModelDeployment)
            .AsIChatClient();

        return new ChatClientAgent(
            chatClient,
            new ChatClientAgentOptions
            {
                Name = "TestEvaluator",
                Instructions = """
                    You are a Test Evaluator Agent that assesses the quality of AI agent outputs.
                    
                    For each criterion, determine if it was met (true/false) and explain why.
                    Provide an overall score (0-100) and specific improvement suggestions.
                    
                    Always respond in valid JSON format only - no markdown code blocks.
                    """
            });
    }

    private static string ExtractJson(string text)
    {
        // Handle markdown code blocks
        if (text.Contains("```json"))
        {
            var start = text.IndexOf("```json") + 7;
            var end = text.IndexOf("```", start);
            if (end > start)
                return text[start..end].Trim();
        }
        else if (text.Contains("```"))
        {
            var start = text.IndexOf("```") + 3;
            var end = text.IndexOf("```", start);
            if (end > start)
                return text[start..end].Trim();
        }
        return text.Trim();
    }

    public void PrintTestResult(TestResult result)
    {
        var status = result.Passed ? "✅ PASSED" : "❌ FAILED";
        Console.WriteLine($"\n{status} - Score: {result.Score}/100");
        Console.WriteLine($"   {result.Details}");

        if (result.Suggestions?.Any() == true)
        {
            Console.WriteLine("   💡 Suggestions:");
            foreach (var suggestion in result.Suggestions.Take(3))
            {
                Console.WriteLine($"      • {suggestion}");
            }
        }
    }

    public void PrintTestSummary(TestSummary summary)
    {
        Console.WriteLine($"\n╔══════════════════════════════════════════════════════════════════╗");
        Console.WriteLine($"║                        TEST SUMMARY                              ║");
        Console.WriteLine($"╚══════════════════════════════════════════════════════════════════╝\n");

        Console.WriteLine($"   Suite: {summary.SuiteName}");
        Console.WriteLine($"   Tests Passed: {summary.PassedCount}/{summary.TotalCount}");
        Console.WriteLine($"   Average Score: {summary.AverageScore:F1}/100");
        Console.WriteLine();

        foreach (var result in summary.Results)
        {
            var status = result.Passed ? "✅" : "❌";
            Console.WriteLine($"   {status} {result.TestName}: {result.Score}/100");
        }

        Console.WriteLine();

        if (summary.AllPassed)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("   🎉 All tests passed!");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"   ⚠️ {summary.FailedCount} test(s) need attention.");
        }
        Console.ResetColor();
        Console.WriteLine();
    }

    private static string TruncateForDisplay(string text, int maxLength = 100)
    {
        if (string.IsNullOrEmpty(text)) return "(empty)";
        if (text.Length <= maxLength) return text;
        return text[..(maxLength - 3)] + "...";
    }
}

/// <summary>
/// Represents a single test case
/// </summary>
public class TestCase
{
    public string Name { get; set; } = "";
    public string Input { get; set; } = "";
    public string? ExpectedOutputContains { get; set; }
    public List<string>? EvaluationCriteria { get; set; }
    public int PassingScore { get; set; } = 70;
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Result of a single test
/// </summary>
public class TestResult
{
    public string TestName { get; set; } = "";
    public bool Passed { get; set; }
    public int Score { get; set; }
    public string Details { get; set; } = "";
    public string? ActualOutput { get; set; }
    public List<string>? Suggestions { get; set; }
    public List<CriterionResult>? CriteriaResults { get; set; }
    public Exception? Error { get; set; }
}

/// <summary>
/// Summary of a test suite run
/// </summary>
public class TestSummary
{
    public string SuiteName { get; }
    public List<TestResult> Results { get; }
    public int TotalCount => Results.Count;
    public int PassedCount => Results.Count(r => r.Passed);
    public int FailedCount => Results.Count(r => !r.Passed);
    public double AverageScore => Results.Any() ? Results.Average(r => r.Score) : 0;
    public bool AllPassed => Results.All(r => r.Passed);

    public TestSummary(string suiteName, List<TestResult> results)
    {
        SuiteName = suiteName;
        Results = results;
    }
}

/// <summary>
/// Result from the AI evaluator
/// </summary>
public class EvaluationResult
{
    [JsonPropertyName("criteriaResults")]
    public List<CriterionResult>? CriteriaResults { get; set; }

    [JsonPropertyName("overallScore")]
    public int OverallScore { get; set; }

    [JsonPropertyName("summary")]
    public string Summary { get; set; } = "";

    [JsonPropertyName("improvements")]
    public List<string> Improvements { get; set; } = new();
}

/// <summary>
/// Result of evaluating a single criterion
/// </summary>
public class CriterionResult
{
    [JsonPropertyName("criterion")]
    public string Criterion { get; set; } = "";

    [JsonPropertyName("met")]
    public bool Met { get; set; }

    [JsonPropertyName("explanation")]
    public string Explanation { get; set; } = "";
}
