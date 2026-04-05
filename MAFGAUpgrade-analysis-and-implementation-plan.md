# MAF GA Upgrade – Analysis and Implementation Plan

> **Date**: April 5, 2026  
> **Scope**: Full MAFPlayground solution upgrade from MAF Preview to MAF 1.0.0 GA  
> **Author**: Automated analysis based on diff between current codebase and MAF GA reference

---

## Upgrade Tracking

| Phase | Task | Description | %Done | Reviewed | Notes |
|-------|------|-------------|-------|----------|-------|
| **1** | 1.1 | MAFPlayground.csproj package updates | 100% | | Added Workflows.Generators; updated all versions |
| **1** | 1.2 | AgentOpenTelemetry.csproj package updates | 100% | | All versions aligned to GA ref |
| **1** | 1.3 | AGUI.Server.csproj package updates | 100% | | AGUI Hosting still preview |
| **1** | 1.4 | AGUI.Client.csproj package updates | 100% | | AGUI still preview |
| **1** | 1.R | Phase 1 build + review | 100% | ✅ | 10 errors (expected API), 197 warnings (obsolete executors) |
| **2** | 2.1 | Rename `CreateAIAgent` → `AsAIAgent` | 100% | | 15 files updated |
| **2** | 2.2 | Rename `AgentRunResponse` → `AgentResponse` | 100% | | 8 files updated |
| **2** | 2.3 | Rename `StreamAsync` → `RunStreamingAsync` | 100% | | 26 files updated |
| **2** | 2.4 | Rename `McpClientFactory` → `McpClient` | 100% | | 3 files updated |
| **2** | 2.5 | Rename `.AsAgent(` → `.AsAIAgent(` on workflows | 100% | | 4 files updated |
| **2** | 2.6 | Rename `ConfigureSubWorkflow` → `BindAsExecutor` | 100% | | 2 files, 7 occurrences |
| **2** | 2.R | Phase 2 build + review | 100% | ✅ | 7 unique errors (AgentThread/GetNewThread – Phase 3), warnings all IMessageHandler/ReflectingExecutor obsolete – Phase 6 |
| **3** | 3.1 | Migrate `AgentThread`→`AgentSession` + `GetNewThread`→`CreateSessionAsync` | 100% | | 9 files type rename, 9 files method rename, Demo11 lazy init |
| **3** | 3.R | Phase 3 build + review | 100% | ✅ | 69 unique errors remain — all from Phase 4+ categories |
| **4** | 4.1 | Migrate `ChatClientAgentOptions` positional ctors | 100% | | Sample04 ×2, Sample06 ×2, Sample08, Sample11, Sample22 ×5 |
| **4** | 4.2 | Migrate `ChatClientAgentOptions { Instructions = }` → `ChatOptions` | 100% | | Demo02,03,04,08,09,10,11,12 + Sample19 (19 instances) |
| **4** | 4.3 | Add `using OpenAI.Chat;` for ChatClient extensions | 100% | | 18 files + ChatMessage alias in Sample02,03 |
| **4** | 4.4 | Rename `AgentRunUpdateEvent` → `AgentResponseUpdateEvent` | 100% | | 11 files |
| **4** | 4.5 | Rename `FunctionApproval*` → `ToolApproval*` + ToolCall API | 100% | | Sample03 |
| **4** | 4.6 | Rename `AddFanInEdge` → `AddFanInBarrierEdge` | 100% | | 16 files |
| **4** | 4.7 | Fix `AddEdge<T>` ambiguity | 100% | | Sample21 ×2 — explicit condition/idempotent |
| **4** | 4.R | Phase 4 build + review | 100% | ✅ | 0 errors, 220 warnings (all CS0618 obsolete executors) |
| **5** | 5.1 | Migrate `Deserialize<T>` → `RunAsync<T>` + `.Result` | 100% | | Demo11 ×2, Demo12 ×5 |
| **5** | 5.2 | Migrate streaming `Deserialize` → `JsonSerializer.Deserialize` | 100% | | Sample04, Sample19 |
| **5** | 5.R | Phase 5 build + review | 100% | ✅ | Included in Phase 4.R above |
| **6** | 6.1 | ReflectingExecutor → source-gen `[MessageHandler]` + `partial` + `Executor` base | 100% | | 20 files, 104 HandleAsync methods across ~96 executor classes |
| **6** | 6.2 | Remove `IMessageHandler<>` interfaces + `using Reflection;` | 100% | | All 20 files; 3 files required manual fix for IMessageHandler with inline comments |
| **6** | 6.3 | Add `partial` to outer static classes (nested executors) | 100% | | 12 files with nested private executors |
| **6** | 6.4 | `AddFanInBarrierEdge` parameter order swap (sources, aggregator) | 100% | | 14 files — old `(agg, sources:)` → new `(sources, agg)` |
| **6** | 6.R | Phase 6 build + review | 100% | ✅ | 0 errors, 9 warnings (all pre-existing: CS0162 unreachable, CS8604/CS8603 nullable) |
| **7** | 7.1 | Sample20 DevUI & Hosting verification | 100% | ✅ | All DevUI/Hosting APIs verified working (MapDevUI, AddAIAgent, AddWorkflow, etc.) |
| **7** | 7.2 | AgentOpenTelemetry project verification | 100% | ✅ | Already migrated via Phases 1-3 |
| **7** | 7.3 | AGUI.Server project verification | 100% | ✅ | Already migrated via Phases 1-3 |
| **7** | 7.4 | AGUI.Client project verification | 100% | ✅ | Already migrated via Phases 1-3 |
| **7** | 7.R | Phase 7 build + review | 100% | ✅ | All sub-projects build clean |
| **8** | 8.1 | Commented samples evaluation | 100% | ✅ | Sample12C, Sample12, Sample07 sections — left commented per analysis recommendations |
| **FIN** | FIN | Final full solution build + verification | 100% | ✅ | **0 errors, 9 warnings** (all pre-existing non-GA-related) |

---

## Table of Contents

