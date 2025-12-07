// SPDX-License-Identifier: LicenseRef-MAFPlayground-NPU-1.0-CH
// Copyright (c) 2025 Jose Luis

using System.ComponentModel;
using System.Text.Json.Serialization;
using Microsoft.Extensions.AI;

namespace MAFPlayground.Demos.ClaimsDemo;

/// <summary>
/// Shared data contracts for Claims Processing Workflows (Demo11, Demo12, etc.)
/// 
/// This file contains all shared data structures used across the claims demo series:
/// - CustomerInfo: Customer identification and contact info
/// - ClaimDraft: Core claim data gathered during intake
/// - ValidationResult: Output from validation agent (used as input to fraud detection)
/// - ClaimReadinessStatus: Enum for claim processing status
/// - ClaimWorkflowState: Demo11-specific state (for reference)
/// - IntakeDecision: Demo11-specific decision output
/// - ProcessedClaim: Demo11-specific final output
/// 
/// Data Flow:
/// Demo11 (Intake) ? ValidationResult ? Demo12 (Fraud Detection)
/// </summary>

// =====================================================================
// SHARED ACROSS ALL CLAIMS DEMOS
// =====================================================================

/// <summary>
/// Customer identification and contact information.
/// Shared between Demo11 (Claims Intake) and Demo12 (Fraud Detection).
/// </summary>
public sealed class CustomerInfo
{
    [JsonPropertyName("customer_id")]
    public string CustomerId { get; set; } = "";
    
    [JsonPropertyName("first_name")]
    public string FirstName { get; set; } = "";
    
    [JsonPropertyName("last_name")]
    public string LastName { get; set; } = "";
    
    [JsonPropertyName("email")]
    public string Email { get; set; } = "";
}

/// <summary>
/// Core claim data gathered during intake process.
/// Contains all the details about the claim incident.
/// Shared between Demo11 (Claims Intake) and Demo12 (Fraud Detection).
/// </summary>
public sealed class ClaimDraft
{
    [JsonPropertyName("claim_type")]
    public string ClaimType { get; set; } = ""; // e.g., "Property", "Auto", "Health"
    
    [JsonPropertyName("claim_sub_type")]
    public string ClaimSubType { get; set; } = ""; // e.g., "BikeTheft", "WaterDamage", "Accident"
    
    [JsonPropertyName("date_of_loss")]
    public string DateOfLoss { get; set; } = "";
    
    [JsonPropertyName("date_reported")]
    public string DateReported { get; set; } = "";
    
    [JsonPropertyName("short_description")]
    public string ShortDescription { get; set; } = "";
    
    [JsonPropertyName("item_description")]
    [Description("MANDATORY: Specific description of the item (e.g., 'Trek X-Caliber 8, red mountain bike')")]
    public string ItemDescription { get; set; } = "";
    
    [JsonPropertyName("detailed_description")]
    [Description("What happened during the incident (circumstances, location, etc.)")]
    public string DetailedDescription { get; set; } = "";
    
    [JsonPropertyName("purchase_price")]
    public decimal? PurchasePrice { get; set; }
}

/// <summary>
/// Validation result from Demo11's ClaimsReadyForProcessingAgent.
/// This is the OUTPUT of Demo11 and the INPUT to Demo12 (Fraud Detection).
/// 
/// Contains validated and normalized claim data ready for fraud analysis.
/// </summary>
[Description("Validated claim ready for fraud detection or processing")]
public sealed class ValidationResult
{
    [JsonPropertyName("ready")]
    [Description("True if claim is complete and ready for processing")]
    public bool Ready { get; set; }
    
    // ===== Claim Data Fields (NEW - from ClaimDraft) =====
    
    [JsonPropertyName("date_of_loss")]
    [Description("Date when the incident occurred")]
    public string? DateOfLoss { get; set; }
    
    [JsonPropertyName("date_reported")]
    [Description("Date when the claim was reported")]
    public string? DateReported { get; set; }
    
    [JsonPropertyName("short_description")]
    [Description("Brief 1-2 sentence description of the claim")]
    public string? ShortDescription { get; set; }
    
    [JsonPropertyName("item_description")]
    [Description("Specific description of the item (e.g., 'Trek X-Caliber 8, red mountain bike')")]
    public string? ItemDescription { get; set; }
    
    [JsonPropertyName("detailed_description")]
    [Description("Detailed description of what happened during the incident")]
    public string? DetailedDescription { get; set; }
    
    [JsonPropertyName("purchase_price")]
    [Description("Purchase price or value of the item")]
    public decimal? PurchasePrice { get; set; }
    
