// SPDX-License-Identifier: LicenseRef-MAFPlayground-NPU-1.0-CH
// Copyright (c) 2025 Jose Luis

using System.ComponentModel;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.AI.OpenAI;
using MAFPlayground.Utils;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Reflection;
using Microsoft.Extensions.AI;

namespace MAFPlayground.Demos;

/// <summary>
/// Demo 12: Claims Fraud Detection Workflow
/// 
/// Takes a validated claim (ValidationResult from Demo11) and processes it through
/// a comprehensive fraud detection pipeline:
/// 
/// 1. DataReviewExecutor - Initial data quality and completeness check
/// 2. ClassificationRouter - Routes to appropriate claim type handler (Property Theft for now)
/// 3. PropertyTheftFanOut - Dispatches to 3 parallel fraud detection agents:
///    - OSINTValidatorAgent: Checks if stolen property is listed for sale online
///    - UserValidationAgent: Analyzes customer's claim history and fraud score
///    - TransactionFraudScoringAgent: Scores the transaction for fraud indicators
/// 4. FraudAggregatorExecutor - Collects all findings into shared state
/// 5. FraudDecisionAgent - Analyzes aggregated data and makes final fraud determination
/// 6. OutcomePresenterAgent - Generates professional email to insurance representative
/// 
/// Workflow Flow:
/// ???????????????????????
/// ? DataReview          ? ? Quality check
/// ???????????????????????
///        ?
/// ???????????????????????
/// ? Classification      ? ? Route by claim type
/// ???????????????????????
///        ? (Property Theft path)
/// ???????????????????????????????????????
/// ?   Parallel Fraud Detection (Fan-Out)?
/// ?  ???????????????? ??????????????????
/// ?  ?OSINT Checker ? ?User Validator ??
/// ?  ???????????????? ??????????????????
/// ?  ????????????????????????????????  ?
/// ?  ?Transaction Fraud Scorer      ?  ?
/// ?  ????????????????????????????????  ?
/// ???????????????????????????????????????
///        ? (Fan-In)
/// ???????????????????????
/// ? FraudAggregator     ? ? Consolidate findings
/// ???????????????????????
///        ?
/// ???????????????????????
/// ? FraudDecisionAgent  ? ? Final determination
/// ???????????????????????
///        ?
/// ???????????????????????
/// ? OutcomePresenter    ? ? Professional email
/// ???????????????????????
/// 
/// Key Features:
/// - Shared state pattern for aggregating fraud signals
/// - Concurrent fraud detection from multiple perspectives
/// - Mock tools with hardcoded fraud indicators
/// - Structured decision-making with confidence scores
/// - Professional email generation for case handlers
/// </summary>
internal static class Demo12_ClaimsFraudDetection
{
    // --------------------- Shared state ---------------------
    private sealed class FraudAnalysisState
    {
        public ValidationResult? OriginalClaim { get; set; }
        public DataReviewResult? DataReview { get; set; }
        public string ClaimType { get; set; } = "";
        public string ClaimSubType { get; set; } = "";
        
        // Fraud detection findings (fan-in from 3 agents)
        public OSINTFinding? OSINTFinding { get; set; }
        public UserHistoryFinding? UserHistoryFinding { get; set; }
        public TransactionFraudFinding? TransactionFraudFinding { get; set; }
        
        // Final decision
        public FraudDecision? FraudDecision { get; set; }
    }

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
    
    /// <summary>Validation result from Demo11 - extended for fraud analysis</summary>
    [Description("Validated claim ready for fraud detection")]
    public sealed class ValidationResult
    {
        [JsonPropertyName("ready")]
        public bool Ready { get; set; }
        
        [JsonPropertyName("customer_id")]
        public string? CustomerId { get; set; }
        
        [JsonPropertyName("contract_id")]
        public string? ContractId { get; set; }
        
        [JsonPropertyName("normalized_claim_type")]
        public string? NormalizedClaimType { get; set; }
        
