// SPDX-License-Identifier: LicenseRef-MAFPlayground-NPU-1.0-CH
// Copyright (c) 2025 Jose Luis

using System.ComponentModel;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.AI.OpenAI;
using MAFPlayground.Demos.ClaimsDemo;
using MAFPlayground.Utils;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Reflection;
using Microsoft.Extensions.AI;

namespace MAFPlayground.Demos.ClaimsDemo;

/// <summary>
/// Demo 12: Claims Fraud Detection Workflow
/// 
/// This demo implements a comprehensive fraud detection pipeline that analyzes validated
/// claims through multiple AI agents working in parallel to detect potential fraud indicators.
/// 
/// Key Architecture Features:
/// - Fan-out/fan-in pattern for parallel fraud analysis
/// - Polymorphic aggregator handling multiple finding types
/// - Data passed through messages (NOT context state in fan-out executors)
/// - Context state operations only in aggregator and non-fan-out executors
/// 
/// Critical Pattern (discovered through Test01-Test06):
/// ⚠️  FAN-OUT EXECUTORS: NO context.ReadStateAsync() or context.QueueStateUpdateAsync()
/// ✅ FAN-OUT EXECUTOR (PropertyTheftFanOut): CAN use state BEFORE SendMessageAsync()
/// ✅ AGGREGATOR: CAN use state freely to store collected findings
/// ✅ OTHER EXECUTORS: CAN use state freely
/// 
/// Takes a validated claim (ValidationResult from Demo11) and processes it through
/// a comprehensive fraud detection pipeline:
/// 
/// 1. DataReviewExecutor - Initial data quality and completeness check
/// 2. ClassificationRouter - Routes to appropriate claim type handler (Property Theft for now)
/// 3. PropertyTheftFanOut - Reads state, sends claim to 3 parallel fraud detection agents:
///    - OSINTValidatorAgent: Checks if stolen property is listed for sale online
///    - UserValidationAgent: Analyzes customer's claim history and fraud score
///    - TransactionFraudScoringAgent: Scores the transaction for fraud indicators
/// 4. FraudAggregatorExecutor - Collects findings (fan-in), stores in state
/// 5. FraudDecisionAgent - Analyzes aggregated data and makes final fraud determination
/// 6. OutcomePresenterAgent - Generates professional email to insurance representative
/// </summary>
internal static class Demo12_ClaimsFraudDetection
{
    // --------------------- Shared state ---------------------
    // FraudAnalysisState is now in SharedClaimsData.cs (shared across demos)

    private static class FraudStateShared
    {
        public const string Scope = "FraudStateScope";
        public const string Key = "singleton";
    }

    private static async Task<FraudAnalysisState> ReadFraudStateAsync(IWorkflowContext context)
    {
        var state = await context.ReadStateAsync<FraudAnalysisState>(FraudStateShared.Key, scopeName: FraudStateShared.Scope);
        return state ?? new FraudAnalysisState();
    }

    private static ValueTask SaveFraudStateAsync(IWorkflowContext context, FraudAnalysisState state)
        => context.QueueStateUpdateAsync(FraudStateShared.Key, state, scopeName: FraudStateShared.Scope);

    // --------------------- Data contracts ---------------------
    // All fraud detection data contracts are now in SharedClaimsData.cs:
    // - FraudAnalysisState (workflow state)
    // - DataReviewResult
    // - OSINTFinding
    // - UserHistoryFinding
    // - TransactionFraudFinding
    // - FraudDecision

