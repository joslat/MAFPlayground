// SPDX-License-Identifier: LicenseRef-MAFPlayground-NPU-1.0-CH
// Copyright (c) 2025 Jose Luis Latorre

using MAFPlayground.Demos;
using MAFPlayground.Samples;
using MAFPlayground.Samples.Sample22_WorkshopPlanner;
using MAFPlayground.Tests;

Console.WriteLine("Hello, Microsoft Agent Framework!");
//await Demo05_SubWorkflows.Execute();

/// AGENT DEMOS AND SAMPLES
//await Demo01_BasicAgent.Execute();
//await Sample02_ImageAgent.Execute();
//await Sample03_FunctionsApprovals.Execute();
//await Sample04_StructuredOutput.Execute();
//await Demo02_SuperPoweredAssistant.Execute();
//await Demo03_ChatWithSuperPoweredAssistant.Execute();
//await Demo08_GitHubMasterMCPAgent.Execute();
//await Demo09_GraphDatabaseCrimeAgent.Execute();


//await Sample01_BasicAgent.Execute();
//await Sample02_ImageAgent.Execute();  
//await Sample03_FunctionsApprovals.Execute();
//await Sample04_StructuredOutput.Execute();
//await Sample05_ConcurrentWorkflow.Execute();
//await Sample06_ConditionalEdges.Execute();
//await Sample08_ConcurrentWithConditional.Execute();
//await Sample07_AgentWorkflowPatterns.Execute("sequential");
//await Sample07_AgentWorkflowPatterns.Execute("concurrent");
//await Sample07_AgentWorkflowPatterns.Execute("handoff");
//await Sample07_AgentWorkflowPatterns.Execute("groupchat");
//await Sample09_SubWorkflows.Execute();
//await Sample10_WorkflowAsAgent.Execute();
//await Sample11_WorkflowAsAgentNested.Execute();
//await Sample12_GroupChatRoundRobin.Execute();
//await Sample12A_WriterChatAgent.Execute();
//await Sample12B_InteractiveWriterChat.Execute();
//await Sample12C_WorkflowAsAgentReview.Execute();
//await Sample12D_CustomReviewWorkflow.Execute();
//await Sample13_MixedAgentsAndExecutors.Execute();

// DEMOS
//await Demo01_BasicAgent.Execute();
//await Demo02_SuperPoweredAssistant.Execute();
//await Demo03_ChatWithSuperPoweredAssistant.Execute();
//await Demo04_WorkflowsBasicSequentialContentProduction.Execute();
//await Sample14_SoftwareDevelopmentPipeline.Execute();
//await Sample15_SoftwareDevelopmentPipelineWithSubWorkflows.Execute();
//await Demo05_SubWorkflows.Execute();
//await Demo07_MixedAgentsAndExecutors.Execute();

//await Sample16_ChatWithWorkflow.Execute();
//await Sample17_WriterCriticIterationWorkflow.Execute();
//await Sample18_WriterCriticAgentsOnly.Execute();
//await Sample19_WriterCriticStructuredOutput.Execute();
//await Sample20_DevUIBasicUsage.Execute(); // Note: This is synchronous (starts web server)
//await Demo08_GitHubMasterMCPAgent.Execute();
//await Demo09_GraphDatabaseCrimeAgent.Execute();
//await Demo10_DevMasterMultiMCP.Execute();

// Demo 11: Claims Workflow - Choose your mode!
//await Demo11_ClaimsWorkflow.Execute();              // Console mode (interactive)
await Demo12_ClaimsFraudDetection.Execute();       // Fraud detection with fan-out/fan-in ✅ WORKS!

// ════════════════════════════════════════════════════════════════════════════════
// TESTS - Minimal test cases to understand workflow patterns
// ════════════════════════════════════════════════════════════════════════════════
//await Test01_FanOutFanInBasic.Execute();                    // Basic fan-out/fan-in with function-based executors
//await Test02_FanOutFanInClassBased.Execute();               // Basic fan-out/fan-in with class-based executors
//await Test03_FanOutFanInWithAsyncBlocking.Execute();        // Fan-out/fan-in with async blocking ✅ WORKS!
//await Test04_FanOutFanInWithRealStateOperations.Execute();  // Fan-out/fan-in with context state blocking ❌ FAILS!
//await Test05_FanOutFanInWithProperAsync.Execute();          // Fan-out/fan-in with proper async/await ❌ STILL FAILS!
//await Test06_FanOutFanInStateInAggregator.Execute();        // Fan-out/fan-in with state in aggregator ✅ WORKS!
//await Sample14_SoftwareDevelopmentPipeline.Execute();
// ════════════════════════════════════════════════════════════════════════════════

//await Sample21_FeatureComplianceReview.Execute();
//await Sample22_WorkshopPlanner.Execute();

//Sample22_DevUI_UserInput.ExecuteDevUI(); // Note: This is synchronous (starts web server)



// ════════════════════════════════════════════════════════════════════════════════
// AG-UI SAMPLES (Separate Projects - React Frontend Required)
// ════════════════════════════════════════════════════════════════════════════════
// These samples demonstrate AG-UI protocol features with CopilotKit React components.
// They run as separate server + client projects outside MAFPlayground.
//
// 📦 Projects:
//   • AGUI.Server - ASP.NET Core AG-UI backend (shared with console client)
//   • AGUI.Client.React - React + CopilotKit frontend (NEW)
//   • AGUI.Client - Console client (existing)
//
// 🚀 How to Run Shared State Recipe Sample:
//   1. Terminal 1 - Start Backend:
//      cd AGUI.Server
//      $env:AGUI_AGENT_TYPE='sharedstate'
//      dotnet run
//
//   2. Terminal 2 - Start React Frontend:
//      cd AGUI.Client.React
//      npm install  # First time only
//      npm run dev
//
//   3. Open browser: http://localhost:5173
//
// 💡 Sample: SharedStateCookingSimple
//   • Recipe Assistant with INSTANT state updates
//   • Agent reads/writes ingredients, instructions, dietary preferences
//   • No network latency - UI updates immediately when agent modifies state
//   • Uses CopilotKit useCopilotReadable/useCopilotAction hooks
//   • Full AG-UI protocol compliance with shared state feature
//
// 📚 Learn More:
//   • AGUI.Server/Agents/SharedStateCookingSimple/README.md
//   • https://docs.copilotkit.ai/microsoft-agent-framework/shared-state
// ════════════════════════════════════════════════════════════════════════════════