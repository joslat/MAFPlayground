# SharedStateCookingSimple - Recipe Assistant with Instant State Updates

## Overview

This sample demonstrates **AG-UI's Shared State feature** - the ability for an AI agent to read and write application state with **instant UI updates** (no network latency).

The Recipe Assistant showcases how the agent can modify ingredients, instructions, and dietary preferences, and these changes appear immediately in the React UI.

## Key Features

? **Shared State Reading** - Agent can inspect current recipe state via `GetRecipeState` tool  
? **Shared State Writing** - Agent can modify ingredients, instructions, preferences via Update tools  
? **Instant Rendering** - UI updates immediately when agent modifies state (no network roundtrip)  
? **CopilotKit Integration** - Uses `useCopilotReadable` and `useCopilotAction` React hooks  
? **AG-UI Protocol** - Full compliance with AG-UI specification  

## Architecture

```
????????????????????????????
?  React Frontend          ?  (AGUI.Client.React)
?  CopilotKit Components   ?  Port: 5173
????????????????????????????
             ? AG-UI Protocol (SSE + HTTP)
             ?
????????????????????????????
?  ASP.NET Core Backend    ?  (AGUI.Server)
?  RecipeAgent             ?  Port: 8888
????????????????????????????
             ?
             ?
????????????????????????????
?  Azure OpenAI            ?
????????????????????????????
```

## Components

### Backend (AGUI.Server)

**RecipeAgent.cs** - Agent with 5 tools:
- `GetRecipeState` - Read current recipe state
- `UpdateIngredients` - Modify ingredients (instant UI update)
- `UpdateInstructions` - Modify cooking steps (instant UI update)
- `UpdateDietaryPreferences` - Set dietary constraints (instant UI update)
- `UpdateCookingDetails` - Change time/servings (instant UI update)

**RecipeState.cs** - Data models:
- `RecipeState` - Complete recipe with ingredients, instructions, preferences
- `Ingredient` - Name, quantity, category, emoji
- `Instruction` - Step number, description, time
- Request/Response types for tool calls

### Frontend (AGUI.Client.React)

**RecipeDemo.tsx** - Main component with:
- `useCopilotReadable` - Makes recipe state available to agent
- `useCopilotAction` - Allows agent to modify state (instant UI update!)
- Recipe display (ingredients, instructions, preferences)
- CopilotKit chat sidebar

## Running the Sample

### Prerequisites

1. ? .NET 9 SDK
2. ? Node.js 18+ and npm
3. ? Azure OpenAI credentials

### Step 1: Set Environment Variables

```powershell
$env:AZURE_OPENAI_ENDPOINT="https://your-resource.openai.azure.com/"
$env:AZURE_OPENAI_API_KEY="your-api-key"
$env:AZURE_OPENAI_DEPLOYMENT_NAME="gpt-4o"  # or gpt-4o-mini
```

### Step 2: Start Backend Server

```powershell
cd AGUI.Server
$env:AGUI_AGENT_TYPE='sharedstate'
dotnet run
```

**Expected output:**
```
???????????????????????????????????????????????????????????
=== AG-UI Server ===
???????????????????????????????????????????????????????????
Endpoint: https://your-resource.openai.azure.com/
Deployment: gpt-4o

? Agent: RecipeAssistant (shared state)
  Description: Recipe assistant with shared state - instant UI updates
  Endpoint: /api/recipe
  Features: Instant UI state updates via AG-UI shared state
  Frontend: Use AGUI.Client.React project

??????????????????????????????????????????????????????????

?? AG-UI Server is starting...
   Listening on: http://localhost:8888
```

### Step 3: Start React Frontend (New Terminal)

```powershell
cd AGUI.Client.React
npm install  # First time only
npm run dev
```

**Expected output:**
```
  VITE v5.x.x  ready in xxx ms

  ?  Local:   http://localhost:5173/
  ?  Network: use --host to expose
```

### Step 4: Open Browser

Navigate to: **http://localhost:5173**

## Try These Commands

Once the app is running, try these commands in the chat:

### Basic Queries
- "What's in this recipe?"
- "Show me the cooking steps"
- "What are the current dietary preferences?"

### Modify Ingredients (Instant Update!)
- "Add 2 cloves of garlic to the ingredients"
- "Remove the wheat flour"
- "Replace carrots with 3 zucchinis"
- "Add some salt and pepper"

### Modify Instructions (Instant Update!)
- "Change step 1 to preheat to 375°F"
- "Add a step to chop the vegetables"
- "Remove the last step"

### Dietary Preferences (Instant Update!)
- "Make this recipe vegan"
- "I need a gluten-free version"
- "Add low-carb and high-protein preferences"

### Scaling
- "Double the recipe"
- "Change to 6 servings"
- "Reduce cooking time to 20 minutes"

