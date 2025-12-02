# Building and Integrating Microsoft Agents with the Microsoft Agents SDK

## Workshop Details

**Duration:** 4 hours

**Target Audience:** Intermediate developers with experience in C#, .NET, basic AI/LLM concepts, and asynchronous programming.

## Overview

This 4-hour, fast-paced workshop enables intermediate developers to gain hands-on experience in building and integrating Microsoft Agents using the Microsoft Agents SDK. Participants will explore the architecture of the Microsoft Agent framework, understand how the Model Context Protocol (MCP) facilitates tool connectivity, and design multi-step, agentic workflows that integrate structured outputs and tool calling. The workshop combines conceptual learning with practical labs so attendees leave with actionable skills to implement enterprise-grade agent solutions.

## Workshop Modules

### Module 1: Microsoft Agents Architecture and MCP Integration

**Duration:** 60 minutes

**Learning Objectives:**
- Understand the key components and architecture of Microsoft Agent 365.
- Explain how Model Context Protocol (MCP) enables agent integration and secure tool access.
- Set up the Microsoft Agent 365 environment and SDK.

**Content Sources:**
- [Microsoft Agent 365 SDK Overview (Microsoft Learn)](https://learn.microsoft.com/en-us/microsoft-agent-365/developer/)
  - *Introduction to core architecture and MCP concepts*

**Activities:**
- Guided walkthrough: Explore the Agent 365 Developer Portal and SDK components.
- Hands-on: Set up the Agent 365 CLI and connect to a sample MCP tool server.
- Mini challenge: Configure agent identity and connect to Microsoft Entra for authentication.

---

### Module 2: Tooling Servers and MCP on Windows

**Duration:** 55 minutes

**Learning Objectives:**
- Understand the structure and operation of Microsoft Agent 365 Tooling Servers.
- Develop a basic MCP tool server for controlled workflow execution.
- Secure and monitor agent connections using Microsoft Defender and Entra permissions.

**Content Sources:**
- [Agent 365 Tooling Servers Overview (Microsoft Learn)](https://learn.microsoft.com/en-us/microsoft-agent-365/tooling-servers-overview)
  - *Core reading and walkthrough for server configuration*
- [Model Context Protocol (MCP) on Windows (Microsoft Learn)](https://learn.microsoft.com/en-us/windows/ai/mcp/overview#mcp-agent-connectors-on-windows)
  - *Secondary reference for MCP configuration and connector development*

**Activities:**
- Live demo: Connect an MCP host app to a local agent registry using odr.exe.
- Hands-on lab: Build and test a simple MCP-compliant tool server with logging and auditing.
- Discussion: Applying compliance and DLP policies to agent workflows.

---

### Module 3: Designing Multi-Step Agent Workflows

**Duration:** 65 minutes

**Learning Objectives:**
- Describe the principles of agentic orchestration and workflow design.
- Implement sequential and concurrent multi-agent workflows in C# using the Agent Framework.
- Handle streaming updates, checkpointing, and structured output management.

**Content Sources:**
- [Microsoft Agent Framework Workflows Overview (Microsoft Learn)](https://learn.microsoft.com/en-us/agent-framework/user-guide/workflows/overview#overview)
  - *Conceptual deep dive into workflows*
- [Agents in Workflows (C# Tutorial) (Microsoft Learn)](https://learn.microsoft.com/en-us/agent-framework/tutorials/workflows/agents-in-workflows)
  - *Hands-on workflow integration tutorial*
- [Create a Simple Concurrent Workflow (C# Tutorial) (Microsoft Learn)](https://learn.microsoft.com/en-us/agent-framework/tutorials/workflows/simple-concurrent-workflow)
  - *Concurrent workflow development exercise*

**Activities:**
- Demo: Visualize and compare sequential vs concurrent workflows using WorkflowBuilder.
- Hands-on: Create a two-agent workflow for information retrieval and summarization using Azure OpenAI.
- Challenge exercise: Add concurrency patterns (fan-out/fan-in) to improve workflow performance.

---

### Module 4: Sequential Orchestration and Tool Calling

**Duration:** 60 minutes

**Learning Objectives:**
- Combine MCP-based tools with sequential agent orchestration for real-world use cases.
- Develop agent pipelines using ChatClientAgent and StreamingRun patterns.
- Implement structured output and error handling for predictable orchestration.

**Content Sources:**
- [Microsoft Agent Framework Workflows Orchestrations - Sequential (C#) (Microsoft Learn)](https://learn.microsoft.com/en-us/agent-framework/user-guide/workflows/orchestrations/sequential)
  - *Hands-on sequential orchestration guide*

**Activities:**
- Live coding: Build a sequential agent pipeline integrating MCP tool calls for external data processing.
- Hands-on lab: Extend the pipeline with event handling and tool result validation.
- Wrap-up discussion: Strategies for integrating Agent Framework workflows with enterprise systems.

---

### Module 5: Capstone: Building an End-to-End Multi-Agent Solution

**Duration:** 40 minutes

**Learning Objectives:**
- Integrate concepts from previous modules to build a full agent solution.
- Deploy and test a governed multi-step workflow connecting to an MCP tool server.
- Demonstrate observability, compliance, and telemetry using Purview and Defender.

**Content Sources:**
- [Selected review of previous modules’ Microsoft Learn components](https://learn.microsoft.com/)
  - *Reference materials for wrap-up and capstone*

**Activities:**
- Hands-on project: Design, build, and test an agent system that queries two tools via MCP and summarizes structured outputs.
- Peer review: Evaluate solutions on logging, compliance, and workflow efficiency.
- Showcase: Present team solutions and discuss optimization strategies.

---

## Success Criteria

Participants will be successful if they can:
- Participants can configure and authenticate Microsoft Agents using the Agent 365 SDK.
- Participants can develop and connect an MCP tool server to an agent workflow.
- Participants can design and implement both sequential and concurrent agent workflows in C#.
- Participants can integrate tool calling and structured outputs into multi-step workflows.
- Participants demonstrate awareness of security, observability, and compliance best practices in agent development.

## Additional Resources

- Microsoft Agent 365 Developer Portal
- Visual Studio Code with .NET 8.0 SDK
- Azure AI Foundry and Azure CLI
- Sample MCP tool server template from Microsoft Learn exercises
- Microsoft Entra ID tenant (lab environment)
- Purview and Defender dashboards for monitoring

---

*Generated by AI Workshop Planner on 2025-11-26 14:44:15 UTC*
