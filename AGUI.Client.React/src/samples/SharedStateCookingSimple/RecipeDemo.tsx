import { useState } from "react";
import { useCopilotReadable, useCopilotAction } from "@copilotkit/react-core";
import "./styles.css";

// Type definitions matching backend models
interface Ingredient {
  name: string;
  quantity: string;
  category: string;
  emoji: string;
}

interface Instruction {
  step: number;
  description: string;
  timeMinutes: number;
}

export function RecipeDemo() {
  // Recipe State - Shared with Agent
  const [ingredients, setIngredients] = useState<Ingredient[]>([
    { name: "Carrots", quantity: "2 cups", category: "vegetable", emoji: "??" },
    { name: "Wheat flour", quantity: "1 cup", category: "grain", emoji: "??" },
    { name: "Olive oil", quantity: "2 tbsp", category: "oil", emoji: "??" }
  ]);

  const [instructions, setInstructions] = useState<Instruction[]>([
    { step: 1, description: "Preheat oven to 350°F (175°C)", timeMinutes: 5 },
    { step: 2, description: "Mix ingredients in a bowl", timeMinutes: 5 }
  ]);

  const [dietaryPreferences, setDietaryPreferences] = useState<string[]>([
    "vegetarian"
  ]);

  const [cookingTimeMinutes, setCookingTimeMinutes] = useState<number>(30);
  const [servings, setServings] = useState<number>(4);

  // SHARED STATE: Make state readable by agent
  useCopilotReadable({
    description: "The current recipe state including all ingredients, instructions, and preferences",
    value: {
      ingredients,
      instructions,
      dietaryPreferences,
      cookingTimeMinutes,
      servings
    }
  });

  // SHARED STATE: Make ingredients writable by agent
  useCopilotAction({
    name: "updateIngredients",
    description: "Update the recipe ingredients list",
    parameters: [
      {
        name: "ingredients",
        type: "object[]",
        description: "Array of ingredient objects with name, quantity, category, and emoji",
        required: true
      }
    ],
    handler: async ({ ingredients: newIngredients }) => {
      console.log("Agent updating ingredients:", newIngredients);
      setIngredients(newIngredients);
    }
  });

  // SHARED STATE: Make instructions writable by agent
  useCopilotAction({
    name: "updateInstructions",
    description: "Update the cooking instructions",
    parameters: [
      {
        name: "instructions",
        type: "object[]",
        description: "Array of instruction objects with step, description, and timeMinutes",
        required: true
      }
    ],
    handler: async ({ instructions: newInstructions }) => {
      console.log("Agent updating instructions:", newInstructions);
      setInstructions(newInstructions);
    }
  });

  // SHARED STATE: Make dietary preferences writable by agent
  useCopilotAction({
    name: "updateDietaryPreferences",
    description: "Update dietary preferences",
    parameters: [
      {
        name: "preferences",
        type: "string[]",
        description: "Array of dietary preference tags",
        required: true
      }
    ],
    handler: async ({ preferences: newPreferences }) => {
      console.log("Agent updating dietary preferences:", newPreferences);
      setDietaryPreferences(newPreferences);
    }
  });

  // SHARED STATE: Make cooking details writable by agent
  useCopilotAction({
    name: "updateCookingDetails",
    description: "Update cooking time or servings",
    parameters: [
      {
        name: "cookingTimeMinutes",
        type: "number",
        description: "Total cooking time in minutes",
        required: false
      },
      {
        name: "servings",
        type: "number",
        description: "Number of servings",
        required: false
      }
    ],
    handler: async ({ cookingTimeMinutes: newTime, servings: newServings }) => {
      console.log("Agent updating cooking details:", { newTime, newServings });
      if (newTime !== undefined) setCookingTimeMinutes(newTime);
      if (newServings !== undefined) setServings(newServings);
    }
  });

  return (
    <div className="recipe-container">
      <header className="recipe-header">
        <h1>?? AI Recipe Assistant</h1>
        <p className="recipe-subtitle">
          Instant state updates powered by AG-UI Shared State
        </p>
      </header>

      <div className="recipe-content">
        {/* Dietary Preferences Section */}
        <section className="recipe-section">
          <h2>?? Dietary Preferences</h2>
          <div className="preferences-grid">
            {dietaryPreferences.map((pref, index) => (
              <span key={index} className="preference-badge">
                {pref}
              </span>
            ))}
            {dietaryPreferences.length === 0 && (
              <p className="empty-state">No dietary preferences set</p>
            )}
          </div>
        </section>

        {/* Recipe Details Section */}
        <section className="recipe-section">
          <h2>?? Recipe Details</h2>
          <div className="details-grid">
            <div className="detail-item">
              <span className="detail-label">Cooking Time:</span>
              <span className="detail-value">{cookingTimeMinutes} minutes</span>
            </div>
            <div className="detail-item">
              <span className="detail-label">Servings:</span>
              <span className="detail-value">{servings} people</span>
            </div>
          </div>
        </section>

        {/* Ingredients Section */}
        <section className="recipe-section">
          <h2>?? Ingredients ({ingredients.length})</h2>
          <div className="ingredients-list">
            {ingredients.map((ing, index) => (
              <div key={index} className="ingredient-item">
                <span className="ingredient-emoji">{ing.emoji}</span>
                <div className="ingredient-details">
                  <span className="ingredient-name">{ing.name}</span>
                  <span className="ingredient-quantity">{ing.quantity}</span>
                </div>
                <span className="ingredient-category">{ing.category}</span>
              </div>
            ))}
            {ingredients.length === 0 && (
              <p className="empty-state">No ingredients yet</p>
            )}
          </div>
        </section>

        {/* Instructions Section */}
        <section className="recipe-section">
          <h2>?? Instructions ({instructions.length} steps)</h2>
          <div className="instructions-list">
            {instructions.map((inst) => (
              <div key={inst.step} className="instruction-item">
                <div className="instruction-step-number">{inst.step}</div>
                <div className="instruction-content">
                  <p className="instruction-description">{inst.description}</p>
                  {inst.timeMinutes > 0 && (
                    <span className="instruction-time">
                      ?? {inst.timeMinutes} min
                    </span>
                  )}
                </div>
              </div>
            ))}
            {instructions.length === 0 && (
              <p className="empty-state">No instructions yet</p>
            )}
          </div>
        </section>

        {/* Help Section */}
        <section className="recipe-section help-section">
          <h3>?? Try asking the agent:</h3>
          <ul className="help-list">
            <li>"What's in this recipe?"</li>
            <li>"Add 2 cloves of garlic to the ingredients"</li>
            <li>"Make this recipe vegan"</li>
            <li>"Change step 1 to preheat to 375°F"</li>
            <li>"Double the recipe"</li>
            <li>"Add a step to chop the vegetables"</li>
          </ul>
          <p className="help-note">
            <strong>Watch the UI update instantly</strong> as the agent modifies
            the shared state! No network latency - pure magic ?
          </p>
        </section>
      </div>
    </div>
  );
}