**Watch the UI update instantly as the agent calls the update tools!** ??

## How Shared State Works

### 1. Agent Side (Backend)

The agent has tools that modify the state:

```csharp
[Description("Add, remove, or update ingredients in the recipe")]
private static StateUpdateResponse UpdateIngredients(
    UpdateIngredientsRequest request)
{
    var state = _stateStore.GetOrAdd(request.ThreadId, _ => new RecipeState());
    state.Ingredients = request.Ingredients;
    _stateStore[request.ThreadId] = state;
    
    return new StateUpdateResponse
    {
        Success = true,
        UpdatedState = state
    };
}
```

### 2. React Side (Frontend)

The UI declares actions the agent can perform:

```typescript
useCopilotAction({
  name: "updateIngredients",
  description: "Update the recipe ingredients",
  parameters: [
    { name: "ingredients", type: "object[]" }
  ],
  handler: async ({ ingredients }) => {
    setIngredients(ingredients); // Instant UI update!
  }
});
```

### 3. The Magic ?

When the agent calls `UpdateIngredients` tool:
1. Backend processes the request
2. AG-UI protocol sends state update via SSE
3. CopilotKit triggers the `useCopilotAction` handler
4. React state updates ? **UI re-renders immediately**
5. **No network latency!** The UI updates before the agent even responds.

## Comparison to Other Patterns

| Pattern | Update Latency | When to Use |
|---------|---------------|-------------|
| **Shared State** (this sample) | **Instant** | Real-time collaborative editing, live dashboards |
| Traditional API calls | Network roundtrip | Standard CRUD operations |
| Polling | Periodic delay | Background updates, non-critical data |
| WebSockets | Near real-time | Chat apps, notifications |

## Key Code Patterns

### Backend: Tool with State Update

```csharp
[Description("Update recipe ingredients")]
private static StateUpdateResponse UpdateIngredients(
    UpdateIngredientsRequest request)
{
    // 1. Get current state
    var state = _stateStore.GetOrAdd(request.ThreadId, _ => new RecipeState());
    
    // 2. Modify state
    state.Ingredients = request.Ingredients;
    
    // 3. Store and return
    _stateStore[request.ThreadId] = state;
    return new StateUpdateResponse { Success = true, UpdatedState = state };
}
```

### Frontend: Action Handler

```typescript
// Make state readable by agent
useCopilotReadable({
  description: "Current recipe state",
  value: { ingredients, instructions, dietaryPreferences }
});

// Make state writable by agent
useCopilotAction({
  name: "updateIngredients",
  handler: async ({ ingredients }) => {
    setIngredients(ingredients); // Instant update!
  }
});
```

## Project Structure

```
AGUI.Server/Agents/SharedStateCookingSimple/
??? RecipeAgent.cs           # Agent with shared state tools
??? RecipeState.cs           # Data models and serialization context
??? README.md                # This file

AGUI.Client.React/src/samples/SharedStateCookingSimple/
??? RecipeDemo.tsx           # Main component with CopilotKit
??? RecipeUI.tsx             # Recipe display components
??? styles.css               # Component styles
```

## Learning Resources

- [AG-UI Shared State Docs](https://docs.copilotkit.ai/shared-state)
- [AG-UI Dojo Demo](https://dojo.ag-ui.com/microsoft-agent-framework-dotnet/feature/shared_state)
- [CopilotKit Integration](https://docs.copilotkit.ai/microsoft-agent-framework)
- [AG-UI Protocol Spec](https://docs.ag-ui.com/introduction)

## Troubleshooting

### Backend not starting?
- Check Azure OpenAI credentials are set
- Ensure port 8888 is not in use
- Verify `$env:AGUI_AGENT_TYPE='sharedstate'` is set

### Frontend not connecting?
- Check backend is running on http://localhost:8888
- Verify CORS is enabled in backend
- Check browser console for errors

### State not updating?
- Ensure `useCopilotAction` handlers match tool names exactly
- Check browser console for CopilotKit errors
- Verify backend tools are being called (check backend console output)

## Next Steps

### Extend This Sample

1. **Add More Recipes** - Store multiple recipes per user
2. **Recipe Search** - Search public recipe database
3. **Nutritional Info** - Calculate calories, macros
4. **Shopping List** - Generate list from ingredients
5. **Recipe Photos** - Upload and display images

### Explore Other AG-UI Features

- **Sample24: Predictive State Updates** - Optimistic UI updates
- **Sample25: Generative UI** - Dynamic UI generation by agent
- **Sample26: HITL** - Human-in-the-loop approvals

## License

Copyright (c) Microsoft Corporation. All rights reserved.  
Licensed under the MIT License.  
Modified for MAFPlayground by Jose Luis Latorre