    // --------------------- Entry point ---------------------
    public static async Task Execute()
    {
        Console.WriteLine("=== Demo 12: Claims Fraud Detection Workflow ===\n");
        Console.WriteLine("This demo analyzes validated claims for potential fraud indicators.\n");
        Console.WriteLine("Features: Polymorphic aggregator, data passing via messages, state in aggregator\n");

        // Azure OpenAI setup
        var azureClient = new AzureOpenAIClient(AIConfig.Endpoint, AIConfig.KeyCredential);
        var deploymentName = "gpt-4o";
        IChatClient chatClient = azureClient.GetChatClient(deploymentName).AsIChatClient();

        // Build workflow
        var workflow = BuildFraudDetectionWorkflow(chatClient);

        WorkflowVisualizerTool.PrintAll(workflow, "Demo 12: Claims Fraud Detection Workflow");

        // Create mock validated claim from Demo11
        var mockClaim = new ValidationResult
        {
            Ready = true,
            CustomerId = "CUST-10001",
            ContractId = "CONTRACT-P-5001",
            NormalizedClaimType = "Property",
            NormalizedClaimSubType = "BikeTheft",
            DateOfLoss = "2025-01-21",
            DateReported = "2025-01-28",
            ShortDescription = "Mountain bike stolen from grocery store",
            ItemDescription = "Trek X-Caliber 8, red mountain bike, 21-speed",
            DetailedDescription = "My mountain bike was stolen from outside the grocery store. It was locked with a cable lock. The bike was a Trek X-Caliber 8, red color, worth approximately $1,200.",
            PurchasePrice = 1200.00m
        };

        Console.WriteLine("\n" + new string('=', 80));
        Console.WriteLine("FRAUD DETECTION ANALYSIS");
        Console.WriteLine(new string('=', 80) + "\n");
        Console.WriteLine("Analyzing claim:");
        Console.WriteLine($"  Customer: {mockClaim.CustomerId}");
        Console.WriteLine($"  Type: {mockClaim.NormalizedClaimType} - {mockClaim.NormalizedClaimSubType}");
        Console.WriteLine($"  Amount: ${mockClaim.PurchasePrice:N2}");
        Console.WriteLine();

        await using StreamingRun run = await InProcessExecution.StreamAsync(workflow, mockClaim);

        await foreach (WorkflowEvent evt in run.WatchStreamAsync())
        {
            switch (evt)
            {
                case AgentRunUpdateEvent agentUpdate:
                    if (!string.IsNullOrEmpty(agentUpdate.Update.Text))
                    {
                        Console.Write(agentUpdate.Update.Text);
                    }
                    break;

                case WorkflowOutputEvent output:
                    Console.WriteLine("\n\n" + new string('=', 80));
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("✅ FRAUD ANALYSIS COMPLETE");
                    Console.ResetColor();
                    Console.WriteLine(new string('=', 80));
                    Console.WriteLine();
                    Console.WriteLine(output.Data);
                    Console.WriteLine();
                    Console.WriteLine(new string('=', 80));
                    break;
            }
        }

        Console.WriteLine("\n✅ Demo 12 Complete!\n");
        Console.WriteLine("Key Concepts Demonstrated:");
        Console.WriteLine("  ✓ Data quality review before fraud analysis");
        Console.WriteLine("  ✓ Classification routing by claim type");
        Console.WriteLine("  ✓ Parallel fraud detection (fan-out/fan-in pattern)");
        Console.WriteLine("  ✓ OSINT validation with mock marketplace data");
        Console.WriteLine("  ✓ Customer history and fraud score analysis");
        Console.WriteLine("  ✓ Transaction-level fraud scoring");
        Console.WriteLine("  ✓ Polymorphic aggregator (handles 3 different finding types)");
        Console.WriteLine("  ✓ Data passing via messages (NO state in fan-out executors)");
        Console.WriteLine("  ✓ State operations in aggregator (stores collected findings)");
        Console.WriteLine("  ✓ AI-powered fraud decision with confidence scores");
        Console.WriteLine("  ✓ Professional email generation for case handlers\n");
    }

