# Building Intelligent Agents with Microsoft Agents SDK and Model Context Protocol

## Workshop Details

**Duration:** 4 hours

**Target Audience:** Intermediate software developers familiar with C#, .NET, asynchronous programming, and basic AI/LLM concepts.

## Overview

This 4-hour hands-on workshop enables intermediate .NET developers to design, build, and integrate intelligent agents using the Microsoft Agents SDK and Model Context Protocol (MCP). Participants will explore the architecture, SDK structure, and integration strategies for building multi-step agentic workflows and tool-based intelligent systems. Through guided labs, developers will gain practical experience in creating, deploying, and extending agents using C#, .NET, and Azure AI services.

## Workshop Modules

### Module 1: Understanding Microsoft Agents and the Model Context Protocol Ecosystem

**Duration:** 50 minutes

**Learning Objectives:**
- Explain the concept and role of AI agents in .NET development
- Describe the architecture and components of the Microsoft Agents SDK
- Understand how the Model Context Protocol (MCP) enables context-aware AI integrations

**Content Sources:**
- [Agents - Conceptual Overview for .NET](https://learn.microsoft.com/en-us/dotnet/ai/conceptual/agents)
  - *Foundation overview of AI Agent concepts and context in .NET*
- [Microsoft Agent 365 SDK Overview](https://learn.microsoft.com/en-us/microsoft-agent-365/developer/)
  - *Introduction to Microsoft Agents architecture and enterprise integration patterns*

**Activities:**
- Group discussion: Identify real-world use cases for intelligent agents
- Quick interactive quiz: Match agent framework components with their functions
- Hands-on: Explore Microsoft Agent 365 Quickstarts to instantiate a basic agent in .NET

---

### Module 2: Getting Started with MCP and Agents SDK in .NET

**Duration:** 55 minutes

**Learning Objectives:**
- Implement a sample MCP client-server architecture using C#
- Integrate Model Context Protocol with .NET AI and the Agents SDK
- Understand interoperability between MCP and Azure AI Foundry

**Content Sources:**
- [Get started with .NET AI and the Model Context Protocol](https://learn.microsoft.com/en-us/dotnet/ai/get-started-mcp)
  - *Hands-on lab introducing MCP architecture and usage in .NET*
- [Build Agents using Model Context Protocol on Azure](https://learn.microsoft.com/en-us/azure/developer/ai/intro-agents-mcp)
  - *Applied lab for developing agents using Azure MCP server and client configuration*

**Activities:**
- Hands-on lab: Configure an MCP client and connect to Azure MCP Server
- Code walkthrough: Implement an MCP message exchange between the agent and a service
- Checkpoint review: Verify MCP handshake and data flow using debugging tools

---

### Module 3: Designing Multi-Step Agentic Workflows and Introducing Semantic Kernel

**Duration:** 60 minutes

**Learning Objectives:**
- Use Semantic Kernel for managing memory, orchestration, and reasoning tasks
- Implement multi-agent workflows using Agents SDK and Semantic Kernel
- Apply dependency injection and plugin/tool registration for dynamic behavior

**Content Sources:**
- [Use Semantic Kernel and Agent Framework in Agents SDK](https://learn.microsoft.com/en-us/microsoft-365/agents-sdk/using-semantic-kernel-agent-framework)
  - *Lab for multi-agent workflows and integrations with Semantic Kernel*

**Activities:**
- Hands-on lab: Create a simple multi-agent workflow using Semantic Kernel
- Pair programming session: Extend the agent to handle contextual conversation memory
- Discussion: Compare orchestration strategies across agent architectures

---

### Module 4: Building Tools and Integrating Context with the MCP Tool API

**Duration:** 60 minutes

**Learning Objectives:**
- Develop custom tools for agents using the MCP tool API
- Handle context exchanges, threads, and message management using C# asynchronous programming
- Integrate external services via REST APIs through MCP mechanisms

**Content Sources:**
- [How to use the Model Context Protocol tool (preview) (C#)](https://learn.microsoft.com/en-us/azure/ai-foundry/agents/how-to/tools/model-context-protocol-samples?view=foundry-classic)
  - *Hands-on lab for building and integrating agent tools and testing asynchronous operations*

**Activities:**
- Hands-on lab: Implement a new tool resource in the Agents SDK
- Code exercise: Test asynchronous message handling between PersistentAgentsClient and MCP server
- Debugging challenge: Identify and fix common MCP tool invocation errors

---

### Module 5: Mastering Communication and Activity Protocols in Microsoft Agents

**Duration:** 55 minutes

**Learning Objectives:**
- Understand the Activity Protocol and its components (TurnContext, Activities, Events)
- Implement conversation handling using Activity schema and message types
- Integrate Microsoft Teams and WebChat adapters for multi-channel interaction

**Content Sources:**
- [Understanding the Activity Protocol](https://learn.microsoft.com/en-us/microsoft-365/agents-sdk/activity-protocol#what-is-an-activity)
  - *Deep dive into conversation management and Activity schema*

**Activities:**
- Guide: Explore and modify sample Activity schema definitions
- Hands-on: Implement a message handler using TurnContext and Activity classes
- Challenge: Build a small prototype that echoes messages across different activity types

---

## Success Criteria

Participants will be successful if they can:
- Participants can configure and connect an MCP client-server environment using .NET
- Participants create at least one multi-step agent workflow leveraging Semantic Kernel
- Participants successfully implement and test a tool using MCP tool API
- Participants demonstrate message handling using the Activity Protocol
- Participants understand end-to-end architecture of an intelligent agent system in Microsoft Agents SDK

## Additional Resources

- Visual Studio 2022 with .NET 8 SDK
- Azure subscription with AI Foundry access
- Semantic Kernel NuGet packages
- Microsoft Agent 365 SDK
- Sample MCP servers and clients
- GitHub repository for code samples
- Microsoft Learn modules for reference

---

*Generated by AI Workshop Planner on 2025-12-06 10:57:01 UTC*