1. [Executive Summary](#1-executive-summary)
2. [Current State Assessment](#2-current-state-assessment)
3. [Breaking Changes: Preview → GA](#3-breaking-changes-preview--ga)
4. [Package Version Mapping](#4-package-version-mapping)
5. [API Migration Reference](#5-api-migration-reference)
6. [Implementation Phases](#6-implementation-phases)
   - [Phase 0: Shared Foundations](#phase-0-shared-foundations)
   - [Phase 1: MAFPlayground Core](#phase-1-mafplayground-core)
   - [Phase 2: MAFPlayground Demos](#phase-2-mafplayground-demos)
   - [Phase 3: MAFPlayground Samples (Basic)](#phase-3-mafplayground-samples-basic)
   - [Phase 4: MAFPlayground Samples (Workflows)](#phase-4-mafplayground-samples-workflows)
   - [Phase 5: MAFPlayground Samples (Advanced)](#phase-5-mafplayground-samples-advanced)
   - [Phase 5B: Sample20 DevUI & Hosting](#phase-5b-sample20-devui--hosting)
   - [Phase 6: MAFPlayground Tests](#phase-6-mafplayground-tests)
   - [Phase 7: AgentOpenTelemetry Project](#phase-7-agentopentelemetry-project)
   - [Phase 8: AGUI Server Project](#phase-8-agui-server-project)
   - [Phase 9: AGUI Client Project](#phase-9-agui-client-project)
   - [Phase 10: Commented/Unused Samples](#phase-10-commentedunused-samples)
7. [Risk Assessment](#7-risk-assessment)
8. [Verification Strategy](#8-verification-strategy)

---

## 1. Executive Summary

The MAFPlayground solution uses **MAF Preview** packages (versions `1.0.0-preview.251110.x` / `1.0.0-preview.251125.x` / `1.0.0-alpha.251110.x`). MAF has now reached **GA at version 1.0.0**. This document catalogs every breaking change, renamed API, removed type, and behavioral difference, then provides a phased upgrade plan.

**Key Impact Summary:**

| Area | Impact | Effort |
|------|--------|--------|
| Agent creation (`CreateAIAgent` → `AsAIAgent`) | **HIGH** – affects 13+ call sites | Low (rename) |
| Session management (`AgentThread`/`GetNewThread` → `AgentSession`/`CreateSessionAsync`) | **HIGH** – affects 10+ files | Medium (async change) |
| Response types (`AgentRunResponse` → `AgentResponse`) | **HIGH** – affects 7+ files | Low (rename) |
| Executor pattern (`ReflectingExecutor<T>` + `IMessageHandler<>` → `[MessageHandler]` + source gen) | **HIGH** – ~55 declarations across 13 files | High (refactor) |
| Workflow execution (`InProcessExecution.StreamAsync` → `RunStreamingAsync`) | **MEDIUM** – affects 10+ call sites | Low (rename) |
| MCP client (`McpClientFactory.CreateAsync` → `McpClient.CreateAsync`) | **MEDIUM** – affects 3 files (4 occurrences) | Low (rename) |
| Package references update | **MEDIUM** – 5 .csproj files | Low (version bump) |
| `ChatClientAgentOptions` constructor changes | **MEDIUM** – affects ~15 call sites | Medium (refactor) |
| DevUI/Hosting API changes | **LOW** – 1 file (Sample20) | Medium |
| Third-party package updates (MEAI, OpenAI SDK, ModelContextProtocol) | **LOW** – version bumps | Low |

**Estimated Total Files to Modify:** ~55 `.cs` files + 4 `.csproj` files

---

## 2. Current State Assessment

### 2.1 Projects in Solution

| Project | Target Framework | MAF Preview Version | Status |
|---------|-----------------|---------------------|--------|
| **MAFPlayground** | net9.0 | 1.0.0-preview.251110.2 | Active – 12 demos, 22 samples, 6 tests |
| **AGUI.Server** | net9.0 | 1.0.0-preview.251125.1 | Active – 4 agent types |
| **AGUI.Client** | net9.0 | 1.0.0-preview.251125.1 | Active – Console client |
| **AgentOpenTelemetry** | net9.0 | 1.0.0-preview.251110.1 | Active – OTel demo |
| **Shared** | net10.0 | N/A (Azure.Core only) | Library – no MAF deps |

### 2.2 Preview APIs in Active Use

| Preview API | Occurrences | Files |
|-------------|-------------|-------|
| `CreateAIAgent(...)` | 14 | Demo01, Sample01–04, Sample22 (5 agents), AGUI.Server (4 agents), AGUI.Client |
| `AgentThread` / `GetNewThread()` | 10 | Demo03, Demo08–10, Demo11, Sample03, Sample10, OTel, AGUI.Client |
| `AgentRunResponse` / `AgentRunResponse<T>` | 7 | Sample03, Sample04, Sample10, Sample12A/B, Sample19 |
| `AgentRunResponseUpdate` | 5 | Sample10, AGUI.Client, AGUI.Server/InspectableAgent |
| `ReflectingExecutor<T>` + `IMessageHandler<>` | ~55 | Demo05–07, Demo11–12, Sample06, Sample08, Test01–06 (~26 executor classes in Tests alone) |
| `InProcessExecution.StreamAsync(...)` | 10+ | Demo04, Sample06, Sample08, Sample11, Test01–06 |
| `McpClientFactory.CreateAsync(...)` | 4 | Demo08, Demo09, Demo10 (×2) |
| `.AsAgent(...)` (workflow→agent) | 3 | Sample10, Sample11, Sample12C |
| `ChatClientAgentOptions(name:, instructions:)` (positional ctor) | 4 | Sample04 (×2), Sample06 (×2) |
| `MapOpenAIResponses()` / `MapOpenAIConversations()` | 1 | Sample20 |
| `AddAIAgent(...)` / `AddWorkflow(...)` (Hosting) | 1 | Sample20 |
| `.ToAgentRunResponseAsync()` | 2 | Sample04, Sample19 |
| `Hosting.OpenAI` package | 1 | .csproj |

---

## 3. Breaking Changes: Preview → GA

### 3.1 Agent Creation

| Preview | GA | Notes |
|---------|-----|-------|
| `chatClient.CreateAIAgent(instructions:, name:, ...)` | `chatClient.AsAIAgent(instructions:, name:, ...)` | Extension method was renamed. `CreateAIAgent` no longer exists on `ChatClient` from OpenAI SDK. The GA `AsAIAgent` is on `IChatClient` and `ChatClient` (OpenAI). |
| `chatClient.CreateAIAgent(new ChatClientAgentOptions(name:, instructions:))` | `chatClient.AsAIAgent(new ChatClientAgentOptions { Name =, ChatOptions = new() { Instructions = } })` | The positional constructor `ChatClientAgentOptions(name:, instructions:)` was removed. GA uses object initializer with `Name`, `ChatOptions.Instructions`. |
| `new ChatClientAgent(chatClient, new ChatClientAgentOptions { Instructions = "..." })` | `new ChatClientAgent(chatClient, new ChatClientAgentOptions { ChatOptions = new() { Instructions = "..." } })` | `Instructions` moved from `ChatClientAgentOptions` to `ChatOptions.Instructions`. |
| `new ChatClientAgent(chatClient, instructions, name)` | `new ChatClientAgent(chatClient, instructions:, name:)` | Still supported via named parameters. GA also accepts inline `string? instructions` param. |

### 3.2 Session Management

| Preview | GA | Notes |
|---------|-----|-------|
| `AgentThread thread = agent.GetNewThread()` | `AgentSession session = await agent.CreateSessionAsync()` | **Complete type replacement.** `AgentThread` is gone. `AgentSession` is the new abstraction. `CreateSessionAsync` is async. |
| `agent.RunAsync(message, thread)` | `agent.RunAsync(message, session)` | Parameter type changed from `AgentThread` to `AgentSession?`. |
| `agent.RunStreamingAsync(message, thread)` | `agent.RunStreamingAsync(message, session)` | Same as above. |

### 3.3 Response Types

| Preview | GA | Notes |
|---------|-----|-------|
| `AgentRunResponse` | `AgentResponse` | Class renamed. |
| `AgentRunResponse<T>` | `AgentResponse<T>` | Generic variant renamed. |
| `AgentRunResponseUpdate` | `AgentResponseUpdate` | Streaming delta type renamed. |
| `response.Deserialize<T>(options)` | `response.Result` (on `AgentResponse<T>`) | Structured output access changed. |
| `updates.ToAgentRunResponseAsync()` | Collect updates manually or use `RunAsync<T>()` | The extension method appears to be removed/renamed. |

### 3.4 Executor Patterns (Workflows)

| Preview | GA | Notes |
|---------|-----|-------|
| `ReflectingExecutor<T>` base class | `Executor` base class (direct) + `partial` class + `[MessageHandler]` attribute | **Major refactor.** `ReflectingExecutor<T>` is **obsolete** (marked with `[Obsolete]`). Migration to source-generated approach required. |
| `IMessageHandler<TIn>` interface | `[MessageHandler]` attribute on method | Interface obsoleted. Methods annotated with `[MessageHandler]` are discovered by source generator. |
| `IMessageHandler<TIn, TOut>` interface | `[MessageHandler]` + method return type | Return type inferred from method signature. |
| Manual `ConfigureRoutes` override | Auto-generated by source generator | Class must be `partial`. Generator produces `ConfigureRoutes`, `ConfigureSentTypes`, `ConfigureYieldTypes`. |
| `[SendsMessage(typeof(...))]` | `[SendsMessage(typeof(...))]` | **Unchanged** – class-level attribute, still used. |

**Example migration:**

```csharp
// PREVIEW:
internal sealed class MyExecutor : ReflectingExecutor<MyExecutor>, IMessageHandler<string, string>
{
    public async ValueTask<string> HandleAsync(string message, IWorkflowContext context, CancellationToken ct)
    {
        return message.ToUpper();
    }
}

// GA:
[SendsMessage(typeof(string))]
internal sealed partial class MyExecutor : Executor("MyExecutor")
{
    [MessageHandler]
    public async ValueTask<string> HandleAsync(string message, IWorkflowContext context, CancellationToken ct)
    {
        return message.ToUpper();
    }
}
// Source generator auto-generates ConfigureRoutes, ConfigureSentTypes, ConfigureYieldTypes
```

### 3.5 Workflow Execution

| Preview | GA | Notes |
|---------|-----|-------|
| `InProcessExecution.StreamAsync(workflow, input)` | `InProcessExecution.RunStreamingAsync(workflow, input)` | Method renamed. |
| `InProcessExecution.RunAsync(workflow, input)` | `InProcessExecution.RunAsync(workflow, input)` | **Unchanged.** |
| `run.NewEvents` (IEnumerable) | `run.NewEvents` (IAsyncEnumerable) | Potentially async now. |
| `run.WatchStreamAsync()` | `run.WatchStreamAsync()` | Likely **unchanged** or same API. |

### 3.6 Workflow-as-Agent

| Preview | GA | Notes |
|---------|-----|-------|
| `workflow.AsAgent(name:, description:)` | `workflow.AsAIAgent(name:, description:)` | Method renamed from `AsAgent` to `AsAIAgent`. Additional parameters available: `executionEnvironment`, `includeExceptionDetails`, `includeWorkflowOutputsInResponse`. |
| `workflow.ConfigureSubWorkflow(id, options)` | `workflow.BindAsExecutor(id, options)` | Renamed. `ConfigureSubWorkflow` is `[Obsolete]`. |

### 3.7 MCP Integration

| Preview | GA | Notes |
|---------|-----|-------|
| `McpClientFactory.CreateAsync(transport)` | `McpClient.CreateAsync(transport)` | Factory class removed; static method on `McpClient` now. |
| `ModelContextProtocol` v0.4.0-preview.3 | `ModelContextProtocol` v1.1.0 | Major version bump – API surface may have further changes. |
| `new StdioClientTransport(new() { ... })` | `new StdioClientTransport(new() { ... })` | Likely **unchanged** or minor property changes. |
| `new HttpClientTransport(new() { ... })` | `new HttpClientTransport(new() { ... })` | Same. |

### 3.8 DevUI & Hosting

| Preview | GA | Notes |
|---------|-----|-------|
| `Microsoft.Agents.AI.Hosting` (preview) | Package may still be preview in GA | Check NuGet for latest. |
| `Microsoft.Agents.AI.Hosting.OpenAI` (alpha) | Likely renamed or folded into core Hosting | The alpha package may not exist in GA. |
| `builder.AddAIAgent(name, instructions)` | Verify in GA hosting APIs | May have changed signature. |
| `builder.AddWorkflow(name, factory).AddAsAIAgent()` | Verify in GA hosting APIs | API may have changed. |
| `app.MapOpenAIResponses()` / `MapOpenAIConversations()` | Verify – may now be `MapDevUI()` only | Consolidation possible. |
| `builder.Services.AddOpenAIResponses()` | Verify in GA | Consolidation possible. |
| `app.MapDevUI()` | `app.MapDevUI()` | Likely **unchanged**. |

### 3.9 OpenTelemetry Integration

| Preview | GA | Notes |
|---------|-----|-------|
| `.AsBuilder().UseOpenTelemetry(sourceName:)` | `.AsBuilder().UseOpenTelemetry(sourceName:)` | Likely **unchanged** – same `AIAgentBuilder` API. |
| `agent.GetNewThread()` | `await agent.CreateSessionAsync()` | Session management change applies here too. |

### 3.10 Package Dependency Updates

| Package | Preview Version | GA Version | Notes |
|---------|----------------|------------|-------|
| `Azure.AI.OpenAI` | 2.5.0-beta.1 | **2.9.0-beta.1** | Significant SDK version bump |
| `Microsoft.Extensions.AI.OpenAI` | 10.0.0-preview.1.25559.3 / 10.0.1-preview.1.25571.5 | **10.4.0** | Major update |
| `Microsoft.Extensions.AI` | (transitive) | **10.4.0** | Through MEAI ecosystem |
| `OpenAI` | 2.6.0 | **2.9.1** | SDK major update |
| `ModelContextProtocol` | 0.4.0-preview.3 | **1.1.0** | GA release itself |
| `Azure.Identity` | 1.17.0 | **1.20.0** | Updated |

---

## 4. Package Version Mapping

### 4.1 MAFPlayground.csproj

| Package | Current | Target | Action |
|---------|---------|--------|--------|
| `Azure.AI.OpenAI` | 2.5.0-beta.1 | 2.9.0-beta.1 | Update |
| `Azure.Identity` | 1.17.0 | 1.20.0 | Update |
| `Microsoft.Agents.AI.DevUI` | 1.0.0-preview.251110.2 | 1.0.0 (or latest preview) | Update |
| `Microsoft.Agents.AI.Hosting` | 1.0.0-preview.251110.2 | 1.0.0 (or latest preview) | Update |
| `Microsoft.Agents.AI.Hosting.OpenAI` | 1.0.0-alpha.251110.2 | **Remove or replace** | Package likely discontinued |
| `Microsoft.Agents.AI.OpenAI` | 1.0.0-preview.251110.2 | **1.0.0** | Update to GA |
| `Microsoft.Agents.AI.Workflows` | 1.0.0-preview.251110.2 | **1.0.0** | Update to GA |
| `Microsoft.Extensions.AI.OpenAI` | 10.0.0-preview.1.25559.3 | 10.4.0 | Update |
| `ModelContextProtocol` | 0.4.0-preview.3 | 1.1.0 | Update |
| **Add**: `Microsoft.Agents.AI` | — | **1.0.0** | Core package (may now be separate) |
| **Add**: `Microsoft.Agents.AI.Workflows.Generators` | — | **1.0.0** | Source gen for executors |

### 4.2 AgentOpenTelemetry.csproj

| Package | Current | Target | Action |
|---------|---------|--------|--------|
| `Microsoft.Agents.AI` | 1.0.0-preview.251110.1 | **1.0.0** | Update |
| `Microsoft.Agents.AI.OpenAI` | 1.0.0-preview.251110.1 | **1.0.0** | Update |
| `Azure.AI.OpenAI` | 2.5.0-beta.1 | 2.9.0-beta.1 | Update |
| `Azure.Identity` | 1.17.0 | 1.20.0 | Update |
| `Microsoft.Extensions.AI.OpenAI` | 9.10.2-preview.1.25552.1 | 10.4.0 | Update |
| `OpenAI` | 2.6.0 | 2.9.1 | Update |
| `Microsoft.Extensions.Logging` | 10.0.0-rc.2.25502.107 | 10.0.0 (stable) | Update |
| `Microsoft.Extensions.Logging.Console` | 10.0.0-rc.2.25502.107 | 10.0.0 (stable) | Update |
| `System.Diagnostics.DiagnosticSource` | 10.0.0-rc.2.25502.107 | 10.0.0 (stable) | Update |
| `OpenTelemetry.*` | 1.14.0-rc.1 | Latest stable | Update |

### 4.3 AGUI.Server.csproj

| Package | Current | Target | Action |
|---------|---------|--------|--------|
| `Microsoft.Agents.AI.Hosting.AGUI.AspNetCore` | 1.0.0-preview.251125.1 | Latest available | Update |
| `Microsoft.Agents.AI` | 1.0.0-preview.251125.1 | **1.0.0** | Update |
| `Azure.AI.OpenAI` | 2.5.0-beta.1 | 2.9.0-beta.1 | Update |
| `Microsoft.Extensions.AI.OpenAI` | 10.0.1-preview.1.25571.5 | 10.4.0 | Update |

### 4.4 AGUI.Client.csproj

| Package | Current | Target | Action |
|---------|---------|--------|--------|
| `Microsoft.Agents.AI.AGUI` | 1.0.0-preview.251125.1 | Latest available | Update |
| `Microsoft.Agents.AI` | 1.0.0-preview.251125.1 | **1.0.0** | Update |

---

## 5. API Migration Reference

### Quick Reference: Search-and-Replace Patterns

These are mechanical changes that can be applied broadly:

| Find | Replace | Scope |
|------|---------|-------|
| `.CreateAIAgent(` | `.AsAIAgent(` | All `.cs` files |
| `AgentThread` | `AgentSession` | All `.cs` files |
| `GetNewThread()` | `await agent.CreateSessionAsync()` | All `.cs` files (requires async context) |
| `AgentRunResponse<` | `AgentResponse<` | All `.cs` files |
| `AgentRunResponse ` | `AgentResponse ` | All `.cs` files |
| `AgentRunResponseUpdate` | `AgentResponseUpdate` | All `.cs` files |
| `InProcessExecution.StreamAsync(` | `InProcessExecution.RunStreamingAsync(` | All `.cs` files |
| `.AsAgent(` (on Workflow) | `.AsAIAgent(` | Workflow files |
| `McpClientFactory.CreateAsync(` | `McpClient.CreateAsync(` | MCP files |
| `.ToAgentRunResponseAsync()` | See structured output migration | Individual review |
| `ConfigureSubWorkflow(` | `BindAsExecutor(` | Workflow files |

### Complex Migrations (Require Manual Review)

1. **`AgentThread` → `AgentSession`**: `GetNewThread()` (sync) → `CreateSessionAsync()` (async). Methods containing this need to be async or handle the task.

2. **`ChatClientAgentOptions` constructor**: Positional params `(name:, instructions:)` → object initializer `{ Name = ..., ChatOptions = new() { Instructions = ... } }`.

3. **`ReflectingExecutor<T>` → Source-generated executor**: Requires:
   - Change base class from `ReflectingExecutor<MyType>` to `Executor("MyId")`
   - Add `partial` keyword to class
   - Remove `IMessageHandler<TIn>` / `IMessageHandler<TIn, TOut>` interfaces
   - Add `[MessageHandler]` attribute to handler methods
   - Add `[SendsMessage(typeof(...))]` / `[YieldsMessage(typeof(...))]` on class if needed
   - Ensure `Microsoft.Agents.AI.Workflows.Generators` is referenced

4. **Structured output**: `AgentRunResponse<T>.Deserialize<T>()` → `AgentResponse<T>.Result` property.

5. **Hosting APIs**: `AddAIAgent`, `AddWorkflow`, `MapOpenAIResponses`, `MapOpenAIConversations` – need verification against GA hosting package.

---

## 6. Implementation Phases

### Recommended Execution Strategy

> **IMPORTANT**: The phases below are grouped by project/area for reference, but should be **executed in migration-type order** for optimal success. The recommended sequence:
>
> 1. **All `.csproj` package updates first** (Phase 0 + tasks 1.1, 7.1, 8.1, 9.1) — unblocks everything
> 2. **Global mechanical renames** — `CreateAIAgent`→`AsAIAgent`, `AgentRunResponse`→`AgentResponse`, `StreamAsync`→`RunStreamingAsync`, `McpClientFactory`→`McpClient`, `.AsAgent(`→`.AsAIAgent(`, `ConfigureSubWorkflow`→`BindAsExecutor` — fixes ~70% of compilation errors with low risk
> 3. **Session management migration** — `AgentThread`→`AgentSession`, `GetNewThread()`→`await CreateSessionAsync()` — requires async context changes
> 4. **`ChatClientAgentOptions` constructor migration** — positional params → object initializer
> 5. **Structured output migration** — `ToAgentRunResponseAsync`/`Deserialize` patterns
> 6. **Executor migration** (`ReflectingExecutor<T>` → source-generated `[MessageHandler]`) — highest risk, do last. Start with simplest executors (Test01, Sample06), escalate to complex (Demo12 with 11 executors)
> 7. **DevUI & Hosting** (Sample20) — uncertain GA API surface, may need NuGet investigation
> 8. **Commented samples evaluation** — post-upgrade decision
>
> **Rationale**: Migration-type ordering lets you `dotnet build` after each step to verify progress. Mechanical renames yield quick wins and unblock the harder work. The riskiest changes (executor source-gen migration) are isolated at the end where you have maximum context.
>
> Run `dotnet build MAFPlayground.slnx` after each step above. Expect some errors until step 6 completes.

---

### Phase 0: Shared Foundations
**Scope**: Solution-level and Shared project  
**Effort**: Small  
**Dependencies**: None

| Task | File(s) | Change |
|------|---------|--------|
| 0.1 | `Shared/Shared.csproj` | No MAF changes needed. Optionally update `Azure.Core` to 1.50.0+ if desired. Note: already targets `net10.0` – verify compatibility. |
| 0.2 | `MAFPlayground.slnx` | No changes needed (project references are stable). |
| 0.3 | All `.csproj` files | Consider adding `Directory.Packages.props` for centralized version management (recommended but optional). |

---

### Phase 1: MAFPlayground Core
**Scope**: MAFPlayground.csproj, AIConfig.cs, Program.cs  
**Effort**: Medium  
**Dependencies**: Phase 0

| Task | File(s) | Change |
|------|---------|--------|
| 1.1 | `MAFPlayground/MAFPlayground.csproj` | Update all MAF package references to GA (see §4.1). Remove `Microsoft.Agents.AI.Hosting.OpenAI` (alpha). Add `Microsoft.Agents.AI.Workflows.Generators` as analyzer reference. |
| 1.2 | `MAFPlayground/AIConfig.cs` | No MAF API changes needed – pure config. |
| 1.3 | `MAFPlayground/Program.cs` | No direct MAF API usage – just calls demo/sample methods. No changes. |

**Verification**: Build succeeds after package update (may have errors in dependent files – expected).

---

### Phase 2: MAFPlayground Demos
**Scope**: All files under `MAFPlayground/Demos/`  
**Effort**: High  
**Dependencies**: Phase 1

#### 2.1 Demo01_BasicAgent.cs
| Change | Detail |
|--------|--------|
| `.CreateAIAgent(` → `.AsAIAgent(` | 2 occurrences |

#### 2.2 Demo02_SuperPoweredAssistant.cs
| Change | Detail |
|--------|--------|
| `new ChatClientAgent(chatClient, new ChatClientAgentOptions { Instructions = ... })` | Migrate `Instructions` to `ChatOptions.Instructions` |

#### 2.3 Demo03_ChatWithSuperPoweredAssistant.cs
| Change | Detail |
|--------|--------|
| `new ChatClientAgent(chatClient, new ChatClientAgentOptions { ... })` | Migrate `Instructions` to `ChatOptions.Instructions` |
| `AgentThread thread = assistant.GetNewThread()` | → `AgentSession session = await assistant.CreateSessionAsync()` |
| `assistant.RunAsync(userInput, thread)` | → `assistant.RunAsync(userInput, session)` |

#### 2.4 Demo04_WorkflowsBasicSequentialContentProduction.cs
| Change | Detail |
|--------|--------|
| `new ChatClientAgent(chatClient, new ChatClientAgentOptions { ... })` × 3 | Migrate `Instructions` to `ChatOptions.Instructions` |
| `InProcessExecution.StreamAsync(` | → `InProcessExecution.RunStreamingAsync(` |

#### 2.5 Demo05_SubWorkflows.cs
| Change | Detail |
|--------|--------|
| `ReflectingExecutor<T>` + `IMessageHandler<>` × 4 | `UppercaseExecutor`, `AppendSuffixExecutor`, `PrefixExecutor`, `PostProcessExecutor` — migrate to `Executor` + `[MessageHandler]` + `partial class`. 1 additional executor is commented out (`ReverseExecutor`). |
| `InProcessExecution.StreamAsync(` | → `InProcessExecution.RunStreamingAsync(` |
| `.BindAsExecutor( ` | Already correct GA naming – verify still works |

#### 2.6 Demo06_ConcurrentWorkflowMixed.cs
| Change | Detail |
|--------|--------|
| Agent creation patterns | Migrate `ChatClientAgent` constructors |
| `ReflectingExecutor<T>` / `IMessageHandler<>` | Migrate to `[MessageHandler]` + `partial` + source gen |
| `InProcessExecution.StreamAsync(` | → `InProcessExecution.RunStreamingAsync(` |

#### 2.7 Demo07_MixedAgentsAndExecutors.cs
| Change | Detail |
|--------|--------|
| Same executor migration as Demo06 | `ReflectingExecutor<T>` → `Executor` + `[MessageHandler]` |
| Workflow execution changes | `StreamAsync` → `RunStreamingAsync` |

#### 2.8 Demo08_GitHubMasterMCPAgent.cs
| Change | Detail |
|--------|--------|
| `McpClientFactory.CreateAsync(` → `McpClient.CreateAsync(` | 1 occurrence |
| `new ChatClientAgent(chatClient, new ChatClientAgentOptions { ... })` | Migrate options |
| `AgentThread thread = gitHubMaster.GetNewThread()` | → `AgentSession session = await gitHubMaster.CreateSessionAsync()` |
| `gitHubMaster.RunAsync(userInput, thread)` | → `gitHubMaster.RunAsync(userInput, session)` |

#### 2.9 Demo09_GraphDatabaseCrimeAgent.cs
| Change | Detail |
|--------|--------|
| `McpClientFactory.CreateAsync(` → `McpClient.CreateAsync(` | 1 occurrence |
| Session management | `GetNewThread()` → `CreateSessionAsync()` |
| Agent run calls | Thread → session parameter |

#### 2.10 Demo10_DevMasterMultiMCP.cs
| Change | Detail |
|--------|--------|
| `McpClientFactory.CreateAsync(` × 2 | → `McpClient.CreateAsync(` |
| `new ChatClientAgent(chatClient, new ChatClientAgentOptions { ... })` | Options migration |
| `AgentThread thread = devMaster.GetNewThread()` | → `CreateSessionAsync()` |
| `devMaster.RunAsync(userInput, thread)` | → Session parameter |

#### 2.11 ClaimsDemo/Demo11_ClaimsWorkflow.cs
| Change | Detail |
|--------|--------|
| `using Microsoft.Agents.AI.Hosting;` | Verify this import is still needed/correct |
| `new ChatClientAgent(chat, new ChatClientAgentOptions { ... })` × 3 | Options migration |
| `AgentThread _thread` field + `_agent.GetNewThread()` × 2 | → `AgentSession` field + `await CreateSessionAsync()` |
| `_agent.RunAsync(prompt, _thread, ...)` | → Session parameter |
| `ReflectingExecutor<T>` + `IMessageHandler<>` patterns (if present) | Migrate to source gen |

#### 2.12 ClaimsDemo/Demo12_ClaimsFraudDetection.cs
| Change | Detail |
|--------|--------|
| `new ChatClientAgent(chat, new ChatClientAgentOptions { ... })` × 5+ | Options migration |
| `ReflectingExecutor<T>` + `IMessageHandler<>` patterns | Migrate to source gen |
| `InProcessExecution.StreamAsync(` | → `RunStreamingAsync(` |

#### 2.13 ClaimsDemo/ClaimsMockTools.cs & SharedClaimsData.cs
| Change | Detail |
|--------|--------|
| No direct MAF API changes expected | Pure data classes and tool functions |

---

### Phase 3: MAFPlayground Samples (Basic)
**Scope**: Simpler samples with minimal workflow usage  
**Effort**: Medium  
**Dependencies**: Phase 1

#### 3.1 Sample01_BasicAgent.cs
| Change | Detail |
|--------|--------|
| `.CreateAIAgent(` × 2 | → `.AsAIAgent(` |

#### 3.2 Sample02_ImageAgent.cs
| Change | Detail |
|--------|--------|
| `.CreateAIAgent(` | → `.AsAIAgent(` |

#### 3.3 Sample03_FunctionsApprovals.cs
| Change | Detail |
|--------|--------|
| `.CreateAIAgent(` | → `.AsAIAgent(` |
| `AgentThread thread = agent.GetNewThread()` | → `AgentSession session = await agent.CreateSessionAsync()` |
| `AgentRunResponse response = await agent.RunAsync(prompt, thread)` | → `AgentResponse response = await agent.RunAsync(prompt, session)` |
| `agent.RunAsync(approvalMessage, thread)` | → session parameter |

#### 3.4 Sample04_StructuredOutput.cs
| Change | Detail |
|--------|--------|
| `chatClient.CreateAIAgent(new ChatClientAgentOptions(name:, instructions:))` | → `chatClient.AsAIAgent(new ChatClientAgentOptions { Name = ..., ChatOptions = new() { Instructions = ... } })` |
| `AgentRunResponse<PersonInfo>` | → `AgentResponse<PersonInfo>` |
| `.ToAgentRunResponseAsync()` | Migrate to `RunAsync<T>()` or manual collection |
| `.Deserialize<PersonInfo>(JsonSerializerOptions.Web)` | Use `.Result` on `AgentResponse<T>` |

#### 3.5 Sample12A_WriterChatAgent.cs
| Change | Detail |
|--------|--------|
| `AgentRunResponse response = await writer.RunAsync(...)` | → `AgentResponse` |

#### 3.6 Sample12B_InteractiveWriterChat.cs
| Change | Detail |
|--------|--------|
| `AgentRunResponse response = await writer.RunAsync(conversationHistory)` × 2 | → `AgentResponse` |

---

### Phase 4: MAFPlayground Samples (Workflows)
**Scope**: Samples using workflow orchestration  
**Effort**: High  
**Dependencies**: Phase 1

#### 4.1 Sample06_ConditionalEdges.cs
| Change | Detail |
|--------|--------|
| `new ChatClientAgent(chatClient, new ChatClientAgentOptions(instructions:) { ... })` | Options migration |
| `ReflectingExecutor<SpamDetectionExecutor>`, `IMessageHandler<ChatMessage, DetectionResult>` | → `partial class SpamDetectionExecutor : Executor("SpamDetectionExecutor")` + `[MessageHandler]` |
| `ReflectingExecutor<EmailAssistantExecutor>`, `IMessageHandler<DetectionResult, EmailResponse>` | Same pattern |
| `ReflectingExecutor<SendEmailExecutor>`, `IMessageHandler<EmailResponse>` | Same pattern |
| `InProcessExecution.StreamAsync(` | → `RunStreamingAsync(` |

#### 4.2 Sample07_AgentWorkflowPatterns.cs
| Change | Detail |
|--------|--------|
| Agent creation patterns | Migrate constructors |
| Commented-out workflow patterns | See Phase 10 |
| `InProcessExecution.StreamAsync(` (commented) | Update if uncommenting |

#### 4.3 Sample08_ConcurrentWithConditional.cs
| Change | Detail |
|--------|--------|
| `new ChatClientAgent(chatClient, ...)` × 3 | Strings-based constructor – likely OK but verify |
| `ReflectingExecutor<T>` patterns (if present) | Migrate |
| `InProcessExecution.StreamAsync(` | → `RunStreamingAsync(` |

#### 4.4 Sample10_WorkflowAsAgent.cs
| Change | Detail |
|--------|--------|
| `workflow.AsAgent(name:, description:)` | → `workflow.AsAIAgent(name:, description:)` |
| `AgentThread thread = workflowAgent.GetNewThread()` | → `await workflowAgent.CreateSessionAsync()` |
| `AgentRunResponseUpdate` × 3 | → `AgentResponseUpdate` |
| `agent.RunStreamingAsync(input, thread)` | → session parameter |

#### 4.5 Sample11_WorkflowAsAgentNested.cs
| Change | Detail |
|--------|--------|
| `wrappedWorkflow.AsAgent(` | → `.AsAIAgent(` |
| `new ChatClientAgentOptions(...)` | Options migration |
| `InProcessExecution.StreamAsync(` | → `RunStreamingAsync(` |

#### 4.6 Sample12D_CustomReviewWorkflow.cs
| Change | Detail |
|--------|--------|
| `new ChatClientAgent(...)` × 5 | Verify constructor patterns |
| `InProcessExecution.RunAsync(` | Already correct name |

#### 4.7 Sample14_SoftwareDevelopmentPipeline.cs
| Change | Detail |
|--------|--------|
| Agent creation + workflow execution patterns | Depends on specific APIs used |
| `ReflectingExecutor` patterns | Migrate if present |

#### 4.8 Sample15_SoftwareDevelopmentPipelineWithSubWorkflows.cs
| Change | Detail |
|--------|--------|
| Sub-workflow patterns | `ConfigureSubWorkflow` → `BindAsExecutor` |
| All other standard migrations | Apply |

#### 4.9 Sample16_ChatWithWorkflow.cs
| Change | Detail |
|--------|--------|
| `ReflectingExecutor<T>` patterns | Migrate executors |
| Agent streaming calls | Standard session/response renames |

#### 4.10 Sample17_WriterCriticIterationWorkflow.cs
| Change | Detail |
|--------|--------|
| `ReflectingExecutor<T>` patterns | Migrate executors |
| Agent streaming calls | Standard renames |

#### 4.11 Sample18_WriterCriticAgentsOnly.cs
| Change | Detail |
|--------|--------|
| Agent-only pattern (no executors expected) | Standard agent migration |

#### 4.12 Sample19_WriterCriticStructuredOutput.cs
| Change | Detail |
|--------|--------|
| `.ToAgentRunResponseAsync()` | Migrate structured output |
| Executor patterns | Migrate if present |

#### 4.13 Sample21_FeatureComplianceReview.cs
| Change | Detail |
|--------|--------|
| Complex workflow with parallel reviewers | Full executor + workflow migration |

---

### Phase 5: MAFPlayground Samples (Advanced)
**Scope**: Sample22_WorkshopPlanner  
**Effort**: Medium  
**Dependencies**: Phase 1

| Task | File(s) | Change |
|------|---------|--------|
| 5.1 | `Sample22_WorkshopPlanner.cs` | `BindAsExecutor` calls verify, `InProcessExecution.StreamAsync(` → `RunStreamingAsync(` |
| 5.2 | `Agents/*.cs` (5 files) | `.CreateAIAgent(new ChatClientAgentOptions(...))` → `.AsAIAgent(...)` with new options pattern |
| 5.3 | `Executors/*.cs` (9 files) | Verify executor patterns (may use function-based executors – confirm if `ReflectingExecutor` is used) |

---

### Phase 5B: Sample20 DevUI & Hosting
**Scope**: `MAFPlayground/Samples/Sample20_DevUIBasicUsage.cs`  
**Effort**: Medium-High (APIs most likely to have changed between preview and GA)  
**Dependencies**: Phase 1

> **⚠️ This sample was omitted from the original phases. Its hosting APIs (`AddAIAgent`, `AddWorkflow`, `MapOpenAIResponses`, `MapOpenAIConversations`, `AddAsAIAgent`, `MapDevUI`) are the MOST uncertain for GA compatibility. Treat this as a research-and-adapt task.**

| Task | File(s) | Change |
|------|---------|--------|
| 5B.1 | `Sample20_DevUIBasicUsage.cs` | Verify `Microsoft.Agents.AI.Hosting` and `Microsoft.Agents.AI.Hosting.OpenAI` packages exist in GA. If `Hosting.OpenAI` (alpha) is discontinued, this entire sample may need a rewrite. |
| 5B.2 | `Sample20_DevUIBasicUsage.cs` | `builder.AddAIAgent(name, instructions)` × 5 — verify method signature unchanged in GA Hosting |
| 5B.3 | `Sample20_DevUIBasicUsage.cs` | `builder.AddWorkflow(name, factory).AddAsAIAgent()` — verify fluent API chain |
| 5B.4 | `Sample20_DevUIBasicUsage.cs` | `app.MapOpenAIResponses()` + `app.MapOpenAIConversations()` + `app.MapDevUI()` — verify all three endpoints exist |
| 5B.5 | `Sample20_DevUIBasicUsage.cs` | `builder.Services.AddOpenAIResponses()` — verify service registration |
| 5B.6 | `Sample20_DevUIBasicUsage.cs` | Any `ReflectingExecutor<T>` patterns inside executor classes — migrate to source gen if present |

---

### Phase 6: MAFPlayground Tests
**Scope**: `MAFPlayground/Tests/`  
**Effort**: **High** (all 6 tests use `ReflectingExecutor<T>` — ~26 executor classes total)  
**Dependencies**: Phase 1

| Task | File(s) | Change |
|------|---------|--------|
| 6.1 | `Test01_FanOutFanInBasic.cs` | 1 `ReflectingExecutor` (`AggregatorExecutor_FunctionBased`) → source gen. Uses function-based executors too (no migration needed for those). `InProcessExecution.StreamAsync(` → `RunStreamingAsync(` |
| 6.2 | `Test02_FanOutFanInClassBased.cs` | 5 `ReflectingExecutor` classes + `IMessageHandler` → source gen. `InProcessExecution.StreamAsync(` → `RunStreamingAsync(` |
| 6.3 | `Test03_FanOutFanInWithAsyncBlocking.cs` | 5+ `ReflectingExecutor` classes → source gen. `InProcessExecution.StreamAsync(` → `RunStreamingAsync(` |
| 6.4 | `Test04_FanOutFanInWithRealStateOperations.cs` | 5 `ReflectingExecutor` classes → source gen. `InProcessExecution.StreamAsync(` → `RunStreamingAsync(` (**known: FAILS – keep as-is to test GA behavior**) |
| 6.5 | `Test05_FanOutFanInWithProperAsync.cs` | 5 `ReflectingExecutor` classes → source gen. `InProcessExecution.StreamAsync(` → `RunStreamingAsync(` (**known: FAILS – keep as-is**) |
| 6.6 | `Test06_FanOutFanInStateInAggregator.cs` | 5+ `ReflectingExecutor` classes → source gen. `InProcessExecution.StreamAsync(` → `RunStreamingAsync(` |

> **Note**: Tests are a good starting point for executor migration practice. Test01 (1 executor) and Test02 (5 executors with simple patterns) are the easiest to validate against since they have deterministic behavior.

---

### Phase 7: AgentOpenTelemetry Project
**Scope**: `AgentOpenTelemetry/` project  
**Effort**: Medium  
**Dependencies**: Phase 0

| Task | File(s) | Change |
|------|---------|--------|
| 7.1 | `AgentOpenTelemetry.csproj` | Update all package versions (see §4.2) |
| 7.2 | `Program.cs` | `agent.GetNewThread()` → `await agent.CreateSessionAsync()` |
| 7.3 | `Program.cs` | `AgentThread thread` type → `AgentSession session` |
| 7.4 | `Program.cs` | `agent.RunStreamingAsync(userInput, thread)` → session parameter |
| 7.5 | `Program.cs` | `new ChatClientAgent(instrumentedChatClient, name:, instructions:, tools:)` – likely still works (this constructor exists in GA) |
| 7.6 | `Program.cs` | `.AsBuilder().UseOpenTelemetry(...)` – verify API unchanged |

---

### Phase 8: AGUI Server Project
**Scope**: `AGUI.Server/` project  
**Effort**: Medium  
**Dependencies**: Phase 0

| Task | File(s) | Change |
|------|---------|--------|
| 8.1 | `AGUI.Server.csproj` | Update package versions (see §4.3) |
| 8.2 | `Agents/BasicAgent.cs` | `chatClient.CreateAIAgent(name:, instructions:)` → `chatClient.AsAIAgent(name:, instructions:)` |
| 8.3 | `Agents/AgentWithTools.cs` | Verify agent creation pattern – likely uses `CreateAIAgent` |
| 8.4 | `Agents/InspectableAgent.cs` | `chatClient.CreateAIAgent(...)` → `.AsAIAgent(...)`. `AgentRunResponseUpdate` → `AgentResponseUpdate` in return type. `AgentThread` → `AgentSession` in method parameter. Verify `AsBuilder().Use(runFunc: null, runStreamingFunc: ...)` middleware pattern still works in GA. |
| 8.5 | `Agents/SharedStateCookingSimple/RecipeAgent.cs` | Agent creation migration |
| 8.6 | `Program.cs` | No direct MAF API changes (uses `IChatClient`-based agent creation). Verify `AddAGUI()` and `MapAGUI()` still work. |

---

### Phase 9: AGUI Client Project  
**Scope**: `AGUI.Client/` project  
**Effort**: Medium (not "Low" — uses 4 preview APIs directly)  
**Dependencies**: Phase 0

| Task | File(s) | Change |
|------|---------|--------|
| 9.1 | `AGUI.Client.csproj` | Update package versions (see §4.4) |
| 9.2 | `Program.cs` | `chatClient.CreateAIAgent(` → `chatClient.AsAIAgent(` (line 53) |
| 9.3 | `Program.cs` | `AgentThread thread = agent.GetNewThread()` → `AgentSession session = await agent.CreateSessionAsync()` (line 58) |
| 9.4 | `Program.cs` | `AgentRunResponseUpdate` → `AgentResponseUpdate` (line 123) |
| 9.5 | `Program.cs` | `agent.RunStreamingAsync(messages, thread)` → session parameter |

---

### Phase 10: Commented/Unused Samples
**Scope**: Decisions on commented-out code  
**Effort**: Low  

| File | Status | Recommendation |
|------|--------|---------------|
| **Sample12C_WorkflowAsAgentReview.cs** | Fully commented out | **Leave commented.** The file documents known issues with workflow-as-agent composition + streaming fragmentation. Update comments to reference GA behaviors but don't uncomment unless the underlying issues were fixed in GA. Consider testing in GA and uncomment if fixed. |
| **Sample12_GroupChatRoundRobin.cs** | Fully commented out | **Leave commented.** The GA has a `GroupChatWorkflowBuilder` API that may have fixed the issues. Consider rewriting with GA APIs as a separate follow-up task. |
| **Sample07 commented sections** | Partially commented (handoff/group chat) | **Leave commented.** Same reasoning – test GA group chat APIs separately. |
| **Sample06 commented `RunAsync` line** | Alternative execution method | **Leave as comment** – useful for reference. |

**Rationale**: Commented samples represent known preview-era limitations. Rather than blindly fixing them, they should be evaluated after the core upgrade is complete to see if GA resolved the underlying issues.

---

## 7. Risk Assessment

### High Risk

| Risk | Mitigation |
|------|------------|
| `ReflectingExecutor<T>` → source gen migration may break complex executor chains | **~55 executor classes across 13 files** (not ~8 as initially estimated). Start with Test01 (1 executor), then Test02 (5 simple executors), then Sample06 (4 executors with typed I/O). Only then tackle Demo12 (11 executors, most complex). |
| `ChatClientAgentOptions` constructor signature differences may have additional changes not captured | Cross-reference with GA source in MAF folder for each usage. |
| `Hosting.OpenAI` alpha package may not have GA equivalent | Check NuGet. If discontinued, Sample20 DevUI demo may need a rewrite or be temporarily disabled. |

### Medium Risk

| Risk | Mitigation |
|------|------------|
| `ModelContextProtocol` 0.4 → 1.1 may have additional breaking changes beyond factory rename | Read MCP 1.1 changelog. Test Demo08, Demo09, Demo10 individually after migration. |
| Tests Test04 and Test05 already fail – may fail differently in GA | Document current behavior before upgrading. Compare after. |
| Third-party package version alignment | Use versions from `MAF/dotnet/Directory.Packages.props` as authoritative source. |

### Low Risk

| Risk | Mitigation |
|------|------------|
| `AgentSession` async initialization adds complexity | Straightforward async/await pattern change. |
| `AgentResponse` rename is mechanical | Find-and-replace with compilation validation. |

---

## 8. Verification Strategy

### Per-Phase Verification

1. **After each phase**: Run `dotnet build` on the solution to catch compilation errors.
2. **After Phase 1**: Verify MAFPlayground project compiles even with some errors in demo/sample files.
3. **After Phases 2-5**: Run each demo/sample individually to verify runtime behavior:
   - Demo01 (basic agent) – smoke test
   - Demo04 (workflow) – workflow execution
   - Demo05 (sub-workflows) – executor migration + sub-workflow binding
   - Demo08 (MCP) – external integration
   - Sample06 (conditional edges) – executor migration
   - Test01 (fan-out/fan-in) – simplest executor test
   - Test02 (class-based) – standard executor pattern
4. **After Phase 5B**: Run Sample20 DevUI with browser to verify hosting endpoints work.
5. **After Phase 7**: Run AgentOpenTelemetry with Aspire dashboard to verify telemetry still works.
6. **After Phases 8-9**: Run AGUI server + client together to verify AG-UI protocol.

### Regression Checks

- **Agent basic execution**: Demo01, Sample01 – agents respond to prompts
- **Tool calling**: Demo02, Sample03 – tools are invoked correctly
- **Session/multi-turn**: Demo03, Demo08 – conversation context is preserved
- **Workflow execution**: Demo04, Sample06 – workflows complete with expected events
- **Concurrent workflows**: Demo06, Sample08 – fan-out/fan-in works
- **Structured output**: Sample04, Sample19 – typed responses deserialize correctly
- **MCP integration**: Demo08, Demo10 – MCP tools are discovered and used
- **OpenTelemetry**: AgentOpenTelemetry – traces/metrics/logs appear in dashboard
- **AGUI**: Server + Client – streaming agent responses work end-to-end

---

## Appendix A: Files by Change Category

### A.1 Files needing `CreateAIAgent` → `AsAIAgent`
- `Demos/Demo01_BasicAgent.cs`
- `Samples/Sample01_BasicAgent.cs`
- `Samples/Sample02_ImageAgent.cs`
- `Samples/Sample03_FunctionsApprovals.cs`
- `Samples/Sample04_StructuredOutput.cs`
- `Samples/Sample22_WorkshopPlanner/Agents/DiscoveryAgent.cs`
- `Samples/Sample22_WorkshopPlanner/Agents/EnricherAgent.cs`
- `Samples/Sample22_WorkshopPlanner/Agents/EvaluatorAgent.cs`
- `Samples/Sample22_WorkshopPlanner/Agents/PlannerAgent.cs`
- `Samples/Sample22_WorkshopPlanner/Agents/RequirementsAgent.cs`
- `AGUI.Server/Agents/BasicAgent.cs`
- `AGUI.Server/Agents/AgentWithTools.cs`
- `AGUI.Server/Agents/InspectableAgent.cs`
- `AGUI.Server/Agents/SharedStateCookingSimple/RecipeAgent.cs`
- `AGUI.Client/Program.cs`

### A.2 Files needing `AgentThread` → `AgentSession`
- `Demos/Demo03_ChatWithSuperPoweredAssistant.cs`
- `Demos/Demo08_GitHubMasterMCPAgent.cs`
- `Demos/Demo09_GraphDatabaseCrimeAgent.cs`
- `Demos/Demo10_DevMasterMultiMCP.cs`
- `Demos/ClaimsDemo/Demo11_ClaimsWorkflow.cs` (2 field declarations)
- `Samples/Sample03_FunctionsApprovals.cs`
- `Samples/Sample10_WorkflowAsAgent.cs` (type + method parameter)
- `AgentOpenTelemetry/Program.cs`
- `AGUI.Client/Program.cs`
- `AGUI.Server/Agents/InspectableAgent.cs` (method parameter)

### A.3 Files needing `AgentRunResponse` → `AgentResponse`
- `Samples/Sample03_FunctionsApprovals.cs`
- `Samples/Sample04_StructuredOutput.cs` (`AgentRunResponse<T>` + `ToAgentRunResponseAsync`)
- `Samples/Sample10_WorkflowAsAgent.cs` (`AgentRunResponseUpdate` × 3)
- `Samples/Sample12A_WriterChatAgent.cs` (×2)
- `Samples/Sample12B_InteractiveWriterChat.cs` (×2)
- `Samples/Sample19_WriterCriticStructuredOutput.cs` (`AgentRunResponse<T>` + `ToAgentRunResponseAsync`)
- `AGUI.Client/Program.cs` (`AgentRunResponseUpdate`)
- `AGUI.Server/Agents/InspectableAgent.cs` (`IAsyncEnumerable<AgentRunResponseUpdate>` return type)

### A.4 Files needing `ReflectingExecutor<T>` migration
**Tests** (~26 executor classes):
- `Tests/Test01_FanOutFanInBasic.cs` (1 ReflectingExecutor: `AggregatorExecutor_FunctionBased`)
- `Tests/Test02_FanOutFanInClassBased.cs` (5 executor classes)
- `Tests/Test03_FanOutFanInWithAsyncBlocking.cs` (5+ executor classes)
- `Tests/Test04_FanOutFanInWithRealStateOperations.cs` (5 executor classes)
- `Tests/Test05_FanOutFanInWithProperAsync.cs` (5 executor classes)
- `Tests/Test06_FanOutFanInStateInAggregator.cs` (5+ executor classes)

**Demos** (~23 executor classes):
- `Demos/Demo05_SubWorkflows.cs` (4 active + 1 commented: `UppercaseExecutor`, `AppendSuffixExecutor`, `PrefixExecutor`, `PostProcessExecutor`)
- `Demos/Demo06_ConcurrentWorkflowMixed.cs` (3 executor classes)
- `Demos/Demo07_MixedAgentsAndExecutors.cs` (verify count)
- `Demos/ClaimsDemo/Demo11_ClaimsWorkflow.cs` (4 executor classes)
- `Demos/ClaimsDemo/Demo12_ClaimsFraudDetection.cs` (11 executor classes — largest single file)

**Samples** (~7 executor classes):
- `Samples/Sample06_ConditionalEdges.cs` (4 executor classes)
- `Samples/Sample08_ConcurrentWithConditional.cs` (3 executor classes)

> **Total: ~55 `ReflectingExecutor` declarations across 13 files.** This is the highest-effort migration task.

### A.5 Files needing `StreamAsync` → `RunStreamingAsync`
**Demos** (7 files):
- `Demos/Demo04_WorkflowsBasicSequentialContentProduction.cs`
- `Demos/Demo05_SubWorkflows.cs`
- `Demos/Demo06_ConcurrentWorkflowMixed.cs`
- `Demos/Demo07_MixedAgentsAndExecutors.cs`
- `Demos/ClaimsDemo/Demo11_ClaimsWorkflow.cs`
- `Demos/ClaimsDemo/Demo12_ClaimsFraudDetection.cs`

**Samples** (12 files, 2 commented):
- `Samples/Sample06_ConditionalEdges.cs`
- `Samples/Sample07_AgentWorkflowPatterns.cs` (commented)
- `Samples/Sample08_ConcurrentWithConditional.cs`
- `Samples/Sample11_WorkflowAsAgentNested.cs`
- `Samples/Sample12C_WorkflowAsAgentReview.cs` (commented)
- `Samples/Sample12_GroupChatRoundRobin.cs` (commented)
- `Samples/Sample14_SoftwareDevelopmentPipeline.cs`
- `Samples/Sample15_SoftwareDevelopmentPipelineWithSubWorkflows.cs`
- `Samples/Sample16_ChatWithWorkflow.cs`
- `Samples/Sample17_WriterCriticIterationWorkflow.cs`
- `Samples/Sample18_WriterCriticAgentsOnly.cs`
- `Samples/Sample19_WriterCriticStructuredOutput.cs`
- `Samples/Sample21_FeatureComplianceReview.cs`
- `Samples/Sample22_WorkshopPlanner/Sample22_WorkshopPlanner.cs`

**Tests** (all 6):
- `Tests/Test01–Test06` (all 6 test files)

> **Total: ~25 files, ~27 occurrences** (excluding commented lines)

### A.6 Files needing MCP migration
- `Demos/Demo08_GitHubMasterMCPAgent.cs` (1 occurrence)
- `Demos/Demo09_GraphDatabaseCrimeAgent.cs` (1 occurrence)
- `Demos/Demo10_DevMasterMultiMCP.cs` (2 occurrences)

### A.7 Files needing `ConfigureSubWorkflow` → `BindAsExecutor`
- `Demos/Demo05_SubWorkflows.cs` (1 occurrence)
- `Samples/Sample15_SoftwareDevelopmentPipelineWithSubWorkflows.cs` (7 occurrences)

### A.8 `.csproj` files to modify
- `MAFPlayground/MAFPlayground.csproj`
- `AgentOpenTelemetry/AgentOpenTelemetry.csproj`
- `AGUI.Server/AGUI.Server.csproj`
- `AGUI.Client/AGUI.Client.csproj`

---

## Appendix B: GA Reference Code Locations

The MAF GA source code is available locally for reference:

| Component | Path |
|-----------|------|
| `ChatClientAgent` | `MAF/dotnet/src/Microsoft.Agents.AI/ChatClient/ChatClientAgent.cs` |
| `ChatClientAgentOptions` | `MAF/dotnet/src/Microsoft.Agents.AI/ChatClient/ChatClientAgentOptions.cs` |
| `AsAIAgent` extension (IChatClient) | `MAF/dotnet/src/Microsoft.Agents.AI/ChatClient/ChatClientExtensions.cs` |
| `AsAIAgent` extension (ChatClient/OpenAI) | `MAF/dotnet/src/Microsoft.Agents.AI.OpenAI/Extensions/OpenAIChatClientExtensions.cs` |
| `AIAgent` base class | `MAF/dotnet/src/Microsoft.Agents.AI.Abstractions/AIAgent.cs` |
| `AgentSession` | `MAF/dotnet/src/Microsoft.Agents.AI.Abstractions/AgentSession.cs` |
| `AgentResponse` / `AgentResponse<T>` | `MAF/dotnet/src/Microsoft.Agents.AI.Abstractions/AgentResponse.cs` |
| `Executor` base class | `MAF/dotnet/src/Microsoft.Agents.AI.Workflows/Executor.cs` |
| `WorkflowBuilder` | `MAF/dotnet/src/Microsoft.Agents.AI.Workflows/WorkflowBuilder.cs` |
| `InProcessExecution` | `MAF/dotnet/src/Microsoft.Agents.AI.Workflows/InProcessExecution.cs` |
| `ExecutorBindingExtensions` | `MAF/dotnet/src/Microsoft.Agents.AI.Workflows/ExecutorBindingExtensions.cs` |
| `WorkflowHostingExtensions` (AsAIAgent) | `MAF/dotnet/src/Microsoft.Agents.AI.Workflows/WorkflowHostingExtensions.cs` |
| GA Samples (getting started) | `MAF/dotnet/samples/01-get-started/` |
| GA Samples (agents) | `MAF/dotnet/samples/02-agents/Agents/` |
| GA Workflow Samples | `MAF/dotnet/samples/03-workflows/` |
| `Directory.Packages.props` (versions) | `MAF/dotnet/Directory.Packages.props` |

---

*End of analysis. Proceed with Phase 0 → Phase 1 → Phase 2 in order, verifying compilation after each phase.*