    private static Workflow BuildFraudDetectionWorkflow(IChatClient chatClient)
    {
        // Fraud-specific mock tools (Demo12 only needs these)
        var tools = new List<AITool>
        {
            AIFunctionFactory.Create(FraudMockTools.CheckOnlineMarketplaces),
            AIFunctionFactory.Create(FraudMockTools.GetCustomerClaimHistory),
            AIFunctionFactory.Create(FraudMockTools.GetTransactionRiskProfile)
        };
        // Note: Demo11 tools (GetCurrentDate, GetCustomerProfile, GetContract) not needed
        // ValidationResult already contains customer_id, contract_id, and date_reported

        // Agents
        var dataReviewAgent = GetDataReviewAgent(chatClient);
        var osintAgent = GetOSINTAgent(chatClient, tools);
        var userHistoryAgent = GetUserHistoryAgent(chatClient, tools);
        var transactionAgent = GetTransactionFraudAgent(chatClient, tools);
        var fraudDecisionAgent = GetFraudDecisionAgent(chatClient);
        var outcomePresenterAgent = GetOutcomePresenterAgent(chatClient);

        // Executors
        var dataReviewExec = new DataReviewExecutor(dataReviewAgent);
        var classificationExec = new ClassificationRouterExecutor();
        var propertyTheftFanOutExec = new PropertyTheftFanOutExecutor();
        var osintExec = new OSINTExecutor(osintAgent);
        var userHistoryExec = new UserHistoryExecutor(userHistoryAgent);
        var transactionExec = new TransactionFraudExecutor(transactionAgent);
        var fraudAggregatorExec = new FraudAggregatorExecutor();
        var fraudDecisionExec = new FraudDecisionExecutor(fraudDecisionAgent);
        var outcomeExec = new OutcomePresenterExecutor(outcomePresenterAgent);

        // Build workflow
        return new WorkflowBuilder(dataReviewExec)
            .AddEdge<DataReviewResult>(dataReviewExec, classificationExec, 
                condition: dr => dr is not null && dr.Proceed)
            .AddEdge(classificationExec, propertyTheftFanOutExec)
            .AddFanOutEdge(propertyTheftFanOutExec, targets: [osintExec, userHistoryExec, transactionExec])
            .AddFanInEdge(fraudAggregatorExec, sources: [osintExec, userHistoryExec, transactionExec])
            .AddEdge(fraudAggregatorExec, fraudDecisionExec)
            .AddEdge(fraudDecisionExec, outcomeExec)
            .WithOutputFrom(outcomeExec)
            .Build();
    }

    // --------------------- Agent factories ---------------------
    
    private static AIAgent GetDataReviewAgent(IChatClient chat) =>
        new ChatClientAgent(chat, new ChatClientAgentOptions
        {
            Name = "DataReviewAgent",
            Instructions = """
                You are a data quality specialist for insurance claims.
                
                Review the claim data for:
                1. Completeness - all required fields present
                2. Consistency - values make sense together
                3. Data quality - descriptions are detailed enough
                4. Red flags - obviously suspicious patterns
                
                Provide a quality score (0-100) and list any concerns.
                Decide whether to proceed with fraud analysis.
                """,
            ChatOptions = new()
            {
                ResponseFormat = ChatResponseFormat.ForJsonSchema<DataReviewResult>()
            }
        });

    private static AIAgent GetOSINTAgent(IChatClient chat, List<AITool> tools) =>
        new ChatClientAgent(chat, new ChatClientAgentOptions
        {
            Name = "OSINTValidatorAgent",
            Instructions = """
                You are an OSINT (Open Source Intelligence) specialist.
                
                Check if the reported stolen property is listed for sale online.
                Use the check_online_marketplaces tool to search Swiss marketplaces
                (ricardo.ch, anibis.ch, etc.) and international sites (eBay).
                
                Provide:
                - Whether item was found
                - Which marketplaces were checked
                - Details of matching listings
                - Fraud indicator score (0-100)
                - Summary of findings
                """,
            ChatOptions = new()
            {
                Tools = tools,
                ResponseFormat = ChatResponseFormat.ForJsonSchema<OSINTFinding>()
            }
        });

    private static AIAgent GetUserHistoryAgent(IChatClient chat, List<AITool> tools) =>
        new ChatClientAgent(chat, new ChatClientAgentOptions
        {
            Name = "UserValidationAgent",
            Instructions = """
                You are a customer fraud analyst.
                
                Analyze the customer's claim history for suspicious patterns.
                Use get_customer_claim_history tool to retrieve past claims.
                
                Look for:
                - Frequency of claims
                - Pattern of claim types
                - Previous fraud indicators
                - Historical fraud score
                
                Provide customer fraud score (0-100) and summary.
                """,
            ChatOptions = new()
            {
                Tools = tools,
                ResponseFormat = ChatResponseFormat.ForJsonSchema<UserHistoryFinding>()
            }
        });

