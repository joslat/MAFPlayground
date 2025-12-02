# Building Agentic Applications with the Microsoft Agents SDK

## Workshop Details

**Duration:** 4 hours

**Target Audience:** Intermediate developers familiar with C#, .NET, async programming, and basic AI/LLM concepts.

## Overview

This 4-hour hands-on workshop empowers intermediate developers to build intelligent, multi-step agentic applications using the Microsoft Agents SDK. Participants will learn how to architect agent workflows, integrate the Model Context Protocol (MCP), and produce structured outputs for reliable tool interaction. By the end of the workshop, attendees will have built a working multi-step agent that communicates with MCP servers and returns structured results.

## Workshop Modules

### Module 1: Introduction to Microsoft Agents SDK and Agent Architecture

**Duration:** 64 minutes

**Learning Objectives:**
- Understand what Microsoft Agents are and their role in AI-powered applications.
- Explore the SDK architecture and supported agent features.
- Set up the development environment for building agentic applications.

**Content Sources:**
- [Introduction to Microsoft Agents SDK](https://learn.microsoft.com/agents-sdk/intro)
  - *Participant-guided learning through official introduction materials.*

**Activities:**
- Guided walkthrough of the Agents SDK documentation.
- Setup a sample Agents SDK project in Visual Studio using provided templates.
- Group discussion: Identify potential agent features relevant to participants’ projects.

---

### Module 2: Building Agentic Workflows

**Duration:** 73 minutes

**Learning Objectives:**
- Learn the concept of multi-step agentic workflows.
- Understand how task orchestration and asynchronous execution are managed.
- Implement a workflow that coordinates multiple agent actions.

**Content Sources:**
- [Building Agentic Workflows](https://learn.microsoft.com/agents-sdk/workflows)
  - *Reference guide and implementation examples for workflow creation.*

**Activities:**
- Hands-on coding: Build a two-step workflow using the Agents SDK.
- Experiment with error handling and retry logic in multi-step flows.
- Peer review session: Share implementation approaches and optimizations.

---

### Module 3: MCP Integration for Context Enhancement

**Duration:** 55 minutes

**Learning Objectives:**
- Understand what Model Context Protocol (MCP) is and how it improves agent context awareness.
- Integrate an MCP server into an agentic workflow.
- Enable rich context-driven interactions between the agent and external data sources.

**Content Sources:**
- [ModelContextProtocol/servers](https://github.com/modelcontextprotocol/servers)
  - *Hands-on reference and code samples to learn MCP integration.*

**Activities:**
- Clone MCP server sample from GitHub and run locally.
- Modify the earlier workflow to include MCP context querying.
- Test and validate enhanced contextual responses using MCP data.

---

### Module 4: Structured Outputs and Tool Calling

**Duration:** 57 minutes

**Learning Objectives:**
- Understand structured outputs and their importance in agent predictability.
- Learn how to define schema for outputs and integrate them with downstream tools.
- Build a workflow that reliably calls external functions using structured results.

**Content Sources:**
- [Structured Outputs with AI](https://learn.microsoft.com/ai/structured-outputs)
  - *Documentation reference for implementing structured outputs.*

**Activities:**
- Define schema for agent responses in JSON format.
- Integrate tool-calling using structured data outputs.
- Run end-to-end test of workflow generating structured tool-ready responses.

---

### Module 5: Hands-on Lab: Complete Agentic Application

**Duration:** 79 minutes

**Learning Objectives:**
- Apply all learned principles—multi-step workflows, MCP integration, structured outputs—in a complete application.
- Collaborate in small groups to build and test a full-featured agent.
- Debug common issues and share learnings.

**Content Sources:**
- [microsoft/agents-sdk-samples](https://github.com/microsoft/agents-sdk-samples)
  - *Hands-on sample repository for completing project exercises.*

**Activities:**
- Build a multi-step agent that integrates MCP and structured outputs.
- Group debugging and performance tuning.
- Final demonstration and discussion on deployment considerations.

---

## Success Criteria

Participants will be successful if they can:
- Participants can design and implement a multi-step agentic workflow using the Microsoft Agents SDK.
- Agents can successfully query context from MCP servers and apply it in decision logic.
- Agents produce structured outputs conforming to defined schemas.
- Participants demonstrate an end-to-end working agent during the final lab exercise.
- Attendees can describe use cases and extensions for agentic architectures in real-world projects.

## Additional Resources

- Visual Studio or VS Code with .NET SDK installed
- Access to Microsoft Learn and GitHub repositories
- Sample data for MCP integration
- Preconfigured MCP server
- Slide deck summarizing SDK architecture and workflow patterns

---

*Generated by AI Workshop Planner on 2025-11-20 11:52:29 UTC*
