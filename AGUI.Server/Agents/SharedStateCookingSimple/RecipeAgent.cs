// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
// Modified for MAFPlayground by Jose Luis Latorre

using System.ComponentModel;
using System.Collections.Concurrent;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace AGUI.Server.Agents.SharedStateCookingSimple;

/// <summary>
/// Recipe Assistant Agent with Shared State support.
/// Demonstrates AG-UI's instant state updates feature - when the agent modifies state,
/// the React UI updates immediately without network latency.
/// </summary>
public static class RecipeAgent
{
    // In-memory state store (per thread/conversation)
    // In production, use a distributed cache or database
    private static readonly ConcurrentDictionary<string, RecipeState> _stateStore = new();

    public static AIAgent Create(IChatClient chatClient, Microsoft.AspNetCore.Http.Json.JsonOptions jsonOptions)
    {
        // Define tools for reading and writing shared state
        AITool[] tools =
        [
            AIFunctionFactory.Create(GetRecipeState, serializerOptions: jsonOptions.SerializerOptions),
            AIFunctionFactory.Create(UpdateIngredients, serializerOptions: jsonOptions.SerializerOptions),
            AIFunctionFactory.Create(UpdateInstructions, serializerOptions: jsonOptions.SerializerOptions),
            AIFunctionFactory.Create(UpdateDietaryPreferences, serializerOptions: jsonOptions.SerializerOptions),
            AIFunctionFactory.Create(UpdateCookingDetails, serializerOptions: jsonOptions.SerializerOptions)
        ];

        return chatClient.CreateAIAgent(
            name: "RecipeAssistant",
            instructions: """
                You are a helpful recipe assistant with expertise in cooking and nutrition.
                
                You can help users:
                1. View their current recipe (ingredients, instructions, dietary preferences)
                2. Modify ingredients (add, remove, or substitute)
                3. Update cooking instructions
                4. Adjust for dietary preferences (vegetarian, vegan, gluten-free, etc.)
                5. Scale recipes (change servings)
                
                IMPORTANT: When making changes, ALWAYS use the update tools to modify the shared state.
                The UI will update instantly when you call these tools.
                
                Available Tools:
                - GetRecipeState: Read the current recipe state
                - UpdateIngredients: Add/remove/modify ingredients
                - UpdateInstructions: Add/remove/modify cooking steps
                - UpdateDietaryPreferences: Set dietary constraints
                - UpdateCookingDetails: Change cooking time or servings
                
                When the user asks for changes:
                1. First call GetRecipeState to see the current recipe
                2. Make your modifications
                3. Call the appropriate Update tool
                4. Confirm the changes to the user
                
                Be conversational, helpful, and provide cooking tips when relevant.
                """,
            tools: tools);
    }

    public static string GetDescription()
    {
        return "Recipe assistant with shared state - instant UI updates";
    }

    // ====================================
    // TOOL: Read Shared State
    // ====================================

