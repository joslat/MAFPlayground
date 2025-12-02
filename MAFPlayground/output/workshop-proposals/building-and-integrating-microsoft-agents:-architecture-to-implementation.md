# Building and Integrating Microsoft Agents: Architecture to Implementation

## Workshop Details

**Duration:** 4 hours

**Target Audience:** Intermediate developers with familiarity in C#, .NET, basic AI/LLM concepts, and asynchronous programming.

## Overview

This 4-hour hands-on workshop equips intermediate developers with the skills to design, build, and integrate Microsoft Agents using the Microsoft Agents SDK. Participants will explore agent architecture, integrate using the Model Context Protocol (MCP), design multi-step agentic workflows, and implement structured outputs and tool calling. The workshop leverages official Microsoft SDK samples and guided practical exercises for fast hands-on learning. By the end, attendees will have built and deployed a functional agent integrated into a sample application.

## Workshop Modules

### Module 1: Introduction to Microsoft Agents Architecture

**Duration:** 45 minutes

**Learning Objectives:**
- Understand the high-level architecture and role of Microsoft Agents.
- Explore how Agents interact with AI models and user contexts.
- Identify key SDK components and flow of data between them.

**Content Sources:**
- [Microsoft Agents SDK Overview (from microsoft/agents-sdk-samples)](https://github.com/microsoft/agents-sdk-samples)
  - *Reference for explaining SDK structure and main components.*

**Activities:**
- Instructor-led overview and interactive whiteboard session on agent architecture.
- Group discussion: Mapping traditional chatbot architecture to Microsoft Agents architecture.
- Quick quiz using Mentimeter/Kahoot on Agent architecture concepts.

---

### Module 2: Integrating Microsoft Agents Using Model Context Protocol (MCP)

**Duration:** 60 minutes

**Learning Objectives:**
- Explain the Model Context Protocol and its importance in agent integration.
- Implement a basic MCP-based integration using provided SDK samples.
- Understand context passing and how it affects agent performance.

**Content Sources:**
- [MCP Integration Examples (microsoft/agents-sdk-samples)](https://github.com/microsoft/agents-sdk-samples)
  - *Use as hands-on reference code and integration base.*

**Activities:**
- Walkthrough of MCP integration example using VS Code.
- Hands-on exercise: Configure a simple MCP integration scenario using the SDK samples.
- Peer review: Share and discuss integration approaches.

---

### Module 3: Designing Multi-Step Agentic Workflows

**Duration:** 60 minutes

**Learning Objectives:**
- Understand multi-step workflows and how Agents manage complex tasks.
- Design a workflow schema that breaks down tasks for Agent orchestration.
- Implement a simple multi-step workflow using SDK samples.

**Content Sources:**
- [Workflow Orchestration Sample (microsoft/agents-sdk-samples)](https://github.com/microsoft/agents-sdk-samples)
  - *Adapt workflow sample for exercise implementation.*

**Activities:**
- Interactive diagramming exercise: Designing a multi-step workflow.
- Hands-on coding: Implement multi-step behavior using the SDK sample.
- Testing and debugging session: Validate workflow with test inputs.

---

### Module 4: Structured Outputs and Tool Calling with Microsoft Agents

**Duration:** 45 minutes

**Learning Objectives:**
- Define structured output formats and understand their importance in agent messaging.
- Implement tool calling for API integration or database queries.
- Test structured output handling within a multi-step workflow.

**Content Sources:**
- [Tool Calling and Structured Output Samples (microsoft/agents-sdk-samples)](https://github.com/microsoft/agents-sdk-samples)
  - *Core reference for activity implementation and testing.*

**Activities:**
- Instructor demo: Implement structured outputs and call external tools.
- Hands-on coding: Extend the previous workflow with tool calling implementation.
- Debugging challenge: Identify and fix output structure errors.

---

### Module 5: Deploying and Evaluating Your Microsoft Agent

**Duration:** 30 minutes

**Learning Objectives:**
- Package and deploy an Agent using SDK components.
- Evaluate agent performance and integration quality.
- Discuss next steps for production-level implementation.

**Content Sources:**
- [Deployment Guides and Examples (microsoft/agents-sdk-samples)](https://github.com/microsoft/agents-sdk-samples)
  - *Use deployment instructions and evaluation examples.*

**Activities:**
- Guided deployment: Build and run the agent locally or via Docker.
- Group discussion: Common deployment pitfalls and optimization strategies.
- Wrap-up reflection: How to extend this agent with more complex logic.

---

## Success Criteria

Participants will be successful if they can:
- Participants can explain Microsoft Agents architecture and key SDK components.
- Each participant successfully implements a basic MCP integration.
- Participants design and code a multi-step agentic workflow within the SDK framework.
- Structured outputs and tool calling are successfully executed from the agent.
- Final deployed agent runs and performs designed tasks without errors.

## Additional Resources

- Laptop with VS Code and .NET SDK installed.
- Access to GitHub: microsoft/agents-sdk-samples repository.
- Microsoft Learn modules on AI Agents and MCP concepts.
- Visual collaboration tools (Miro, Jamboard) for diagram exercises.

---

*Generated by AI Workshop Planner on 2025-11-20 11:44:56 UTC*
