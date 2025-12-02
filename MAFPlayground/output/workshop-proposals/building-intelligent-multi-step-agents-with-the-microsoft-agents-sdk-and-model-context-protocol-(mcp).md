# Building Intelligent Multi-step Agents with the Microsoft Agents SDK and Model Context Protocol (MCP)

## Workshop Details

**Duration:** 4 hours

**Target Audience:** Intermediate developers familiar with C#, .NET, async programming, and basic AI/LLM concepts.

## Overview

This 4-hour intermediate-level workshop guides developers through the architecture, design, and implementation of intelligent multi-step agents using the Microsoft Agents SDK with integrated Model Context Protocol (MCP). Participants will gain hands-on experience building agentic workflows that utilize structured outputs, tool calling, and the MCP framework for seamless model integration. By the end, developers will be able to construct and extend their own agent applications that orchestrate multiple steps and tools across contexts.

## Workshop Modules

### Module 1: Microsoft Agents Architecture and MCP Fundamentals

**Duration:** 60 minutes

**Learning Objectives:**
- Understand the core architecture of the Microsoft Agents SDK and Agent 365 platform
- Explain the purpose and function of the Model Context Protocol (MCP)
- Identify scenarios and components for integrating MCP with Agents

**Content Sources:**
- [Microsoft Agent 365 SDK Overview](https://learn.microsoft.com/en-us/microsoft-agent-365/developer/)
  - *Introduction and conceptual overview*
- [Get started with .NET AI and the Model Context Protocol](https://learn.microsoft.com/en-us/dotnet/ai/get-started-mcp)
  - *Hands-on guided introduction*

**Activities:**
- Presentation and group discussion on Agent architecture and MCP
- Guided lab: Set up a .NET environment and explore MCP client-server interactions using the Microsoft Learn tutorial
- Q&A: Discuss integration scenarios based on participants’ existing projects

---

### Module 2: Building and Configuring Agents with the MCP SDK

**Duration:** 90 minutes

**Learning Objectives:**
- Install and configure Agents SDK with MCP integration
- Implement a basic MCP agent to communicate with multiple tools
- Configure Azure OpenAI and container-based deployment

**Content Sources:**
- [Build Agents using Model Context Protocol on Azure](https://learn.microsoft.com/en-us/azure/developer/ai/intro-agents-mcp)
  - *Hands-on implementation*
- [How to use the Model Context Protocol tool (preview)](https://learn.microsoft.com/en-us/azure/ai-foundry/agents/how-to/tools/model-context-protocol-samples?view=foundry-classic)
  - *Reference for tool calling and environment configuration*

**Activities:**
- Live coding demo: Create a basic agent connected to an MCP server on Azure
- Lab exercise: Deploy the configured MCP agent to Azure Container Apps
- Team exercise: Configure custom tools and test automated tool invocation

---

### Module 3: Agentic Workflows, Tool Orchestration, and Structured Outputs

**Duration:** 90 minutes

**Learning Objectives:**
- Develop multi-step, AI-driven workflows using MCP and the Agents SDK
- Orchestrate multiple agents and tools with structured responses
- Use Semantic Kernel and dependency injection for modular extensibility

**Content Sources:**
- [Use Semantic Kernel and Agent Framework in Agents SDK](https://learn.microsoft.com/en-us/microsoft-365/agents-sdk/using-semantic-kernel-agent-framework)
  - *Hands-on building block*
- [Microsoft Agent Framework and MCP](https://github.com/deeparajasabeson/microsoftagentframework-and-mcp)
  - *Code-based lab for structured output and tool calling*

**Activities:**
- Hands-on lab: Extend an MCP agent to execute a multi-step conversation-driven workflow
- Workshop challenge: Build an agent that uses structured JSON outputs to trigger multiple tools via the MCP interface
- Code review session: Compare workflow orchestration strategies and share insights

---

### Module 4: Scaling, Observability, and Best Practices for Agentic Systems

**Duration:** 60 minutes

**Learning Objectives:**
- Implement observability using OpenTelemetry and event logging
- Understand compliance and governance integration within Microsoft 365
- Review performance optimization and extensibility patterns for agent workflow systems

**Content Sources:**
- [Agent Framework (GitHub)](https://github.com/iki-cpu/agent-framework)
  - *Demonstration of orchestration and scaling patterns*
- [Microsoft Agent 365 SDK Overview](https://learn.microsoft.com/en-us/microsoft-agent-365/developer/)
  - *Reference for lifecycle, identity, and governance integrations*

**Activities:**
- Group design exercise: Design an observability plan for a production agent
- Mini-lab: Extend existing lab agent with basic telemetry and logging
- Closing discussion: Lessons learned and next steps for project implementation

---

## Success Criteria

Participants will be successful if they can:
- Participants can explain how MCP integrates within the Microsoft Agents SDK
- Participants can build and deploy a basic MCP agent using .NET and Azure
- Participants can design a multi-step agent workflow that performs structured tasks via tool calling
- Participants demonstrate the ability to implement structured outputs and observability within their agents
- Participants complete the final lab successfully and present their working agent workflow

## Additional Resources

- Visual Studio or VS Code (latest version)
- Microsoft .NET 8 SDK
- Azure subscription (for MCP agent deployment)
- Access to the Microsoft Learn modules and GitHub repositories provided in content sources
- Sample agent templates and lab guide
- Semantic Kernel SDK

---

*Generated by AI Workshop Planner on 2025-11-26 15:47:10 UTC*