    private static AIAgent GetTransactionFraudAgent(IChatClient chat, List<AITool> tools) =>
        new ChatClientAgent(chat, new ChatClientAgentOptions
        {
            Name = "TransactionFraudScoringAgent",
            Instructions = """
                You are a transaction fraud specialist.
                
                Analyze this specific claim transaction for fraud indicators:
                - Claim amount vs typical claims
                - Timing patterns (e.g., filed right before policy expires)
                - Description quality and plausibility
                - Geographic/behavioral anomalies
                
                Use get_transaction_risk_profile tool for context.
                
                Provide:
                - Anomaly score (0-100)
                - List of red flags detected
                - Overall transaction fraud score
                - Summary
                """,
            ChatOptions = new()
            {
                Tools = tools,
                ResponseFormat = ChatResponseFormat.ForJsonSchema<TransactionFraudFinding>()
            }
        });

    private static AIAgent GetFraudDecisionAgent(IChatClient chat) =>
        new ChatClientAgent(chat, new ChatClientAgentOptions
        {
            Name = "FraudDecisionAgent",
            Instructions = """
                You are a fraud decision specialist.
                
                Analyze ALL the findings from OSINT, User History, and Transaction analysis.
                
                Make a final determination:
                - Is this likely fraud? (true/false)
                - Confidence score (0-100)
                - Combined fraud score (0-100)
                - Recommendation: APPROVE / INVESTIGATE / REJECT
                - Clear reasoning
                - Key factors that influenced your decision
                
                Be thorough but fair. Consider all evidence.
                """,
            ChatOptions = new()
            {
                ResponseFormat = ChatResponseFormat.ForJsonSchema<FraudDecision>()
            }
        });

    private static AIAgent GetOutcomePresenterAgent(IChatClient chat) =>
        new ChatClientAgent(chat, """
            You are a professional fraud case reporter.
            
            Generate a clear, professional email to the insurance case handler
            summarizing the fraud analysis and recommendation.
            
            Include:
            - Executive summary
            - Fraud determination and confidence
            - Key findings from each analysis
            - Recommended action
            - Next steps
            
            Use professional, formal language.
            Be factual and avoid speculation beyond the analysis.
            """);

    // --------------------- Executors ---------------------

    private sealed class DataReviewExecutor :
        ReflectingExecutor<DataReviewExecutor>,
        IMessageHandler<ValidationResult, DataReviewResult>
    {
        private readonly AIAgent _agent;
        public DataReviewExecutor(AIAgent agent) : base("DataReview") => _agent = agent;

        public async ValueTask<DataReviewResult> HandleAsync(
            ValidationResult claim,
            IWorkflowContext context,
            CancellationToken cancellationToken = default)
        {
            var state = await ReadFraudStateAsync(context);
            state.OriginalClaim = claim;
            
            Console.WriteLine("\n=== Data Quality Review ===\n");

            var prompt = $"""
                Review this claim data for quality and completeness:
                
                Customer ID: {claim.CustomerId}
                Contract ID: {claim.ContractId}
                Type: {claim.NormalizedClaimType} - {claim.NormalizedClaimSubType}
                Date of Loss: {claim.DateOfLoss}
                Date Reported: {claim.DateReported}
                Item: {claim.ItemDescription}
                Value: ${claim.PurchasePrice}
                
                Short Description: {claim.ShortDescription}
                Detailed Description: {claim.DetailedDescription}
                """;

            var response = await _agent.RunAsync(prompt, cancellationToken: cancellationToken);
            var result = response.Deserialize<DataReviewResult>(JsonSerializerOptions.Web);

            state.DataReview = result;
            await SaveFraudStateAsync(context, state);

            Console.WriteLine($"Quality Score: {result.QualityScore}/100");
            Console.WriteLine($"Proceed: {result.Proceed}\n");

            return result;
        }
    }

