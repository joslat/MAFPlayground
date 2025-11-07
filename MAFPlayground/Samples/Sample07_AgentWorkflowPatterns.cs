//// Copyright (c) Microsoft. All rights reserved.
//// SPDX-License-Identifier: MIT

//using Azure.AI.OpenAI;
//using MAFPlayground.Utils;
//using Microsoft.Agents.AI;
//using Microsoft.Agents.AI.Workflows;
//using Microsoft.Extensions.AI;
//using OpenAI;
//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text.Json;
//using System.Threading.Tasks;

//namespace MAFPlayground.Samples;

///// <summary>
///// This sample introduces the use of AI agents as executors within a workflow,
///// using <see cref="AgentWorkflowBuilder"/> to compose the agents into one of
///// several common patterns: Sequential, Concurrent, Handoffs, and Group Chat.
///// </summary>
///// <remarks>
///// Pre-requisites:
///// - An Azure OpenAI chat completion deployment must be configured.
///// </remarks>
//internal static class Sample07_AgentWorkflowPatterns
//{
//    public static async Task Execute(string workflowType)
//    {
//        // Set up the Azure OpenAI client using AIConfig
//        var azureClient = new AzureOpenAIClient(AIConfig.Endpoint, AIConfig.KeyCredential);
//        var deploymentName = "gpt-4o-mini";
//        IChatClient chatClient = azureClient.GetChatClient(deploymentName).AsIChatClient();

//        Console.WriteLine("=== Sample 07: Agent Workflow Patterns ===");
//        Console.WriteLine();
//        if (string.IsNullOrWhiteSpace(workflowType))
//        {
//            Console.WriteLine("No workflow type provided. Please choose one of the following:");
//            Console.WriteLine("  'sequential' - Agents process messages one after another.");
//            Console.WriteLine("  'concurrent' - Agents process the same input in parallel.");
//            Console.WriteLine("  'handoffs'   - A triage agent routes questions to specialist agents.");
//            Console.WriteLine("  'groupchat'  - Agents take turns in a round-robin group chat.");
//            Console.WriteLine();
//            Console.Write("Choose workflow type ('sequential', 'concurrent', 'handoffs', 'groupchat'): ");
//            workflowType = Console.ReadLine();
//        }
//        else
//        {
//            Console.WriteLine($"Workflow type provided: '{workflowType}'");
//        }

//        switch (workflowType?.ToLowerInvariant())
//        {
//            case "sequential":
//                await RunSequentialWorkflow(chatClient);
//                break;

//            case "concurrent":
//                await RunConcurrentWorkflow(chatClient);
//                break;

//            case "handoffs":
//                await RunHandoffsWorkflow(chatClient);
//                break;

//            case "groupchat":
//                await RunGroupChatWorkflow(chatClient);
//                break;

//            default:
//                Console.WriteLine("Invalid workflow type. Please choose 'sequential', 'concurrent', 'handoffs', or 'groupchat'.");
//                break;
//        }
//    }

//    /// <summary>
//    /// Runs a sequential workflow where agents process messages one after another.
//    /// </summary>
//    private static async Task RunSequentialWorkflow(IChatClient chatClient)
//    {
//        Console.WriteLine("\n=== Sequential Translation Workflow ===");
//        Console.WriteLine("Input will be translated through French → Spanish → English sequentially.\n");

//        var workflow = AgentWorkflowBuilder.BuildSequential(
//            from lang in new[] { "French", "Spanish", "English" }
//            select GetTranslationAgent(lang, chatClient));

//        // Visualize the workflow
//        WorkflowVisualizerTool.PrintAll(workflow, "Sequential Translation Workflow");

//        await RunWorkflowAsync(workflow, new List<ChatMessage> { new(ChatRole.User, "Hello, world!") });
//    }

//    /// <summary>
//    /// Runs a concurrent workflow where agents process the same input in parallel.
//    /// </summary>
//    private static async Task RunConcurrentWorkflow(IChatClient chatClient)
//    {
//        Console.WriteLine("\n=== Concurrent Translation Workflow ===");
//        Console.WriteLine("Input will be translated to French, Spanish, and English concurrently.\n");

