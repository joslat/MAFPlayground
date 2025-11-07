//// SPDX-License-Identifier: LicenseRef-MAFPlayground-NPU-1.0-CH
//// Copyright (c) 2025 Jose Luis Latorre

//using Azure.AI.OpenAI;
//using MAFPlayground.Utils;
//using Microsoft.Agents.AI;
//using Microsoft.Agents.AI.Workflows;
//using Microsoft.Extensions.AI;
//using System;
//using System.Collections.Generic;
//using System.Threading.Tasks;

//namespace MAFPlayground.Samples;

///// <summary>
///// This sample demonstrates a group chat workflow using the Round Robin pattern.
///// 
///// In this pattern, agents take turns processing messages in a predetermined order,
///// continuing until a termination condition is met (max iterations or specific outcome).
///// 
///// Workflow:
///// 1. Create two specialized agents (Writer and Editor)
///// 2. Configure Round Robin group chat manager with max iterations
///// 3. Writer creates content, Editor provides feedback
///// 4. Writer improves based on feedback, creating an iterative refinement loop
///// 
///// This is useful for:
///// - Collaborative content creation
///// - Iterative refinement workflows
///// - Writer-editor collaboration
///// - Sequential review and improvement processes
///// </summary>
///// <remarks>
///// Pre-requisites:
///// - An Azure OpenAI chat completion deployment must be configured.
///// - Based on: https://github.com/luisquintanilla/hello-world-agents
///// </remarks>
//internal static class Sample12_GroupChatRoundRobin
//{
//    public static async Task Execute()
//    {
//        Console.WriteLine("=== Sample 12: Group Chat with Round Robin ===");
//        Console.WriteLine("Two agents will collaborate on writing and refining a short story.\n");

//        // Set up the Azure OpenAI client using AIConfig
//        var azureClient = new AzureOpenAIClient(AIConfig.Endpoint, AIConfig.KeyCredential);
//        var deploymentName = AIConfig.ModelDeployment;
//        IChatClient chatClient = azureClient.GetChatClient(deploymentName).AsIChatClient();

//        // ====================================
//        // Step 1: Create specialized agents
//        // ====================================
//        Console.WriteLine("Creating specialized agents...\n");

//        ChatClientAgent writerAgent = new(
//            chatClient,
//            name: "Writer",
//            instructions: @"You are a creative writer who crafts engaging stories.
//Focus on creating vivid descriptions, interesting characters, and compelling narratives.
//When you receive feedback from the editor, carefully incorporate their suggestions to improve your work.
//Build upon the previous version of the story while addressing the editor's critiques."
//        );

//        ChatClientAgent editorAgent = new(
//            chatClient,
//            name: "Editor",
//            instructions: @"You are a constructive editor who provides specific, actionable feedback on stories.
//Analyze the narrative structure, character development, pacing, and writing style.
//Point out what works well and what could be improved.
//Provide concrete suggestions that the writer can implement.
//Be supportive but honest in your c/*r*/itique."
//        );

//        // ====================================
//        // Step 2: Build Round Robin group chat workflow
//        // ====================================
//        Console.WriteLine("Building Round Robin group chat workflow...\n");

//        var workflow = AgentWorkflowBuilder
//            .CreateGroupChatBuilderWith(agents => new AgentWorkflowBuilder.RoundRobinGroupChatManager(agents) 
//            { 
//                MaximumIterationCount = 6  // 3 rounds: Writer → Editor → Writer → Editor → Writer → Editor
//            })
//            .AddParticipants([writerAgent, editorAgent])
//            .Build();

//        // Visualize the workflow
//        WorkflowVisualizerTool.PrintAll(workflow, "Group Chat - Round Robin Workflow (Writer-Editor Collaboration)");

//        // ====================================
//        // Step 3: Execute the group chat
//        // ====================================
//        Console.WriteLine("\n--- Starting Group Chat ---\n");
//        Console.WriteLine("Task: Collaboratively write and refine a short story about a mysterious library.\n");
//        Console.WriteLine("=".PadRight(80, '='));
//        Console.WriteLine();

//        var initialMessage = new List<ChatMessage> 
//        { 
//            new(ChatRole.User, "Write a short story about a mysterious library where books come to life at midnight. Keep it concise but engaging (around 200 words).")
//        };

//        await using StreamingRun run = await InProcessExecution.StreamAsync(workflow, initialMessage);
//        await run.TrySendMessageAsync(new TurnToken(emitEvents: true));

//        string? currentAgent = null;
//        int turnCount = 0;

//        await foreach (WorkflowEvent evt in run.WatchStreamAsync().ConfigureAwait(false))
//        {
//            if (evt is AgentRunUpdateEvent agentUpdate)
//            {
//                // Detect agent change
//                if (agentUpdate.Update.AuthorName != currentAgent)
//                {
//                    currentAgent = agentUpdate.Update.AuthorName;
//                    turnCount++;
                    
//                    Console.WriteLine();
//                    Console.WriteLine($">>> Turn {turnCount}: {currentAgent}");
//                    Console.WriteLine(new string('-', 80));
//                }

//                // Stream agent response
//                Console.Write(agentUpdate.Update.Text);
//            }
//            else if (evt is WorkflowOutputEvent output)
//            {
//                Console.WriteLine();
//                Console.WriteLine();
//                Console.WriteLine("=".PadRight(80, '='));
//                Console.WriteLine("=== Group Chat Completed ===");
//                Console.WriteLine($"Total turns: {turnCount}");
//                Console.WriteLine("=".PadRight(80, '='));
//            }
//        }

//        Console.WriteLine();
//        Console.WriteLine("✅ Sample 12 Complete: Round Robin group chat collaboration finished!");
//        Console.WriteLine();
//        Console.WriteLine("Key Takeaways:");
//        Console.WriteLine("- Writer and Editor collaborated in alternating turns");
//        Console.WriteLine("- Writer created initial story, then refined it based on Editor's feedback");
//        Console.WriteLine("- The cycle repeated for 3 rounds (6 total turns)");
//        Console.WriteLine("- Each iteration improved the story quality through feedback incorporation");
//    }
//}