    private sealed class ClassificationRouterExecutor :
        ReflectingExecutor<ClassificationRouterExecutor>,
        IMessageHandler<DataReviewResult, string>
    {
        public ClassificationRouterExecutor() : base("ClassificationRouter") { }

        public async ValueTask<string> HandleAsync(
            DataReviewResult review,
            IWorkflowContext context,
            CancellationToken cancellationToken = default)
        {
            var state = await ReadFraudStateAsync(context);
            var claim = state.OriginalClaim!;
            
            Console.WriteLine($"\n=== Classification Router ===");
            Console.WriteLine($"Claim Type: {claim.NormalizedClaimType}");
            Console.WriteLine($"Sub-Type: {claim.NormalizedClaimSubType}\n");

            // For now, route everything to Property Theft
            // Future: Add other claim type handlers
            state.ClaimType = claim.NormalizedClaimType ?? "";
            state.ClaimSubType = claim.NormalizedClaimSubType ?? "";
            await SaveFraudStateAsync(context, state);

            return "PropertyTheft";
        }
    }

    private sealed class PropertyTheftFanOutExecutor :
        ReflectingExecutor<PropertyTheftFanOutExecutor>,
        IMessageHandler<string>
    {
        public PropertyTheftFanOutExecutor() : base("PropertyTheftFanOut") { }

        public async ValueTask HandleAsync(
            string routeType,
            IWorkflowContext context,
            CancellationToken cancellationToken = default)
        {
            Console.WriteLine("\n=== Parallel Fraud Detection (Fan-Out) ===");
            Console.WriteLine("Dispatching to 3 fraud detection agents...\n");
            
            // ✅ Read state HERE (fan-out executor can use state)
            var state = await ReadFraudStateAsync(context);
            var claim = state.OriginalClaim!;
            
            // ✅ Send the CLAIM to all executors (not a string!)
            await context.SendMessageAsync(claim, cancellationToken: cancellationToken);
        }
    }

    // ===== REFACTORED EXECUTORS - Now return simple strings =====

    private sealed class OSINTExecutor :
        ReflectingExecutor<OSINTExecutor>,
        IMessageHandler<ValidationResult, OSINTFinding>  // ✅ Accept claim directly!
    {
        private readonly AIAgent _agent;
        public OSINTExecutor(AIAgent agent) : base("OSINT") => _agent = agent;

        // ✅ NO STATE OPERATIONS AT ALL!
        public async ValueTask<OSINTFinding> HandleAsync(
            ValidationResult claim,  // ✅ Claim passed directly from PropertyTheftFanOut!
            IWorkflowContext context,
            CancellationToken cancellationToken = default)
        {
            Console.WriteLine("=== OSINT Validation (Online Marketplaces) ===\n");

            var prompt = $"""
                Check if this stolen item is listed for sale online:
                
                Item Description: {claim.ItemDescription}
                Detailed Context: {claim.DetailedDescription}
                Value: ${claim.PurchasePrice}
                Date of Loss: {claim.DateOfLoss}
                
                Use check_online_marketplaces tool to search.
                """;

            var response = await _agent.RunAsync(prompt, cancellationToken: cancellationToken);
            var finding = response.Deserialize<OSINTFinding>(JsonSerializerOptions.Web);

            Console.WriteLine($"✓ OSINT Check Complete - Fraud Score: {finding.FraudIndicatorScore}/100");
            Console.WriteLine($"[DEBUG - OSINTExecutor] Returning finding directly\n");
            
            return finding;  // ✅ Return structured finding!
        }
    }

    private sealed class UserHistoryExecutor :
        ReflectingExecutor<UserHistoryExecutor>,
        IMessageHandler<ValidationResult, UserHistoryFinding>  // ✅ Accept claim directly!
    {
        private readonly AIAgent _agent;
        public UserHistoryExecutor(AIAgent agent) : base("UserHistory") => _agent = agent;

        // ✅ NO STATE OPERATIONS AT ALL!
        public async ValueTask<UserHistoryFinding> HandleAsync(
            ValidationResult claim,  // ✅ Claim passed directly from PropertyTheftFanOut!
            IWorkflowContext context,
            CancellationToken cancellationToken = default)
        {
            Console.WriteLine("=== User History Analysis ===\n");

            var prompt = $"""
                Analyze this customer's claim history for fraud patterns:
                
                Customer ID: {claim.CustomerId}
                Current Claim Type: {claim.NormalizedClaimType} - {claim.NormalizedClaimSubType}
                
                Use get_customer_claim_history tool to retrieve past claims.
                """;

            var response = await _agent.RunAsync(prompt, cancellationToken: cancellationToken);
            var finding = response.Deserialize<UserHistoryFinding>(JsonSerializerOptions.Web);

            Console.WriteLine($"✓ User History Check Complete - Customer Fraud Score: {finding.CustomerFraudScore}/100");
            Console.WriteLine($"[DEBUG - UserHistoryExecutor] Returning finding directly\n");
            
            return finding;  // ✅ Return structured finding!
        }
    }