//        var workflow = AgentWorkflowBuilder.BuildConcurrent(
//            from lang in new[] { "French", "Spanish", "English" }
//            select GetTranslationAgent(lang, chatClient));

//        // Visualize the workflow
//        WorkflowVisualizerTool.PrintAll(workflow, "Concurrent Translation Workflow");

//        // ISSUE: What is the "BATCHER" executor that appears in the Mermaid and DOT diagrams?
//        //flowchart TD
//        //    Start["Start (Start)"];
//        //    4eaedcde6c56467e8fd92a68b98fb057["4eaedcde6c56467e8fd92a68b98fb057"];
//        //    cc0c636b920b4dd1ba5000ccfdadf6de["cc0c636b920b4dd1ba5000ccfdadf6de"];
//        //    1524c98619994e4fbb45bd2b26414a4e["1524c98619994e4fbb45bd2b26414a4e"];
//        //    Batcher / 4eaedcde6c56467e8fd92a68b98fb057["Batcher/4eaedcde6c56467e8fd92a68b98fb057"];
//        //    Batcher / cc0c636b920b4dd1ba5000ccfdadf6de["Batcher/cc0c636b920b4dd1ba5000ccfdadf6de"];
//        //    Batcher / 1524c98619994e4fbb45bd2b26414a4e["Batcher/1524c98619994e4fbb45bd2b26414a4e"];
//        //    ConcurrentEnd["ConcurrentEnd"];

//        //    fan_in::ConcurrentEnd::BAD20FD4((fan -in))
//        //    Batcher / 1524c98619994e4fbb45bd2b26414a4e-- > fan_in::ConcurrentEnd::BAD20FD4;
//        //    Batcher / 4eaedcde6c56467e8fd92a68b98fb057-- > fan_in::ConcurrentEnd::BAD20FD4;
//        //    Batcher / cc0c636b920b4dd1ba5000ccfdadf6de-- > fan_in::ConcurrentEnd::BAD20FD4;
//        //    fan_in::ConcurrentEnd::BAD20FD4-- > ConcurrentEnd;
//        //    Start-- > 4eaedcde6c56467e8fd92a68b98fb057;
//        //    Start-- > cc0c636b920b4dd1ba5000ccfdadf6de;
//        //    Start-- > 1524c98619994e4fbb45bd2b26414a4e;
//        //    4eaedcde6c56467e8fd92a68b98fb057-- > Batcher / 4eaedcde6c56467e8fd92a68b98fb057;
//        //    cc0c636b920b4dd1ba5000ccfdadf6de-- > Batcher / cc0c636b920b4dd1ba5000ccfdadf6de;
//        //    1524c98619994e4fbb45bd2b26414a4e-- > Batcher / 1524c98619994e4fbb45bd2b26414a4e;

//        //  IMHO , the "BATCHER" executor appears to be an internal component used to manage the concurrent execution of multiple agents.
//        // It likely handles the distribution of input messages to the various agents and collects their outputs.
//        // this could be simplified on just a SINGLE node that fans in all the agents responses to the final output node.

//        await RunWorkflowAsync(workflow, new List<ChatMessage> { new(ChatRole.User, "Hello, world!") });
//    }

//    /// <summary>
//    /// Runs a handoff workflow where a triage agent routes questions to specialist agents.
//    /// </summary>
//    private static async Task RunHandoffsWorkflow(IChatClient chatClient)
//    {
//        Console.WriteLine("\n=== Handoffs Workflow (Homework Assistant) ===");
//        Console.WriteLine("A triage agent will route your questions to history or math specialists.\n");

//        ChatClientAgent historyTutor = new(
//            chatClient,
//            "You provide assistance with historical queries. Explain important events and context clearly. Only respond about history.",
//            "history_tutor",
//            "Specialist agent for historical questions");

//        ChatClientAgent mathTutor = new(
//            chatClient,
//            "You provide help with math problems. Explain your reasoning at each step and include examples. Only respond about math.",
//            "math_tutor",
//            "Specialist agent for math questions");

//        ChatClientAgent triageAgent = new(
//            chatClient,
//            "You determine which agent to use based on the user's homework question. ALWAYS handoff to another agent.",
//            "triage_agent",
//            "Routes messages to the appropriate specialist agent");