        [JsonPropertyName("normalized_claim_sub_type")]
        public string? NormalizedClaimSubType { get; set; }
        
        [JsonPropertyName("date_of_loss")]
        public string? DateOfLoss { get; set; }
        
        [JsonPropertyName("detailed_description")]
        public string? DetailedDescription { get; set; }
        
        [JsonPropertyName("purchase_price")]
        public decimal? PurchasePrice { get; set; }
    }

    [Description("Initial data quality review result")]
    public sealed class DataReviewResult
    {
        [JsonPropertyName("data_complete")]
        [Description("Whether all required fields are present and well-formed")]
        public bool DataComplete { get; set; }
        
        [JsonPropertyName("quality_score")]
        [Description("Overall data quality score 0-100")]
        public int QualityScore { get; set; }
        
        [JsonPropertyName("concerns")]
        [Description("List of data quality concerns")]
        public List<string> Concerns { get; set; } = new();
        
        [JsonPropertyName("proceed")]
        [Description("Whether to proceed with fraud analysis")]
        public bool Proceed { get; set; }
    }

    [Description("OSINT (Open Source Intelligence) validation result")]
    public sealed class OSINTFinding
    {
        [JsonPropertyName("item_found_online")]
        [Description("Whether the reported stolen item was found listed for sale")]
        public bool ItemFoundOnline { get; set; }
        
        [JsonPropertyName("marketplaces_checked")]
        [Description("List of online marketplaces checked")]
        public List<string> MarketplacesChecked { get; set; } = new();
        
        [JsonPropertyName("matching_listings")]
        [Description("Details of matching listings found")]
        public List<string> MatchingListings { get; set; } = new();
        
        [JsonPropertyName("fraud_indicator_score")]
        [Description("Score 0-100 indicating likelihood of fraud")]
        public int FraudIndicatorScore { get; set; }
        
        [JsonPropertyName("summary")]
        [Description("Summary of OSINT findings")]
        public string Summary { get; set; } = "";
    }

    [Description("Customer history and fraud score analysis")]
    public sealed class UserHistoryFinding
    {
        [JsonPropertyName("previous_claims_count")]
        [Description("Number of previous claims filed")]
        public int PreviousClaimsCount { get; set; }
        
        [JsonPropertyName("suspicious_activity_detected")]
        [Description("Whether suspicious patterns were detected")]
        public bool SuspiciousActivityDetected { get; set; }
        
        [JsonPropertyName("customer_fraud_score")]
        [Description("Historical fraud score for customer 0-100")]
        public int CustomerFraudScore { get; set; }
        
        [JsonPropertyName("claim_history")]
        [Description("Summary of past claims and their outcomes")]
        public List<string> ClaimHistory { get; set; } = new();
        
        [JsonPropertyName("summary")]
        [Description("Summary of user history analysis")]
        public string Summary { get; set; } = "";
    }

    [Description("Transaction-level fraud scoring")]
    public sealed class TransactionFraudFinding
    {
        [JsonPropertyName("anomaly_score")]
        [Description("Transaction anomaly score 0-100")]
        public int AnomalyScore { get; set; }
        
        [JsonPropertyName("red_flags")]
        [Description("List of fraud red flags detected")]
        public List<string> RedFlags { get; set; } = new();
        
        [JsonPropertyName("transaction_fraud_score")]
        [Description("Overall transaction fraud score 0-100")]
        public int TransactionFraudScore { get; set; }
        
        [JsonPropertyName("summary")]
        [Description("Summary of transaction analysis")]
        public string Summary { get; set; } = "";
    }

    [Description("Final fraud determination")]
    public sealed class FraudDecision
    {
        [JsonPropertyName("is_fraud")]
        [Description("Final determination: Is this likely fraud?")]
        public bool IsFraud { get; set; }
        
        [JsonPropertyName("confidence_score")]
        [Description("Confidence in the decision 0-100")]
        public int ConfidenceScore { get; set; }
        