    private sealed class TransactionFraudExecutor :
        ReflectingExecutor<TransactionFraudExecutor>,
        IMessageHandler<ValidationResult, TransactionFraudFinding>  // ✅ Accept claim directly!
    {
        private readonly AIAgent _agent;
        public TransactionFraudExecutor(AIAgent agent) : base("TransactionFraud") => _agent = agent;

        // ✅ NO STATE OPERATIONS AT ALL!
        public async ValueTask<TransactionFraudFinding> HandleAsync(
            ValidationResult claim,  // ✅ Claim passed directly from PropertyTheftFanOut!
            IWorkflowContext context,
            CancellationToken cancellationToken = default)
        {
            Console.WriteLine("=== Transaction Fraud Scoring ===\n");

            var prompt = $"""
                Analyze this transaction for fraud indicators:
                
                Claim Amount: ${claim.PurchasePrice}
                Date of Loss: {claim.DateOfLoss}
                Description: {claim.DetailedDescription}
                
                Use get_transaction_risk_profile tool for context.
                """;

            var response = await _agent.RunAsync(prompt, cancellationToken: cancellationToken);
            var finding = response.Deserialize<TransactionFraudFinding>(JsonSerializerOptions.Web);

            Console.WriteLine($"✓ Transaction Analysis Complete - Fraud Score: {finding.TransactionFraudScore}/100");
            Console.WriteLine($"[DEBUG - TransactionFraudExecutor] Returning finding directly\n");
            
            return finding;  // ✅ Return structured finding!
        }
    }

    // ===== POLYMORPHIC AGGREGATOR - Handles different finding types! =====

    /// <summary>
    /// Wrapper class to hold findings from different executors
    /// </summary>
    private sealed class FraudFindingMessage
    {
        public OSINTFinding? OSINTFinding { get; set; }
        public UserHistoryFinding? UserHistoryFinding { get; set; }
        public TransactionFraudFinding? TransactionFraudFinding { get; set; }
    }

    private sealed class FraudAggregatorExecutor :
        ReflectingExecutor<FraudAggregatorExecutor>,
        IMessageHandler<OSINTFinding, string>,           // ✅ Handle OSINT findings
        IMessageHandler<UserHistoryFinding, string>,      // ✅ Handle User History findings
        IMessageHandler<TransactionFraudFinding, string>  // ✅ Handle Transaction findings
    {
        private OSINTFinding? _osintFinding;
        private UserHistoryFinding? _userHistoryFinding;
        private TransactionFraudFinding? _transactionFinding;
        private int _receivedCount = 0;
        private const int ExpectedCount = 3;

        public FraudAggregatorExecutor() : base("FraudAggregator") { }

        // ✅ Handle OSINT Finding
        public ValueTask<string> HandleAsync(
            OSINTFinding finding, 
            IWorkflowContext context, 
            CancellationToken cancellationToken = default)
        {
            return HandleFindingAsync("OSINT", finding, context, cancellationToken, 
                () => { _osintFinding = finding; });
        }

        // ✅ Handle User History Finding
        public ValueTask<string> HandleAsync(
            UserHistoryFinding finding, 
            IWorkflowContext context, 
            CancellationToken cancellationToken = default)
        {
            return HandleFindingAsync("UserHistory", finding, context, cancellationToken, 
                () => { _userHistoryFinding = finding; });
        }

        // ✅ Handle Transaction Finding
        public ValueTask<string> HandleAsync(
            TransactionFraudFinding finding, 
            IWorkflowContext context, 
            CancellationToken cancellationToken = default)
        {
            return HandleFindingAsync("Transaction", finding, context, cancellationToken, 
                () => { _transactionFinding = finding; });
        }

        // ✅ Common handling logic
        private async ValueTask<string> HandleFindingAsync<T>(
            string sourceName,
            T finding,
            IWorkflowContext context,
            CancellationToken cancellationToken,
            Action storeFinding)
        {
            Console.WriteLine($"\n[Aggregator] ✅ HandleAsync CALLED from {sourceName}!");
            Console.WriteLine($"[Aggregator] Received finding {_receivedCount + 1}/{ExpectedCount}");
            
            storeFinding();  // Store the finding in local field
            _receivedCount++;

            // Wait for all fraud findings
            if (_receivedCount >= ExpectedCount)
            {
                Console.WriteLine("\n=== All Fraud Findings Collected (Fan-In) ===\n");
                
                // ✅ NOW store in state (Test06 pattern - state ops in aggregator work!)
                Console.WriteLine("[Aggregator] 💾 Storing all findings in shared state...");
                var state = await ReadFraudStateAsync(context);
                state.OSINTFinding = _osintFinding;
                state.UserHistoryFinding = _userHistoryFinding;
                state.TransactionFraudFinding = _transactionFinding;
                await SaveFraudStateAsync(context, state);
                
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✅ All findings stored in shared state!");
                Console.ResetColor();
                Console.WriteLine();
                
                // Reset for potential re-runs
                _osintFinding = null;
                _userHistoryFinding = null;
                _transactionFinding = null;
                _receivedCount = 0;
                
                return "[Fraud Analysis Complete] All findings collected and stored";
            }

            // Return null to signal workflow to wait for more inputs
            return null!;
        }
    }

