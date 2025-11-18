// SPDX-License-Identifier: LicenseRef-MAFPlayground-NPU-1.0-CH
// Copyright (c) 2025 Jose Luis Latorre

using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using System.Text;

namespace MAFPlayground.Demos;

/// <summary>
/// Demo 10: The Dev Master - Multi-MCP Powered Learning Assistant
/// 
/// Demonstrates the power of combining multiple MCP servers to create a comprehensive
/// learning and development assistant. This agent connects to:
/// 
/// 1. **GitHub MCP Server** - For exploring code repositories, commits, issues, and PRs
/// 2. **Microsoft Learn MCP Server** - For accessing official Microsoft documentation and learning content
/// 
/// Together, these MCPs enable the agent to:
/// - Help you understand code patterns by analyzing GitHub repos
/// - Provide official documentation and best practices from Microsoft Learn
/// - Connect real-world code examples with official guidance
/// - Answer technical questions with both practical examples and theoretical knowledge
/// 
/// The agent uses MCP to dynamically discover and use tools from multiple sources,
/// creating a powerful development and learning experience.
/// 
/// Type 'q' or 'quit' to exit the conversation.
/// </summary>
internal static class Demo10_DevMasterMultiMCP
{
    public static async Task Execute()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        Console.WriteLine("\n" + new string('?', 80));
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("=== DEMO 10: THE DEV MASTER - MULTI-MCP LEARNING ASSISTANT ===");
        Console.ResetColor();
        Console.WriteLine(new string('?', 80) + "\n");

        // ====================================
        // Step 1: Connect to GitHub MCP Server
        // ====================================
        Console.WriteLine("?? Connecting to GitHub MCP Server...");
        await using var githubMcpClient = await McpClientFactory.CreateAsync(new StdioClientTransport(new()
        {
            Name = "GitHubMCPServer",
            Command = "npx",
            Arguments = ["-y", "--verbose", "@modelcontextprotocol/server-github"],
        }));
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("? GitHub MCP Server connected!");
        Console.ResetColor();

        var githubTools = await githubMcpClient.ListToolsAsync().ConfigureAwait(false);
        Console.WriteLine($"   Found {githubTools.Count} GitHub tools\n");

        // ====================================
        // Step 2: Connect to Microsoft Learn MCP Server
        // ====================================
        Console.WriteLine("?? Connecting to Microsoft Learn MCP Server...");
        await using var learnMcpClient = await McpClientFactory.CreateAsync(new HttpClientTransport(new()
        {
            Name = "MicrosoftLearnMCPServer",
            Endpoint = new Uri("https://learn.microsoft.com/api/mcp")
        }));
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("? Microsoft Learn MCP Server connected!");
        Console.ResetColor();

        var learnTools = await learnMcpClient.ListToolsAsync().ConfigureAwait(false);
        Console.WriteLine($"   Found {learnTools.Count} Microsoft Learn tools\n");

