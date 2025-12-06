# AG-UI Shared State Recipe Demo

A complete demonstration of **AG-UI's Shared State feature** using Microsoft Agent Framework (.NET) and CopilotKit (React).

## ?? What This Demo Shows

This sample demonstrates the **killer feature** of AG-UI: **instant state updates**. When the AI agent modifies the recipe (ingredients, instructions, preferences), the React UI updates **immediately** without network latency.

### Key Features

- ? **Instant UI Updates** - No waiting for API responses
- ? **Shared State Protocol** - Bidirectional state sync between agent and UI
- ? **CopilotKit Integration** - `useCopilotReadable` and `useCopilotAction` hooks
- ? **Full Type Safety** - TypeScript frontend, C# backend
- ? **AG-UI Protocol Compliant** - Works with any AG-UI client

## ??? Architecture

```
?????????????????????????????
?  React Frontend           ?  Port: 5173
?  - CopilotKit Components  ?
?  - Shared State Hooks     ?
?????????????????????????????
             ? AG-UI Protocol (SSE + HTTP)
             ?
?????????????????????????????
?  ASP.NET Core Backend     ?  Port: 8888
?  - RecipeAgent            ?
?  - Shared State Tools     ?
?????????????????????????????
             ?
             ?
?????????????????????????????
?  Azure OpenAI             ?
?  - GPT-4o / GPT-4o-mini   ?
?????????????????????????????
```

## ?? Projects

### Backend: `AGUI.Server`
- **Location**: `AGUI.Server/Agents/SharedStateCookingSimple/`
- **Language**: C# (.NET 9)
- **Key Files**:
  - `RecipeAgent.cs` - Agent with 5 shared state tools
  - `RecipeState.cs` - Data models and JSON serialization

### Frontend: `AGUI.Client.React`
- **Location**: `AGUI.Client.React/src/samples/SharedStateCookingSimple/`
- **Framework**: React 18 + TypeScript + Vite
- **Key Files**:
  - `RecipeDemo.tsx` - Main component with CopilotKit
  - `styles.css` - Beautiful gradient UI

## ?? Quick Start

### Prerequisites

1. ? **.NET 9 SDK** - For backend
2. ? **Node.js 18+** - For frontend
3. ? **Azure OpenAI** - GPT-4o or GPT-4o-mini deployment

### Step 1: Set Environment Variables

```powershell
$env:AZURE_OPENAI_ENDPOINT="https://your-resource.openai.azure.com/"
$env:AZURE_OPENAI_API_KEY="your-api-key"
$env:AZURE_OPENAI_DEPLOYMENT_NAME="gpt-4o"  # or gpt-4o-mini
```

### Step 2: Run the Demo

**Option A: One Command (Recommended)**

```powershell
.\start-sharedstate-both.ps1
```

This launches both backend and frontend in separate windows.

**Option B: Manual Start**

Terminal 1 - Backend:
```powershell
.\start-sharedstate-server.ps1
```

Terminal 2 - Frontend:
```powershell
.\start-sharedstate-client.ps1
```

### Step 3: Open Browser

Navigate to: **http://localhost:5173**

## ?? Try These Commands

Once the app is running, chat with the Recipe Assistant:

### View Recipe
- "What's in this recipe?"
- "Show me the ingredients"
- "What are the cooking steps?"

### Modify Ingredients (Watch Instant Update! ?)
- "Add 2 cloves of garlic"
- "Remove the wheat flour"
- "Replace carrots with 3 zucchinis"
- "Add salt and pepper"

### Change Instructions (Watch Instant Update! ?)
- "Change step 1 to preheat to 375°F"
- "Add a step to chop the vegetables"
- "Make step 2 more detailed"

### Dietary Adjustments (Watch Instant Update! ?)
- "Make this recipe vegan"
- "I need a gluten-free version"
- "Add low-carb preference"

### Scale Recipe (Watch Instant Update! ?)
- "Double the recipe"
- "Change to 6 servings"
- "Reduce cooking time to 20 minutes"

## ?? How It Works

### 1. Backend Agent (C#)

The agent has tools that modify state:

```csharp
[Description("Update recipe ingredients")]
private static StateUpdateResponse UpdateIngredients(
    UpdateIngredientsRequest request)
{
    var state = _stateStore.GetOrAdd(request.ThreadId, _ => new RecipeState());
    state.Ingredients = request.Ingredients;
    return new StateUpdateResponse { Success = true, UpdatedState = state };
}
```

### 2. Frontend React (TypeScript)

The UI declares what the agent can modify:

```typescript
useCopilotAction({
  name: "updateIngredients",
  handler: async ({ ingredients }) => {
    setIngredients(ingredients); // ? Instant UI update!
  }
});
```

### 3. The Magic ?

When the agent calls a tool:
1. Backend processes the request
2. AG-UI protocol sends state update via SSE
3. CopilotKit triggers the action handler
4. React state updates ? **UI re-renders instantly**
5. **No waiting for network!**

## ?? Project Structure

```
MAFPlayground/
??? AGUI.Server/
?   ??? Agents/
?       ??? SharedStateCookingSimple/
?           ??? RecipeAgent.cs      # Agent with tools
?           ??? RecipeState.cs      # Data models
?           ??? README.md           # Detailed backend docs
?
??? AGUI.Client.React/
?   ??? src/
?   ?   ??? samples/
?   ?   ?   ??? SharedStateCookingSimple/
?   ?   ?       ??? RecipeDemo.tsx  # Main component
?   ?   ?       ??? styles.css      # UI styles
?   ?   ??? App.tsx                 # CopilotKit setup
?   ?   ??? main.tsx                # Entry point
?   ??? package.json
?
??? start-sharedstate-server.ps1   # Launch backend
??? start-sharedstate-client.ps1   # Launch frontend
??? start-sharedstate-both.ps1     # Launch both
??? README.md                       # This file
```

## ?? Troubleshooting

### Backend won't start?
- ? Check Azure OpenAI credentials are set
- ? Ensure port 8888 is not in use
- ? Verify `$env:AGUI_AGENT_TYPE='sharedstate'` is set

### Frontend won't connect?
- ? Check backend is running on http://localhost:8888
- ? Verify CORS is enabled in backend
- ? Check browser console for errors

### State not updating instantly?
- ? Ensure action names match between backend tools and frontend handlers
- ? Check browser console for CopilotKit errors
- ? Verify backend tools are being called (check backend console)

### npm install fails?
- ? Check Node.js version (18+ required)
- ? Try `npm cache clean --force`
- ? Delete `node_modules` and try again

## ?? Learn More

### Documentation
- [AG-UI Shared State Docs](https://docs.copilotkit.ai/shared-state)
- [CopilotKit Microsoft Agent Framework Integration](https://docs.copilotkit.ai/microsoft-agent-framework)
- [AG-UI Protocol Specification](https://docs.ag-ui.com/introduction)

### Live Demos
- [AG-UI Dojo - Shared State](https://dojo.ag-ui.com/microsoft-agent-framework-dotnet/feature/shared_state)

### Backend Deep Dive
- See `AGUI.Server/Agents/SharedStateCookingSimple/README.md` for detailed backend documentation

## ?? Next Steps

### Extend This Sample

1. **Multiple Recipes** - Store recipes per user/session
2. **Recipe Search** - Search public recipe APIs
3. **Nutritional Info** - Calculate calories and macros
4. **Shopping List** - Generate from ingredients
5. **Recipe Photos** - Upload and display images
6. **Recipe Sharing** - Share via URL

### Explore Other AG-UI Features

- **Predictive State Updates** - Optimistic UI updates
- **Generative UI** - Dynamic UI generation by agent
- **Human-in-the-Loop** - Approval workflows
- **Backend Tool Rendering** - Server-side tool execution

## ?? Contributing

Found a bug or have an improvement? Open an issue or PR!

## ?? License

Copyright (c) Microsoft Corporation. All rights reserved.  
Licensed under the MIT License.  
Modified for MAFPlayground by Jose Luis Latorre

---

**Built with ?? using Microsoft Agent Framework and CopilotKit**