    private sealed class FraudDecisionExecutor :
        ReflectingExecutor<FraudDecisionExecutor>,
        IMessageHandler<string, FraudDecision>
    {
        private readonly AIAgent _agent;
        public FraudDecisionExecutor(AIAgent agent) : base("FraudDecision") => _agent = agent;

        public async ValueTask<FraudDecision> HandleAsync(
            string _,
            IWorkflowContext context,
            CancellationToken cancellationToken = default)
        {
            var state = await ReadFraudStateAsync(context);
            
            Console.WriteLine("=== Final Fraud Decision ===\n");

            var prompt = $"""
                Make final fraud determination based on ALL findings:
                
                OSINT Finding:
                {JsonSerializer.Serialize(state.OSINTFinding, new JsonSerializerOptions { WriteIndented = true })}
                
                User History Finding:
                {JsonSerializer.Serialize(state.UserHistoryFinding, new JsonSerializerOptions { WriteIndented = true })}
                
                Transaction Fraud Finding:
                {JsonSerializer.Serialize(state.TransactionFraudFinding, new JsonSerializerOptions { WriteIndented = true })}
                
                Provide your final decision with reasoning.
                """;

            var response = await _agent.RunAsync(prompt, cancellationToken: cancellationToken);
            var decision = response.Deserialize<FraudDecision>(JsonSerializerOptions.Web);

            state.FraudDecision = decision;
            await SaveFraudStateAsync(context, state);

            // Print readable decision
            Console.WriteLine(new string('─', 80));
            Console.ForegroundColor = decision.IsFraud ? ConsoleColor.Red : ConsoleColor.Green;
            Console.WriteLine($"FRAUD DETERMINATION: {(decision.IsFraud ? "LIKELY FRAUD" : "NO FRAUD DETECTED")}");
            Console.ResetColor();
            Console.WriteLine($"Confidence: {decision.ConfidenceScore}%");
            Console.WriteLine($"Recommendation: {decision.Recommendation}");
            Console.WriteLine(new string('─', 80));
            Console.WriteLine();

            return decision;
        }
    }

    private sealed class OutcomePresenterExecutor :
        ReflectingExecutor<OutcomePresenterExecutor>,
        IMessageHandler<FraudDecision, string>
    {
        private readonly AIAgent _agent;
        public OutcomePresenterExecutor(AIAgent agent) : base("OutcomePresenter") => _agent = agent;

        public async ValueTask<string> HandleAsync(
            FraudDecision decision,
            IWorkflowContext context,
            CancellationToken cancellationToken = default)
        {
            var state = await ReadFraudStateAsync(context);
            
            Console.WriteLine("=== Generating Case Handler Email ===\n");

            var prompt = $"""
                Generate a professional email to the insurance case handler summarizing this fraud analysis:
                
                Claim Details:
                - Customer: {state.OriginalClaim?.CustomerId}
                - Type: {state.ClaimType} - {state.ClaimSubType}
                - Amount: ${state.OriginalClaim?.PurchasePrice}
                
                Fraud Analysis:
                {JsonSerializer.Serialize(state.FraudDecision, new JsonSerializerOptions { WriteIndented = true })}
                
                Include all key findings and provide clear next steps.
                """;

            var sb = new StringBuilder();
            await foreach (var up in _agent.RunStreamingAsync(new ChatMessage(ChatRole.User, prompt), cancellationToken: cancellationToken))
            {
                if (!string.IsNullOrEmpty(up.Text))
                {
                    sb.Append(up.Text);
                }
            }

            return sb.ToString();
        }
    }

