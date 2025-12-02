// SPDX-License-Identifier: LicenseRef-MAFPlayground-NPU-1.0-CH
// Copyright (c) 2025 Jose Luis Latorre

using System.Text.Json.Serialization;

namespace MAFPlayground.Samples.Sample22_WorkshopPlanner;

/// <summary>
/// Represents the initial workshop requirements extracted from natural language.
/// </summary>
public record WorkshopRequirements(
    [property: JsonPropertyName("goal")] string Goal,
    [property: JsonPropertyName("audience_level")] string AudienceLevel,
    [property: JsonPropertyName("duration_hours")] int DurationHours,
    [property: JsonPropertyName("focus_areas")] List<string> FocusAreas,
    [property: JsonPropertyName("prerequisites")] List<string> Prerequisites
);

/// <summary>
/// Represents the final workshop plan with structured modules.
/// </summary>
public record WorkshopPlan(
    [property: JsonPropertyName("workshop_title")] string WorkshopTitle,
    [property: JsonPropertyName("overview")] string Overview,
    [property: JsonPropertyName("total_duration_hours")] int TotalDurationHours,
    [property: JsonPropertyName("target_audience")] string TargetAudience,
    [property: JsonPropertyName("modules")] List<WorkshopModule> Modules,
    [property: JsonPropertyName("resources")] List<string> Resources,
    [property: JsonPropertyName("success_criteria")] List<string> SuccessCriteria
);

/// <summary>
/// Represents a single module within the workshop plan.
/// </summary>
public record WorkshopModule(
    [property: JsonPropertyName("module_number")] int ModuleNumber,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("duration_minutes")] int DurationMinutes,
    [property: JsonPropertyName("objectives")] List<string> Objectives,
    [property: JsonPropertyName("content_sources")] List<ContentSource> ContentSources,
    [property: JsonPropertyName("activities")] List<string> Activities
);

/// <summary>
/// Represents a content source used in a workshop module.
/// </summary>
public record ContentSource(
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("usage")] string Usage
);

/// <summary>
/// Represents the final markdown deliverable ready for file writing.
/// </summary>
public record MarkdownDeliverable(
    [property: JsonPropertyName("filename")] string Filename,
    [property: JsonPropertyName("content")] string Content
);

/// <summary>
/// Represents a candidate training component discovered from MCP servers.
/// </summary>
public record TrainingCandidate(
    [property: JsonPropertyName("source")] string Source, // "GitHub" or "MicrosoftLearn"
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("relevance_score")] double RelevanceScore
);

/// <summary>
/// Result from the discovery phase containing all discovered candidates.
/// </summary>
public record DiscoveryResult(
    [property: JsonPropertyName("candidates")] List<TrainingCandidate> Candidates,
    [property: JsonPropertyName("total_discovered")] int TotalDiscovered
);

/// <summary>
/// Represents an enriched training component with detailed metadata.
/// </summary>
public record EnrichedComponent(
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("source")] string Source,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("topics_covered")] List<string> TopicsCovered,
    [property: JsonPropertyName("estimated_time_minutes")] int EstimatedTimeMinutes,
    [property: JsonPropertyName("difficulty_level")] string DifficultyLevel,
    [property: JsonPropertyName("hands_on_exercises")] bool HandsOnExercises,
    [property: JsonPropertyName("last_updated")] string LastUpdated
);

/// <summary>
/// Represents the evaluation result for an enriched component.
/// </summary>
public record EvaluationResult(
    [property: JsonPropertyName("component_title")] string ComponentTitle,
    [property: JsonPropertyName("component_url")] string ComponentUrl,
    [property: JsonPropertyName("approved")] bool Approved,
    [property: JsonPropertyName("alignment_score")] double AlignmentScore,
    [property: JsonPropertyName("reasoning")] string Reasoning,
    [property: JsonPropertyName("suggested_use")] string? SuggestedUse // null if rejected
);

/// <summary>
/// State object for managing the enrichment and evaluation loop.
/// </summary>
public record ComponentEvaluationState(
    [property: JsonPropertyName("pending_candidates")] List<TrainingCandidate> PendingCandidates,
    [property: JsonPropertyName("current_candidate")] TrainingCandidate? CurrentCandidate,
    [property: JsonPropertyName("evaluation_results")] List<EvaluationResult> EvaluationResults,
    [property: JsonPropertyName("has_pending_components")] bool HasPendingComponents
);

/// <summary>
/// Aggregated results from all evaluations.
/// </summary>
public record AggregatedComponents(
    [property: JsonPropertyName("approved_components")] List<(EnrichedComponent Component, EvaluationResult Evaluation)> ApprovedComponents,
    [property: JsonPropertyName("total_evaluated")] int TotalEvaluated,
    [property: JsonPropertyName("acceptance_rate")] double AcceptanceRate
);