    // ===== Validation Metadata =====
    
    [JsonPropertyName("missing_fields")]
    [Description("List of missing or incomplete fields")]
    public List<string> MissingFields { get; set; } = new();
    
    [JsonPropertyName("blocking_issues")]
    [Description("Critical issues that block processing")]
    public List<string> BlockingIssues { get; set; } = new();
    
    [JsonPropertyName("suggested_questions")]
    [Description("Natural language questions to ask the user to fill gaps")]
    public List<string> SuggestedQuestions { get; set; } = new();
    
    // ===== Resolved/Normalized Data =====
    
    [JsonPropertyName("customer_id")]
    [Description("Resolved customer ID")]
    public string? CustomerId { get; set; }
    
    [JsonPropertyName("contract_id")]
    [Description("Resolved contract ID")]
    public string? ContractId { get; set; }
    
    [JsonPropertyName("normalized_claim_type")]
    [Description("Normalized claim type (e.g., 'Property', 'Auto')")]
    public string? NormalizedClaimType { get; set; }
    
    [JsonPropertyName("normalized_claim_sub_type")]
    [Description("Normalized claim sub-type (e.g., 'BikeTheft', 'WaterDamage')")]
    public string? NormalizedClaimSubType { get; set; }
}

/// <summary>
/// Status enum for claim processing workflow.
/// </summary>
public enum ClaimReadinessStatus
{
    Draft,
    PendingValidation,
    Ready,
    NeedsMoreInfo
}

// =====================================================================
// DEMO11-SPECIFIC (Claims Intake Workflow)
// =====================================================================

/// <summary>
/// Workflow state for Demo11 Claims Intake.
/// Tracks the entire intake process including conversation history.
/// </summary>
public sealed class ClaimWorkflowState
{
    public int IntakeIteration { get; set; } = 1;
    public ClaimReadinessStatus Status { get; set; } = ClaimReadinessStatus.Draft;
    public CustomerInfo? Customer { get; set; }
    public ClaimDraft ClaimDraft { get; set; } = new();
    public List<ChatMessage> ConversationHistory { get; } = new();
    public string? ContractId { get; set; }
}

/// <summary>
/// Decision output from Demo11's ClaimsUserFacingAgent (intake agent).
/// Determines if enough information has been gathered to proceed to validation.
/// </summary>
[Description("Result from the intake agent deciding if ready to proceed")]
public sealed class IntakeDecision
{
    [JsonPropertyName("ready_for_validation")]
    [Description("True if enough information gathered to start validation")]
    public bool ReadyForValidation { get; set; }
    
    [JsonPropertyName("response_to_user")]
    [Description("Message to show the user (question if more info needed, or confirmation if ready)")]
    public string ResponseToUser { get; set; } = "";
    
    [JsonPropertyName("customer_id")]
    [Description("Customer ID if provided")]
    public string? CustomerId { get; set; }
    
    [JsonPropertyName("first_name")]
    [Description("Customer first name if provided")]
    public string? FirstName { get; set; }
    
    [JsonPropertyName("last_name")]
    [Description("Customer last name if provided")]
    public string? LastName { get; set; }
    
    [JsonPropertyName("claim_draft")]
    [Description("Claim details extracted so far")]
    public ClaimDraft? ClaimDraft { get; set; }
}

/// <summary>
/// Final processed claim output from Demo11.
/// </summary>
public sealed class ProcessedClaim
{
    public string ClaimId { get; set; } = "";
    public string CustomerId { get; set; } = "";
    public string ContractId { get; set; } = "";
    public string Status { get; set; } = "";
    public string Summary { get; set; } = "";
}

// =====================================================================
// DEMO12-SPECIFIC (Fraud Detection Workflow)
// =====================================================================

/// <summary>
/// Workflow state for Demo12 Fraud Detection.
/// Tracks the entire fraud analysis process including findings from multiple agents.
/// </summary>
public sealed class FraudAnalysisState
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

/// <summary>
/// Initial data quality review result from Demo12's DataReviewAgent.
/// </summary>
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

/// <summary>
/// OSINT (Open Source Intelligence) validation result from Demo12's OSINTAgent.
/// </summary>
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

/// <summary>
/// Customer history and fraud score analysis from Demo12's UserHistoryAgent.
/// </summary>
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

/// <summary>
/// Transaction-level fraud scoring from Demo12's TransactionFraudAgent.
/// </summary>
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

/// <summary>
/// Final fraud determination from Demo12's FraudDecisionAgent.
/// </summary>
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