        [JsonPropertyName("combined_fraud_score")]
        [Description("Combined fraud score from all sources 0-100")]
        public int CombinedFraudScore { get; set; }
        
        [JsonPropertyName("recommendation")]
        [Description("Recommended action (APPROVE, INVESTIGATE, REJECT)")]
        public string Recommendation { get; set; } = "";
        
        [JsonPropertyName("reasoning")]
        [Description("Explanation of the decision")]
        public string Reasoning { get; set; } = "";
        
        [JsonPropertyName("key_factors")]
        [Description("Key factors that influenced the decision")]
        public List<string> KeyFactors { get; set; } = new();
    }

    // --------------------- Entry point ---------------------
    public static async Task Execute()
    {
        Console.WriteLine("=== Demo 12: Claims Fraud Detection Workflow ===\n");
        Console.WriteLine("This demo analyzes validated claims for potential fraud indicators.\n");

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
                    Console.WriteLine("? FRAUD ANALYSIS COMPLETE");
                    Console.ResetColor();
                    Console.WriteLine(new string('=', 80));
                    Console.WriteLine();
                    Console.WriteLine(output.Data);
                    Console.WriteLine();
                    Console.WriteLine(new string('=', 80));
                    break;
            }
        }

        Console.WriteLine("\n? Demo 12 Complete!\n");
        Console.WriteLine("Key Concepts Demonstrated:");
        Console.WriteLine("  ? Data quality review before fraud analysis");
        Console.WriteLine("  ? Classification routing by claim type");
        Console.WriteLine("  ? Parallel fraud detection (fan-out/fan-in pattern)");
        Console.WriteLine("  ? OSINT validation with mock marketplace data");
        Console.WriteLine("  ? Customer history and fraud score analysis");
        Console.WriteLine("  ? Transaction-level fraud scoring");
        Console.WriteLine("  ? Shared state for aggregating findings");
        Console.WriteLine("  ? AI-powered fraud decision with confidence scores");
        Console.WriteLine("  ? Professional email generation for case handlers\n");
    }

    private static Workflow BuildFraudDetectionWorkflow(IChatClient chatClient)
    {
        // Mock tools for fraud detection
        var tools = new List<AITool>
        {
            AIFunctionFactory.Create(FraudMockTools.CheckOnlineMarketplaces),
            AIFunctionFactory.Create(FraudMockTools.GetCustomerClaimHistory),
            AIFunctionFactory.Create(FraudMockTools.GetTransactionRiskProfile)
        };

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
                Value: ${claim.PurchasePrice}
                
                Description: {claim.DetailedDescription}
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
            
            await context.SendMessageAsync(routeType, cancellationToken: cancellationToken);
        }
    }

    private sealed class OSINTExecutor :
        ReflectingExecutor<OSINTExecutor>,
        IMessageHandler<string, OSINTFinding>
    {
        private readonly AIAgent _agent;
        public OSINTExecutor(AIAgent agent) : base("OSINT") => _agent = agent;

        public async ValueTask<OSINTFinding> HandleAsync(
            string _,
            IWorkflowContext context,
            CancellationToken cancellationToken = default)
        {
            var state = await ReadFraudStateAsync(context);
            var claim = state.OriginalClaim!;
            
            Console.WriteLine("=== OSINT Validation (Online Marketplaces) ===\n");

            var prompt = $"""
                Check if this stolen item is listed for sale online:
                
                Item: {claim.DetailedDescription}
                Value: ${claim.PurchasePrice}
                Date of Loss: {claim.DateOfLoss}
                
                Use check_online_marketplaces tool to search.
                """;

            var response = await _agent.RunAsync(prompt, cancellationToken: cancellationToken);
            var finding = response.Deserialize<OSINTFinding>(JsonSerializerOptions.Web);

            state.OSINTFinding = finding;
            await SaveFraudStateAsync(context, state);

            Console.WriteLine($"? OSINT Check Complete - Fraud Score: {finding.FraudIndicatorScore}/100\n");

            return finding;
        }
    }

    private sealed class UserHistoryExecutor :
        ReflectingExecutor<UserHistoryExecutor>,
        IMessageHandler<string, UserHistoryFinding>
    {
        private readonly AIAgent _agent;
        public UserHistoryExecutor(AIAgent agent) : base("UserHistory") => _agent = agent;

        public async ValueTask<UserHistoryFinding> HandleAsync(
            string _,
            IWorkflowContext context,
            CancellationToken cancellationToken = default)
        {
            var state = await ReadFraudStateAsync(context);
            var claim = state.OriginalClaim!;
            
            Console.WriteLine("=== User History Analysis ===\n");

            var prompt = $"""
                Analyze this customer's claim history for fraud patterns:
                
                Customer ID: {claim.CustomerId}
                Current Claim Type: {claim.NormalizedClaimType} - {claim.NormalizedClaimSubType}
                
                Use get_customer_claim_history tool to retrieve past claims.
                """;

            var response = await _agent.RunAsync(prompt, cancellationToken: cancellationToken);
            var finding = response.Deserialize<UserHistoryFinding>(JsonSerializerOptions.Web);

            state.UserHistoryFinding = finding;
            await SaveFraudStateAsync(context, state);

            Console.WriteLine($"? User History Check Complete - Customer Fraud Score: {finding.CustomerFraudScore}/100\n");

            return finding;
        }
    }

    private sealed class TransactionFraudExecutor :
        ReflectingExecutor<TransactionFraudExecutor>,
        IMessageHandler<string, TransactionFraudFinding>
    {
        private readonly AIAgent _agent;
        public TransactionFraudExecutor(AIAgent agent) : base("TransactionFraud") => _agent = agent;

        public async ValueTask<TransactionFraudFinding> HandleAsync(
            string _,
            IWorkflowContext context,
            CancellationToken cancellationToken = default)
        {
            var state = await ReadFraudStateAsync(context);
            var claim = state.OriginalClaim!;
            
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

            state.TransactionFraudFinding = finding;
            await SaveFraudStateAsync(context, state);

            Console.WriteLine($"? Transaction Analysis Complete - Fraud Score: {finding.TransactionFraudScore}/100\n");

            return finding;
        }
    }

    private sealed class FraudAggregatorExecutor :
        ReflectingExecutor<FraudAggregatorExecutor>,
        IMessageHandler<string, string>
    {
        private readonly List<string> _confirmations = new();
        private const int ExpectedCount = 3;

        public FraudAggregatorExecutor() : base("FraudAggregator") { }

        public async ValueTask<string> HandleAsync(
            string confirmation, 
            IWorkflowContext context, 
            CancellationToken cancellationToken = default)
        {
            _confirmations.Add(confirmation);
            Console.WriteLine($"[FraudAggregator] Received confirmation {_confirmations.Count}/{ExpectedCount}");
            Console.WriteLine($"  ? {confirmation}");

            // Wait for all fraud findings
            if (_confirmations.Count >= ExpectedCount)
            {
                Console.WriteLine("\n=== All Fraud Findings Collected (Fan-In) ===\n");
                
                // Verify all findings are in shared state
                var state = await ReadFraudStateAsync(context);
                var allPresent = state.OSINTFinding != null && 
                                state.UserHistoryFinding != null && 
                                state.TransactionFraudFinding != null;
                
                if (!allPresent)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("??  Warning: Not all findings were stored in shared state!");
                    Console.ResetColor();
                }
                
                var summary = string.Join("\n", _confirmations);
                
                // Reset for potential re-runs
                _confirmations.Clear();
                
                return $"[Fraud Analysis Complete]\n{summary}\n[All findings verified in shared state - proceeding to decision]";
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
            Console.WriteLine(new string('?', 80));
            Console.ForegroundColor = decision.IsFraud ? ConsoleColor.Red : ConsoleColor.Green;
            Console.WriteLine($"FRAUD DETERMINATION: {(decision.IsFraud ? "LIKELY FRAUD" : "NO FRAUD DETECTED")}");
            Console.ResetColor();
            Console.WriteLine($"Confidence: {decision.ConfidenceScore}%");
            Console.WriteLine($"Recommendation: {decision.Recommendation}");
            Console.WriteLine(new string('?', 80));
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
}

// --------------------- Mock Fraud Detection Tools ---------------------
internal static class FraudMockTools
{
    [Description("Check online marketplaces for stolen items listed for sale")]
    public static string CheckOnlineMarketplaces(
        [Description("Description of the stolen item")] string itemDescription,
        [Description("Approximate value of the item")] decimal value)
    {
        Console.WriteLine($"?? Tool called: check_online_marketplaces('{itemDescription}', ${value})");
        
        // Mock: Simulate finding the item on ricardo.ch (Swiss marketplace)
        var found = value > 1000; // Items over $1000 are "found" (suspicious)
        
        var result = new
        {
            marketplaces_checked = new[] { "ricardo.ch", "anibis.ch", "ebay.ch", "facebook_marketplace" },
            item_found = found,
            matching_listings = found ? new[]
            {
                "Ricardo.ch listing: Red mountain bike, Trek brand, CHF 950 - Listed 3 days after reported theft",
                "Anibis.ch listing: Mountain bike accessories - Similar description"
            } : Array.Empty<string>(),
            fraud_indicator = found ? 85 : 15
        };

        return JsonSerializer.Serialize(result);
    }

    [Description("Get customer's claim history and fraud indicators")]
    public static string GetCustomerClaimHistory(
        [Description("Customer ID")] string customerId)
    {
        Console.WriteLine($"?? Tool called: get_customer_claim_history('{customerId}')");
        
        // Mock data: CUST-10001 has suspicious history
        var mockHistory = new Dictionary<string, object>
        {
            ["CUST-10001"] = new
            {
                customer_id = "CUST-10001",
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
            ["CUST-10002"] = new
            {
                customer_id = "CUST-10002",
                total_claims = 2,
                claims_last_12_months = 1,
                previous_fraud_flags = 0,
                customer_fraud_score = 20,
                claim_history = new[]
                {
                    "2024-10: Collision - APPROVED - $3,000",
                    "2022-03: Theft - APPROVED - $500"
                }
            }
        };

        if (mockHistory.TryGetValue(customerId, out var history))
        {
            return JsonSerializer.Serialize(history);
        }

        return JsonSerializer.Serialize(new
        {
            customer_id = customerId,
            total_claims = 0,
            claims_last_12_months = 0,
            previous_fraud_flags = 0,
            customer_fraud_score = 0,
            claim_history = Array.Empty<string>()
        });
    }

    [Description("Get transaction risk profile and anomaly indicators")]
    public static string GetTransactionRiskProfile(
        [Description("Claim amount")] decimal amount,
        [Description("Date of loss")] string dateOfLoss)
    {
        Console.WriteLine($"?? Tool called: get_transaction_risk_profile(${amount}, '{dateOfLoss}')");
        
        // Mock risk factors
        var highValue = amount > 1000;
        var recentClaim = DateTime.TryParse(dateOfLoss, out var lossDate) && 
                          (DateTime.Now - lossDate).TotalDays < 7;

        var redFlags = new List<string>();
        if (highValue) redFlags.Add("High value claim (>$1000)");
        if (recentClaim) redFlags.Add("Claim filed immediately after incident");
        if (highValue && recentClaim) redFlags.Add("High value + immediate filing pattern");

        var result = new
        {
            amount_percentile = highValue ? 85 : 45, // Percentile vs other claims
            timing_anomaly = recentClaim,
            red_flags = redFlags.ToArray(),
            transaction_risk_score = redFlags.Count * 25 // 0, 25, 50, or 75
        };

        return JsonSerializer.Serialize(result);
    }
}
