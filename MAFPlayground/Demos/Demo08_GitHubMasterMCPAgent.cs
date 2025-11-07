// SPDX-License-Identifier: LicenseRef-MAFPlayground-NPU-1.0-CH
// Copyright (c) 2025 Jose Luis Latorre

using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;

namespace MAFPlayground.Demos;

/// <summary>
/// Demo 08: GitHub Master - MCP Powered Agent
/// 
/// Demonstrates the power of Model Context Protocol (MCP) by connecting to a GitHub MCP server.
/// This agent can explore repositories, check commits, analyze issues, and help you navigate
/// the vast world of GitHub with style!
/// 
/// The agent uses MCP to dynamically access GitHub tools without hardcoding them.
/// 
/// Type 'q' or 'quit' to exit the conversation.
/// </summary>
internal static class Demo08_GitHubMasterMCPAgent
{
    public static async Task Execute()
    {
        // Set console encoding to UTF-8 to support emojis and special characters
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        Console.WriteLine("\n=== DEMO 08: GITHUB MASTER - MCP POWERED AGENT ===\n");

        // Step 1: Create an MCP client connected to the GitHub server
        Console.WriteLine("🔌 Connecting to GitHub MCP Server...");
        await using var mcpClient = await McpClientFactory.CreateAsync(new StdioClientTransport(new()
        {
            Name = "GitHubMCPServer",
            Command = "npx",
            Arguments = ["-y", "--verbose", "@modelcontextprotocol/server-github"],
        }));

        Console.WriteLine("✅ Connected to GitHub MCP Server!\n");

        // Step 2: Retrieve the list of tools available from the MCP server
        Console.WriteLine("🔍 Discovering available GitHub tools...");
        var mcpTools = await mcpClient.ListToolsAsync().ConfigureAwait(false);

        Console.WriteLine($"✅ Found {mcpTools.Count} GitHub tools!\n");

        // Step 3: Create the Azure OpenAI chat client using the config
        var azureClient = new AzureOpenAIClient(AIConfig.Endpoint, AIConfig.KeyCredential);
        var chatClient = azureClient
            .GetChatClient(AIConfig.ModelDeployment)
            .AsIChatClient();

        // Step 4: Create the agent with the MCP tools
        AIAgent gitHubMaster = new ChatClientAgent(
            chatClient,
            new ChatClientAgentOptions
            {
                Name = "GitHubMaster",
                Instructions = """
                    You are The GitHub Master 🎩 - a witty, knowledgeable, and slightly eccentric 
                    GitHub expert who speaks with flair and enthusiasm!
                    
                    You can:
                    - Explore GitHub repositories with surgical precision
                    - Analyze commits like reading a thrilling novel
                    - Navigate issues and pull requests with ease
                    - Provide insights on repository activity and contributors
                    - Search for repositories and discover hidden gems
                    
                    Personality traits:
                    - Be proactive! If the user asks about a repo, dig deeper and offer related insights
                    - Use emojis to add character (but don't overdo it - you're cool, not a teenager)
                    - Make technical information accessible and engaging
                    - When exploring commits or issues, provide context and highlight what's interesting
                    - If something fails, try alternative approaches - you're a problem solver!
                    
                    Response style:
                    - Start responses with a friendly greeting or acknowledgment
                    - Structure information clearly (use markdown when helpful)
                    - End with a proactive suggestion or question to keep the conversation going
                    - Remember the conversation context and build upon previous exchanges
                    
                    Keep it fun, informative, and helpful. You're not just retrieving data - 
                    you're telling stories about code and collaboration!
                    """,
                ChatOptions = new ChatOptions
                {
                    // Cast MCP tools to AITool and add them to the agent
                    Tools = [.. mcpTools.Cast<AITool>()]
                }
            });

        // Step 5: Create a new thread for conversation
        AgentThread thread = gitHubMaster.GetNewThread();

        // Welcome message
        Console.WriteLine("🎩 The GitHub Master is here to serve!\n");
        Console.WriteLine("💡 I can help you with:");
        Console.WriteLine("   • Exploring repositories and their history");
        Console.WriteLine("   • Analyzing recent commits and changes");
        Console.WriteLine("   • Investigating issues and pull requests");
        Console.WriteLine("   • Discovering repository statistics and contributors");
        Console.WriteLine("   • Searching for interesting projects");
        Console.WriteLine();
        Console.WriteLine("📝 Examples:");
        Console.WriteLine("   • 'Summarize the last 5 commits to microsoft/semantic-kernel'");
        Console.WriteLine("   • 'What are the open issues in microsoft/agent-framework?'");
        Console.WriteLine("   • 'Show me the contributors to dotnet/runtime'");
        Console.WriteLine("   • 'Find popular machine learning repositories'");
        Console.WriteLine();
        Console.WriteLine("Type 'q' or 'quit' to exit the conversation.\n");
        Console.WriteLine(new string('=', 80));
        Console.WriteLine();

        // Step 6: Interactive conversation loop
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
                Console.WriteLine("👋 The GitHub Master bids you farewell! May your commits be meaningful and your PRs swift! ✨");
                Console.ResetColor();
                break;
            }

            // Get response from the GitHub Master
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.Write("GitHub Master 🎩: ");
            Console.ResetColor();

            try
            {
                var response = await gitHubMaster.RunAsync(userInput, thread);
                Console.WriteLine(response.Text);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Oops! Even the GitHub Master encounters mysteries: {ex.Message}");
                Console.WriteLine($"💡 Try rephrasing your question or being more specific about the repository.");
                Console.ResetColor();
            }

            Console.WriteLine();
        }

        Console.WriteLine();
        Console.WriteLine("✅ Demo Complete: The GitHub Master has completed this session.");
        Console.WriteLine("💡 Key Takeaway: MCP allows agents to discover and use external tools dynamically,");
        Console.WriteLine("   without hardcoding every possible function. The agent automatically learned");
        Console.WriteLine("   what GitHub operations are available and how to use them!");
    }
}