    // --------------------- Mock Tools ---------------------
    
    private static class FraudMockTools
    {
        [Description("Check if stolen property is listed for sale on online marketplaces")]
        public static string CheckOnlineMarketplaces(
            [Description("Description of the stolen item")]
            string itemDescription,
            [Description("Approximate value of the item")]
            decimal itemValue)
        {
            // Mock logic: High-value items are "found" online (suspicious!)
            var found = itemValue > 1000;
            var fraudIndicator = found ? 85 : 15;

            var marketplaces = new[] { "ricardo.ch", "anibis.ch", "ebay.ch", "facebook_marketplace" };
            var result = new
            {
                marketplaces_checked = marketplaces,
                item_found = found,
                matching_listings = found
                    ? new[] { $"Ricardo.ch listing: Similar item, CHF {itemValue * 0.8m:F0} - Listed 3 days after reported theft" }
                    : Array.Empty<string>(),
                fraud_indicator = fraudIndicator
            };

            return JsonSerializer.Serialize(result);
        }

        [Description("Retrieve customer's claim history and fraud score")]
        public static string GetCustomerClaimHistory(
            [Description("Customer ID")]
            string customerId)
        {
            // Mock customer data
            var result = customerId switch
            {
                "CUST-10001" => new
                {
                    total_claims = 5,
                    claims_last_12_months = 3,
                    previous_fraud_flags = 1,
                    customer_fraud_score = 65,
                    claim_history = new[]
                    {
                        "2024-12: BikeTheft - APPROVED - $800",
                        "2024-09: WaterDamage - APPROVED - $1,500",
                        "2024-06: BikeTheft - FLAGGED (duplicate pattern) - $900",
                        "2023-12: Burglary - APPROVED - $2,000",
                        "2023-08: BikeTheft - APPROVED - $600"
                    }
                },
                "CUST-10002" => new
                {
                    total_claims = 2,
                    claims_last_12_months = 1,
                    previous_fraud_flags = 0,
                    customer_fraud_score = 20,
                    claim_history = new[]
                    {
                        "2024-10: Collision - APPROVED - $3,000",
                        "2022-03: Theft - APPROVED - $500"
                    }
                },
                _ => new
                {
                    total_claims = 1,
                    claims_last_12_months = 1,
                    previous_fraud_flags = 0,
                    customer_fraud_score = 10,
                    claim_history = new[] { "First claim" }
                }
            };

            return JsonSerializer.Serialize(result);
        }

        [Description("Analyze transaction risk profile for fraud indicators")]
        public static string GetTransactionRiskProfile(
            [Description("Claim amount")]
            decimal claimAmount,
            [Description("Date of loss")]
            string dateOfLoss)
        {
            // Mock logic: High value + recent = high risk
            var redFlags = new List<string>();
            var highValue = claimAmount > 1000;
            var recent = DateTime.TryParse(dateOfLoss, out var lossDate) &&
                        (DateTime.Now - lossDate).TotalDays < 7;

            if (highValue) redFlags.Add("High value claim (>$1000)");
            if (recent) redFlags.Add("Claim filed immediately after incident");
            if (highValue && recent) redFlags.Add("High value + immediate filing pattern");

            var riskScore = redFlags.Count * 25;

            var result = new
            {
                amount_percentile = highValue ? 85 : 35,
                timing_anomaly = recent,
                red_flags = redFlags.ToArray(),
                transaction_risk_score = riskScore
            };

            return JsonSerializer.Serialize(result);
        }
    }
}