        // ====================================
        // Step 3: Combine all tools
        // ====================================
        var allTools = new List<AITool>();
        allTools.AddRange(githubTools.Cast<AITool>());
        allTools.AddRange(learnTools.Cast<AITool>());

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"???  Total tools available: {allTools.Count}");
        Console.ResetColor();
        Console.WriteLine($"   • GitHub tools: {githubTools.Count}");
        Console.WriteLine($"   • Microsoft Learn tools: {learnTools.Count}\n");

        // ====================================
        // Step 4: Create the Azure OpenAI chat client
        // ====================================
        var azureClient = new AzureOpenAIClient(AIConfig.Endpoint, AIConfig.KeyCredential);
        var chatClient = azureClient
            .GetChatClient(AIConfig.ModelDeployment)
            .AsIChatClient();

        // ====================================
        // Step 5: Create the Dev Master agent
        // ====================================
        AIAgent devMaster = new ChatClientAgent(
            chatClient,
            new ChatClientAgentOptions
            {
                Name = "DevMaster",
                Instructions = """
                    You are The Dev Master ???? - a brilliant development mentor who combines 
                    practical code examples from GitHub with official Microsoft documentation 
                    to provide comprehensive, actionable guidance.
                    
                    YOUR SUPERPOWERS:
                    
                    ?? GitHub Access:
                    - Explore real-world code repositories
                    - Analyze commits, issues, and pull requests
                    - Find code examples and patterns
                    - Investigate how popular projects solve problems
                    
                    ?? Microsoft Learn Access:
                    - Search official Microsoft documentation
                    - Find best practices and guidance
                    - Access up-to-date technical references
                    - Provide authoritative answers
                    
                    YOUR APPROACH:
                    
                    1. **Understand the Question**
                       - Identify if the user needs code examples, documentation, or both
                       - Clarify ambiguous requests before diving in
                    
                    2. **Strategic Tool Usage**
                       - For "how to" questions: Search Microsoft Learn FIRST for official guidance
                       - For "show me examples": Search GitHub for real implementations
                       - For complex topics: Combine both! Show docs + real code
                    
                    3. **Synthesize Knowledge**
                       - Connect theory (Learn) with practice (GitHub)
                       - Explain WHY solutions work, not just HOW
                       - Highlight best practices from official docs
                       - Show real-world implementations from GitHub
                    
                    4. **Progressive Disclosure**
                       - Start with a clear, concise answer
                       - Provide details when asked
                       - Offer to dive deeper or show more examples
                    
                    RESPONSE STYLE:
                    
                    - **Clear Structure**: Use headings and bullet points
                    - **Code Examples**: Format code properly with markdown
                    - **Context**: Explain what you're searching for and why
                    - **Links**: Mention repository names or doc sources
                    - **Proactive**: Suggest related topics or next steps
                    - **Honest**: If you can't find something, say so and suggest alternatives
                    
                    PERSONALITY:
                    
                    - Be enthusiastic but not overwhelming
                    - Use emojis sparingly for emphasis
                    - Be encouraging and supportive
                    - Celebrate good questions
                    - Make learning enjoyable
                    
                    EXAMPLES OF GREAT RESPONSES:
                    
                    "Great question! Let me check the official Microsoft Learn docs for 
                    the recommended approach to [topic]... 
                    
                    [After search] According to Microsoft's documentation, the best practice is...
                    
                    Want to see how it's implemented in a real project? I can check some 
                    popular repositories that use this pattern!"
                    
                    Remember: You're not just retrieving information - you're being a mentor,
                    connecting dots between official guidance and real-world practice!
                    """,
                ChatOptions = new ChatOptions
                {
                    Tools = allTools
                }
            });

        // ====================================
        // Step 6: Create a new thread
        // ====================================
        AgentThread thread = devMaster.GetNewThread();

        // ====================================
        // Welcome message
        // ====================================
        Console.WriteLine(new string('?', 80));
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("???? THE DEV MASTER IS READY TO ASSIST!");
        Console.ResetColor();
        Console.WriteLine(new string('?', 80));
        Console.WriteLine();
        
        Console.WriteLine("?? I have access to:");
        Console.WriteLine("   ?? GitHub - Real-world code examples and patterns");
        Console.WriteLine("   ?? Microsoft Learn - Official documentation and best practices");
        Console.WriteLine();
        
        Console.WriteLine("?? What I can help you with:");
        Console.WriteLine();
        Console.WriteLine("   ?? Learning Topics:");
        Console.WriteLine("      • 'How do I implement dependency injection in .NET?'");
        Console.WriteLine("      • 'What's the best way to handle async/await in C#?'");
        Console.WriteLine("      • 'Explain Azure Functions triggers and bindings'");
        Console.WriteLine();
        Console.WriteLine("   ?? Code Examples:");
        Console.WriteLine("      • 'Show me how Microsoft implements authentication in their repos'");
        Console.WriteLine("      • 'Find examples of using Semantic Kernel'");
        Console.WriteLine("      • 'How does the Agent Framework handle workflows?'");
        Console.WriteLine();
        Console.WriteLine("   ?? Combined Queries:");
        Console.WriteLine("      • 'Teach me about MCP and show real implementations'");
        Console.WriteLine("      • 'What's the official guidance on OpenTelemetry + practical examples?'");
        Console.WriteLine("      • 'Best practices for AI agents + code from microsoft/agent-framework'");
        Console.WriteLine();
        Console.WriteLine("   ?? Research & Discovery:");
        Console.WriteLine("      • 'What's new in .NET 9 according to Learn?'");
        Console.WriteLine("      • 'Find popular Azure AI repositories'");
        Console.WriteLine("      • 'Compare official guidance with community implementations'");
        Console.WriteLine();
        
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("?? Tip: I work best when you're specific! Tell me if you want:");
        Console.WriteLine("   • Official documentation and best practices");
        Console.WriteLine("   • Real code examples from GitHub");
        Console.WriteLine("   • Both! (recommended for learning)");
        Console.ResetColor();
        Console.WriteLine();
        
        Console.WriteLine("Type 'q' or 'quit' to exit.\n");
        Console.WriteLine(new string('?', 80));
        Console.WriteLine();

        // ====================================
        // Step 7: Interactive conversation loop
        // ====================================
        while (true)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("You: ");
            Console.ResetColor();

            string? userInput = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(userInput))
            {
                continue;
            }

            // Check for quit command
            if (userInput.Equals("q", StringComparison.OrdinalIgnoreCase) ||
                userInput.Equals("quit", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("?? Happy coding! The Dev Master is always here when you need guidance.");
                Console.WriteLine("   Remember: The best developers never stop learning! ??");
                Console.ResetColor();
                break;
            }

            // Get response from the Dev Master
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.Write("Dev Master ??: ");
            Console.ResetColor();

            try
            {
                var response = await devMaster.RunAsync(userInput, thread);
                Console.WriteLine(response.Text);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"? Encountered an issue: {ex.Message}");
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("?? Troubleshooting tips:");
                Console.WriteLine("   • Try rephrasing your question");
                Console.WriteLine("   • Be more specific about what you're looking for");
                Console.WriteLine("   • Check if the repository name or topic is correct");
                Console.ResetColor();
            }

            Console.WriteLine();
        }

        Console.WriteLine();
        Console.WriteLine(new string('?', 80));
        Console.WriteLine("? Demo Complete: The Dev Master session has ended.");
        Console.WriteLine(new string('?', 80));
        Console.WriteLine();
        
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("?? Key Takeaways:");
        Console.ResetColor();
        Console.WriteLine();
        Console.WriteLine("   1??  Multi-MCP Integration:");
        Console.WriteLine("      Multiple MCP servers can work together seamlessly");
        Console.WriteLine();
        Console.WriteLine("   2??  Complementary Knowledge:");
        Console.WriteLine("      GitHub (practical examples) + Learn (official docs) = Comprehensive learning");
        Console.WriteLine();
        Console.WriteLine("   3??  Dynamic Tool Discovery:");
        Console.WriteLine($"      {allTools.Count} tools discovered and used without hardcoding");
        Console.WriteLine();
        Console.WriteLine("   4??  HTTP MCP Support:");
        Console.WriteLine("      Microsoft Learn MCP uses HTTP transport (not stdio like GitHub)");
        Console.WriteLine();
        Console.WriteLine("   5??  Agent Orchestration:");
        Console.WriteLine("      The agent intelligently chooses which tools to use based on context");
        Console.WriteLine();
        
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("?? This pattern enables building powerful AI assistants that combine");
        Console.WriteLine("   multiple knowledge sources for superior user experiences!");
        Console.ResetColor();
        Console.WriteLine();
    }
}
