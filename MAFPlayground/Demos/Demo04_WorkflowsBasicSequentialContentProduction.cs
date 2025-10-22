// SPDX-License-Identifier: LicenseRef-MAFPlayground-NPU-1.0-CH
// Copyright (c) 2025 Jose Luis Latorre

using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace MAFPlayground.Demos;

/// <summary>
/// Demo 04: Content Production Pipeline
/// 
/// Demonstrates a sequential workflow where specialized AI agents work together in a pipeline.
/// Each agent has a specific role and the output of one becomes the input of the next.
/// 
/// Story: "Let's build a content pipeline where each agent specializes in one thing."
/// 
/// Pipeline Flow:
/// 1. Research Agent → Gathers facts about a topic
/// 2. Writer Agent → Creates engaging content from research
/// 3. Editor Agent → Polishes and perfects the final piece
/// </summary>
internal static class Demo04_WorkflowsBasicSequentialContentProduction
{
    public static async Task Execute()
    {
        Console.WriteLine("\n=== DEMO 04: CONTENT PRODUCTION PIPELINE ===\n");

        // Create the AzureOpenAIClient using the lazy config from AIConfig
        var azureClient = new AzureOpenAIClient(AIConfig.Endpoint, AIConfig.KeyCredential);

        // Get the chat client
        var chatClient = azureClient
            .GetChatClient(AIConfig.ModelDeployment)
            .AsIChatClient();

        // Phase 1: Create Three Specialized Agents
        Console.WriteLine("📋 Setting up specialized agents...\n");

        // Agent 1: Researcher
        AIAgent researcher = new ChatClientAgent(
            chatClient,
            new ChatClientAgentOptions
            {
                Name = "Researcher",
                Instructions = """
                    You are a research specialist. When given a topic:
                    - Identify 3-5 key facts or insights
                    - Find interesting angles or trends
                    - Provide factual, concise bullet points
                    - No fluff, just useful information for a writer
                    """
            });

        // Agent 2: Writer
        AIAgent writer = new ChatClientAgent(
            chatClient,
            new ChatClientAgentOptions
            {
                Name = "Writer",
                Instructions = """
                    You are a creative content writer. Take the research provided and:
                    - Write an engaging LinkedIn post (150-200 words)
                    - Start with a hook that grabs attention
                    - Use storytelling and clear structure
                    - End with a thought-provoking question or call-to-action
                    - Don't mention it's based on research - make it natural
                    """
            });

        // Agent 3: Editor
        AIAgent editor = new ChatClientAgent(
            chatClient,
            new ChatClientAgentOptions
            {
                Name = "Editor",
                Instructions = """
                    You are a professional editor. Review the content and:
                    - Fix any grammar or clarity issues
                    - Ensure punchy, engaging tone
                    - Add relevant emojis (2-3 max) for LinkedIn
                    - Verify strong opening and closing
                    - Make it publication-ready
                    Return ONLY the final polished version.
                    """
            });

        Console.WriteLine("✅ Agents created:");
        Console.WriteLine("   • Researcher - Gathers key facts and insights");
        Console.WriteLine("   • Writer - Creates engaging narratives");
        Console.WriteLine("   • Editor - Polishes to perfection\n");

        // Phase 2: Build Sequential Workflow
        Console.WriteLine("🔧 Building sequential pipeline...\n");

        Workflow contentPipeline = AgentWorkflowBuilder
            .BuildSequential(researcher, writer, editor);

        Console.WriteLine("✅ Pipeline assembled: Researcher → Writer → Editor\n");
        Console.WriteLine(new string('=', 80));

        // Phase 3: Run the Pipeline with Streaming to Show Each Agent's Output
        Console.WriteLine("\n📝 Topic: Benefits of AI Agent Frameworks\n");
        Console.WriteLine("🚀 Starting content production pipeline...\n");
        Console.WriteLine(new string('=', 80));

        var topic = "Create a LinkedIn post about the benefits of using AI agent frameworks in enterprise software development.";

        var initialMessages = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.User, topic)
        };

        // Execute with streaming to capture each agent's output
        await using StreamingRun run = await InProcessExecution.StreamAsync(contentPipeline, initialMessages);
        await run.TrySendMessageAsync(new TurnToken(emitEvents: true));

        string? currentAgent = null;
        string currentOutput = "";

        await foreach (WorkflowEvent evt in run.WatchStreamAsync())
        {
            if (evt is AgentRunUpdateEvent agentUpdate)
            {
                // Detect agent change
                if (agentUpdate.Update.AuthorName != currentAgent)
                {
                    // Print previous agent's output if any
                    if (currentAgent != null && !string.IsNullOrWhiteSpace(currentOutput))
                    {
                        Console.WriteLine("\n" + new string('-', 80));
                        Console.WriteLine();
                    }

                    currentAgent = agentUpdate.Update.AuthorName;
                    currentOutput = "";

                    // Print header for new agent
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"╔═══ {currentAgent} Output ═══╗");
                    Console.ResetColor();
                    Console.WriteLine();
                }

                // Accumulate and display agent output
                if (!string.IsNullOrEmpty(agentUpdate.Update.Text))
                {
                    Console.Write(agentUpdate.Update.Text);
                    currentOutput += agentUpdate.Update.Text;
                }
            }
            else if (evt is WorkflowOutputEvent output)
            {
                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine(new string('=', 80));
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("=== 🎉 FINAL PUBLISHED CONTENT ===");
                Console.ResetColor();
                Console.WriteLine();
                
                var finalContent = output.As<List<ChatMessage>>();
                if (finalContent != null && finalContent.Count > 0)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine(finalContent.Last().Text);
                    Console.ResetColor();
                }
                
                Console.WriteLine();
                Console.WriteLine(new string('=', 80));
            }
        }

        Console.WriteLine("\n💡 The Power of Sequential Workflows:");
        Console.WriteLine("   • Three specialists, each doing ONE thing brilliantly");
        Console.WriteLine("   • The output of one becomes the input of the next");
        Console.WriteLine("   • Composition over complexity");
        Console.WriteLine("   • Publication-ready content in seconds\n");

        Console.WriteLine("🎯 Real-World Applications:");
        Console.WriteLine("   📝 Content Marketing: Blog posts, social media, newsletters at scale");
        Console.WriteLine("   📊 Report Generation: Data → Analysis → Executive Summary");
        Console.WriteLine("   📧 Email Campaigns: Research → Draft → Personalize → Compliance");
        Console.WriteLine("   📄 Documentation: Technical specs → User docs → Localization");
        Console.WriteLine("   🎯 Product Descriptions: Features → Benefits → SEO-optimized copy");

        Console.WriteLine("\n✅ Demo Complete: Sequential content pipeline executed successfully!");
    }
}