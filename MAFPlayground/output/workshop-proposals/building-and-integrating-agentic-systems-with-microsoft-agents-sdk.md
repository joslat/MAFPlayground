# Building and Integrating Agentic Systems with Microsoft Agents SDK

## Workshop Details

**Duration:** 4 hours

**Target Audience:** Intermediate developers familiar with C#, .NET, asynchronous programming, and basic AI/LLM concepts.

## Overview

This 4-hour intermediate-level workshop equips developers with the skills to design, build, and integrate agentic systems using the Microsoft Agents SDK. Participants will explore the Agents architecture, Model Context Protocol (MCP) integration, multi-step agentic workflows, and structured outputs with tool calling. Through guided labs and interactive coding exercises leveraging official Microsoft Learn modules and GitHub repositories, developers will deepen their understanding of how agentic systems operate and how to embed them into .NET solutions.

## Workshop Modules

### Module 1: Introduction to Agentic Systems and Microsoft Agents SDK

**Duration:** 45 minutes

**Learning Objectives:**
- Understand the concept of agentic systems and their role in intelligent applications.
- Explore the architecture and capabilities of the Microsoft Agents SDK.
- Set up the development environment and tools for agent development.

**Content Sources:**
- [Microsoft Agents SDK Documentation](https://learn.microsoft.com/en-us/agents-sdk)
  - *Reference for SDK overview and installation guidance.*

**Activities:**
- Group discussion on agentic system patterns and use cases.
- Guided setup of SDK environment in Visual Studio.
- Short quiz on key SDK architecture components.

---

### Module 2: Understanding and Implementing Model Context Protocol (MCP)

**Duration:** 60 minutes

**Learning Objectives:**
- Describe the purpose and function of the Model Context Protocol (MCP).
- Integrate MCP with Agents SDK for contextual communication between agents and models.
- Implement a simple MCP-powered agent interaction in C#.

**Content Sources:**
- [Microsoft Learn: Implement Contextual Data Exchange Using MCP](https://learn.microsoft.com/en-us/training/modules/mcp-integration)
  - *Conceptual and implementation guidance for MCP integration.*

**Activities:**
- Live coding demo of MCP integration.
- Pair exercise to implement MCP for a sample use case.
- Troubleshooting lab: diagnosing issues with MCP data exchange.

---

### Module 3: Designing Multi-Step Agentic Workflows

**Duration:** 75 minutes

**Learning Objectives:**
- Understand multi-step workflows and orchestration patterns in agentic systems.
- Use Agents SDK to coordinate multi-agent communication.
- Implement a multi-step workflow to perform a complex task using multiple agents.

**Content Sources:**
- [Agents SDK Samples](https://github.com/microsoft/agents-sdk-samples)
  - *Hands-on lab using provided samples to develop multi-step workflows.*

**Activities:**
- Guided lab using microsoft/agents-sdk-samples repository.
- Participants extend the sample workflow with an additional decision layer.
- Code review and feedback session on workflow design.

---

### Module 4: Structured Outputs and Tool Calling

**Duration:** 60 minutes

**Learning Objectives:**
- Define and handle structured outputs for agent responses.
- Implement tool calling within the agent’s workflow.
- Design agents capable of delegating tasks to external APIs and tools.

**Content Sources:**
- [Microsoft Learn: Advanced Agent Output Structuring](https://learn.microsoft.com/en-us/training/modules/agents-structured-output)
  - *Conceptual understanding and implementation instructions for structured outputs.*
- [Agents SDK Samples](https://github.com/microsoft/agents-sdk-samples)
  - *Hands-on implementation reference for tool calling and structured output.*

**Activities:**
- Hands-on coding lab integrating external tools via agent commands.
- Workshop participants develop a mini agent project showcasing structured output processing.
- Wrap-up discussion on production deployment considerations.

---

### Module 5: Integration, Debugging, and Best Practices

**Duration:** 40 minutes

**Learning Objectives:**
- Integrate Microsoft Agents SDK-powered agents into an existing .NET application.
- Learn debugging strategies specific to asynchronous agentic workflows.
- Review best practices for scalability, performance, and security.

**Content Sources:**
- [Microsoft Learn: Debugging .NET Applications](https://learn.microsoft.com/en-us/training/modules/debug-dotnet)
  - *Reference for debugging strategies in .NET applications.*

**Activities:**
- Integration exercise combining previous module outputs into a functional .NET app.
- Group discussion of challenges faced and resolutions.
- Checklist creation of best practices for agent deployment.

---

## Success Criteria

Participants will be successful if they can:
- Participants can set up and configure the Microsoft Agents SDK environment.
- Participants can implement MCP integration to enable contextual intelligence.
- Participants can design and execute multi-step agent workflows using SDK samples.
- Participants can build agents capable of structured outputs and tool calling.
- Participants successfully integrate a working agent into a .NET application and demonstrate debugging capabilities.
- Participants can articulate at least three best practices for developing scalable agentic systems.

## Additional Resources

- Visual Studio or VS Code with .NET SDK installed
- Microsoft Agents SDK
- Access to Microsoft Learn platform
- Access to the microsoft/agents-sdk-samples GitHub repository
- Azure subscription for optional cloud deployment

---

*Generated by AI Workshop Planner on 2025-11-20 11:42:55 UTC*
