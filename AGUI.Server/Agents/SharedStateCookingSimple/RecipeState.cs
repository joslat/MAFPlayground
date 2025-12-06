// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
// Modified for MAFPlayground by Jose Luis Latorre

using System.Text.Json.Serialization;

namespace AGUI.Server.Agents.SharedStateCookingSimple;

/// <summary>
/// Complete recipe state shared between agent and React UI.
/// This state updates instantly in the UI when the agent modifies it.
/// </summary>
public sealed class RecipeState
{
    [JsonPropertyName("ingredients")]
    public List<Ingredient> Ingredients { get; set; } = new();

    [JsonPropertyName("instructions")]
    public List<Instruction> Instructions { get; set; } = new();

    [JsonPropertyName("dietaryPreferences")]
    public List<string> DietaryPreferences { get; set; } = new();

    [JsonPropertyName("cookingTimeMinutes")]
    public int CookingTimeMinutes { get; set; }

    [JsonPropertyName("servings")]
    public int Servings { get; set; }
}

/// <summary>
/// A single ingredient in the recipe.
/// </summary>
public sealed class Ingredient
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("quantity")]
    public string Quantity { get; set; } = string.Empty;

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty; // e.g., "vegetable", "protein", "grain", "spice"

    [JsonPropertyName("emoji")]
    public string Emoji { get; set; } = "??"; // Visual representation

    public Ingredient() { }

    public Ingredient(string name, string quantity, string category, string emoji = "??")
    {
        Name = name;
        Quantity = quantity;
        Category = category;
        Emoji = emoji;
    }
}

/// <summary>
/// A single cooking instruction step.
/// </summary>
public sealed class Instruction
{
    [JsonPropertyName("step")]
    public int Step { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("timeMinutes")]
    public int TimeMinutes { get; set; }

    public Instruction() { }

    public Instruction(int step, string description, int timeMinutes = 0)
    {
        Step = step;
        Description = description;
        TimeMinutes = timeMinutes;
    }
}

/// <summary>
/// Request to update recipe ingredients.
/// </summary>
public sealed class UpdateIngredientsRequest
{
    [JsonPropertyName("threadId")]
    public string ThreadId { get; set; } = string.Empty;

    [JsonPropertyName("ingredients")]
    public List<Ingredient> Ingredients { get; set; } = new();
}

/// <summary>
/// Request to update cooking instructions.
/// </summary>
public sealed class UpdateInstructionsRequest
{
    [JsonPropertyName("threadId")]
    public string ThreadId { get; set; } = string.Empty;

    [JsonPropertyName("instructions")]
    public List<Instruction> Instructions { get; set; } = new();
}

/// <summary>
/// Request to update dietary preferences.
/// </summary>
public sealed class UpdateDietaryPreferencesRequest
{
    [JsonPropertyName("threadId")]
    public string ThreadId { get; set; } = string.Empty;

    [JsonPropertyName("preferences")]
    public List<string> Preferences { get; set; } = new();
}

/// <summary>
/// Response from state update operations.
/// </summary>
public sealed class StateUpdateResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("updatedState")]
    public RecipeState? UpdatedState { get; set; }
}

/// <summary>
/// JSON serialization context for Recipe state types (required for source generation).
/// </summary>
[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(RecipeState))]
[JsonSerializable(typeof(Ingredient))]
[JsonSerializable(typeof(Instruction))]
[JsonSerializable(typeof(UpdateIngredientsRequest))]
[JsonSerializable(typeof(UpdateInstructionsRequest))]
[JsonSerializable(typeof(UpdateDietaryPreferencesRequest))]
[JsonSerializable(typeof(StateUpdateResponse))]
public sealed partial class RecipeJsonSerializerContext : JsonSerializerContext
{
}
