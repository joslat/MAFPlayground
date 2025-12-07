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