//        var workflow = AgentWorkflowBuilder.CreateHandoffBuilderWith(triageAgent)
//            .WithHandoffs(triageAgent, [ mathTutor, historyTutor])
//            .WithHandoffs([ mathTutor, historyTutor ], triageAgent)
//            .Build();

//        // Visualize the workflow
//        WorkflowVisualizerTool.PrintAll(workflow, "Handoffs Workflow");

//        List<ChatMessage> messages = new();
//        while (true)
//        {
//            Console.Write("\nQ (or 'exit' to quit): ");
//            string? input = Console.ReadLine();
            
//            if (string.IsNullOrWhiteSpace(input) || input.Equals("exit", StringComparison.OrdinalIgnoreCase))
//            {
//                Console.WriteLine("Exiting handoffs workflow.");
//                break;
//            }

//            messages.Add(new ChatMessage(ChatRole.User, input));
//            messages.AddRange(await RunWorkflowAsync(workflow, messages));
//            //var newMessages = await RunWorkflowAsync(workflow, messages);
//            //messages.AddRange(newMessages);
//        }
//    }

//    /// <summary>
//    /// Runs a group chat workflow where agents take turns in a round-robin fashion.
//    /// </summary>
//    private static async Task RunGroupChatWorkflow(IChatClient chatClient)
//    {
//        Console.WriteLine("\n=== Group Chat Workflow ===");
//        Console.WriteLine("Agents will discuss the input in a round-robin group chat (max 5 iterations).\n");

//        var workflow = AgentWorkflowBuilder
//            .CreateGroupChatBuilderWith(agents => new AgentWorkflowBuilder.RoundRobinGroupChatManager(agents) { MaximumIterationCount = 5 })
//            .AddParticipants(from lang in new[] { "French", "Spanish", "English" } select GetTranslationAgent(lang, chatClient))
//            .Build();

//        // Visualize the workflow
//        WorkflowVisualizerTool.PrintAll(workflow, "Group Chat - RoundRobin - Workflow");

//        await RunWorkflowAsync(workflow, new List<ChatMessage> { new(ChatRole.User, "Hello, world!") });
//    }

//    /// <summary>
//    /// Executes the workflow and prints streaming updates.
//    /// </summary>
//    private static async Task<List<ChatMessage>> RunWorkflowAsync(Workflow workflow, List<ChatMessage> messages)
//    {
//        string? lastExecutorId = null;

//        await using StreamingRun run = await InProcessExecution.StreamAsync(workflow, messages);
//        await run.TrySendMessageAsync(new TurnToken(emitEvents: true));
        
//        await foreach (WorkflowEvent evt in run.WatchStreamAsync().ConfigureAwait(false))
//        {
//            if (evt is AgentRunUpdateEvent e)
//            {
//                if (e.ExecutorId != lastExecutorId)
//                {
//                    lastExecutorId = e.ExecutorId;
//                    Console.WriteLine();
//                    Console.WriteLine($"[{e.ExecutorId}]:");
//                }

//                Console.Write(e.Update.Text);
                
//                if (e.Update.Contents.OfType<FunctionCallContent>().FirstOrDefault() is FunctionCallContent call)
//                {
//                    Console.WriteLine();
//                    Console.WriteLine($"  [Calling function '{call.Name}' with arguments: {JsonSerializer.Serialize(call.Arguments)}]");
//                }
//            }
//            else if (evt is WorkflowOutputEvent output)
//            {
//                Console.WriteLine();
//                Console.WriteLine("\n=== Workflow Completed ===\n");
//                return output.As<List<ChatMessage>>() ?? new List<ChatMessage>();
//            }
//        }

//        return new List<ChatMessage>();
//    }

//    /// <summary>
//    /// Creates a translation agent for the specified target language.
//    /// </summary>
//    private static ChatClientAgent GetTranslationAgent(string targetLanguage, IChatClient chatClient) =>
//        new(
//            chatClient,
//            $"You are a translation assistant who only responds in {targetLanguage}. Respond to any " +
//            $"input by outputting the name of the input language and then translating the input to {targetLanguage}.");
//}