    /// <summary>
    /// Get the current recipe state for a conversation thread.
    /// </summary>
    [Description("Get the current recipe state including ingredients, instructions, and dietary preferences")]
    private static RecipeState GetRecipeState(
        [Description("Conversation thread ID")] string threadId)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"[Tool] GetRecipeState called for thread: {threadId}");
        Console.ResetColor();

        // Get existing state or return default
        if (_stateStore.TryGetValue(threadId, out var state))
        {
            Console.WriteLine($"  → Found existing recipe with {state.Ingredients.Count} ingredients");
            return state;
        }

        // Return a default recipe to start
        var defaultState = new RecipeState
        {
            Ingredients = new List<Ingredient>
            {
                new("Carrots", "2 cups", "vegetable", "🥕"),
                new("Wheat flour", "1 cup", "grain", "🌾"),
                new("Olive oil", "2 tbsp", "oil", "🫒")
            },
            Instructions = new List<Instruction>
            {
                new(1, "Preheat oven to 350°F (175°C)", 5),
                new(2, "Mix ingredients in a bowl", 5)
            },
            DietaryPreferences = new List<string> { "vegetarian" },
            CookingTimeMinutes = 30,
            Servings = 4
        };

        _stateStore[threadId] = defaultState;
        Console.WriteLine($"  → Created default recipe");
        return defaultState;
    }

    // ====================================
    // TOOL: Write Shared State - Ingredients
    // ====================================

    /// <summary>
    /// Update the recipe ingredients. This triggers instant UI update.
    /// </summary>
    [Description("Add, remove, or update ingredients in the recipe")]
    private static StateUpdateResponse UpdateIngredients(
        [Description("The update request with thread ID and new ingredients")] UpdateIngredientsRequest request)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"[Tool] UpdateIngredients called for thread: {request.ThreadId}");
        Console.WriteLine($"  → Updating to {request.Ingredients.Count} ingredients");
        Console.ResetColor();

        // Get or create state
        var state = _stateStore.GetOrAdd(request.ThreadId, _ => new RecipeState());

        // Update ingredients
        state.Ingredients = request.Ingredients;

        // Store updated state
        _stateStore[request.ThreadId] = state;

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"  ✓ Ingredients updated successfully");
        Console.ResetColor();

        return new StateUpdateResponse
        {
            Success = true,
            Message = $"Updated {request.Ingredients.Count} ingredients",
            UpdatedState = state
        };
    }

    // ====================================
    // TOOL: Write Shared State - Instructions
    // ====================================

    /// <summary>
    /// Update the cooking instructions. This triggers instant UI update.
    /// </summary>
    [Description("Add, remove, or update cooking instructions")]
    private static StateUpdateResponse UpdateInstructions(
        [Description("The update request with thread ID and new instructions")] UpdateInstructionsRequest request)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"[Tool] UpdateInstructions called for thread: {request.ThreadId}");
        Console.WriteLine($"  → Updating to {request.Instructions.Count} steps");
        Console.ResetColor();

        // Get or create state
        var state = _stateStore.GetOrAdd(request.ThreadId, _ => new RecipeState());

        // Update instructions
        state.Instructions = request.Instructions;

        // Store updated state
        _stateStore[request.ThreadId] = state;

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"  ✓ Instructions updated successfully");
        Console.ResetColor();

        return new StateUpdateResponse
        {
            Success = true,
            Message = $"Updated {request.Instructions.Count} cooking steps",
            UpdatedState = state
        };
    }

    // ====================================
    // TOOL: Write Shared State - Dietary Preferences
    // ====================================

    /// <summary>
    /// Update dietary preferences (vegetarian, vegan, gluten-free, etc.).
    /// </summary>
    [Description("Set dietary preferences like vegetarian, vegan, low-carb, gluten-free, etc.")]
    private static StateUpdateResponse UpdateDietaryPreferences(
        [Description("The update request with thread ID and new preferences")] UpdateDietaryPreferencesRequest request)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"[Tool] UpdateDietaryPreferences called for thread: {request.ThreadId}");
        Console.WriteLine($"  → Setting preferences: {string.Join(", ", request.Preferences)}");
        Console.ResetColor();

        // Get or create state
        var state = _stateStore.GetOrAdd(request.ThreadId, _ => new RecipeState());

        // Update preferences
        state.DietaryPreferences = request.Preferences;

        // Store updated state
        _stateStore[request.ThreadId] = state;

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"  ✓ Dietary preferences updated successfully");
        Console.ResetColor();

        return new StateUpdateResponse
        {
            Success = true,
            Message = $"Updated dietary preferences: {string.Join(", ", request.Preferences)}",
            UpdatedState = state
        };
    }

    // ====================================
    // TOOL: Write Shared State - Cooking Details
    // ====================================

    /// <summary>
    /// Update cooking time or servings.
    /// </summary>
    [Description("Update cooking time (in minutes) or number of servings")]
    private static StateUpdateResponse UpdateCookingDetails(
        [Description("Conversation thread ID")] string threadId,
        [Description("Total cooking time in minutes (optional)")] int? cookingTimeMinutes = null,
        [Description("Number of servings (optional)")] int? servings = null)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"[Tool] UpdateCookingDetails called for thread: {threadId}");
        if (cookingTimeMinutes.HasValue)
            Console.WriteLine($"  → Cooking time: {cookingTimeMinutes.Value} minutes");
        if (servings.HasValue)
            Console.WriteLine($"  → Servings: {servings.Value}");
        Console.ResetColor();

        // Get or create state
        var state = _stateStore.GetOrAdd(threadId, _ => new RecipeState());

        // Update details
        if (cookingTimeMinutes.HasValue)
            state.CookingTimeMinutes = cookingTimeMinutes.Value;
        if (servings.HasValue)
            state.Servings = servings.Value;

        // Store updated state
        _stateStore[threadId] = state;

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"  ✓ Cooking details updated successfully");
        Console.ResetColor();

        var message = $"Updated cooking details: ";
        if (cookingTimeMinutes.HasValue && servings.HasValue)
            message += $"{cookingTimeMinutes.Value} minutes, {servings.Value} servings";
        else if (cookingTimeMinutes.HasValue)
            message += $"{cookingTimeMinutes.Value} minutes";
        else if (servings.HasValue)
            message += $"{servings.Value} servings";

        return new StateUpdateResponse
        {
            Success = true,
            Message = message,
            UpdatedState = state
        };
    }
}
