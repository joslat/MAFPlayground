// SPDX-License-Identifier: LicenseRef-MAFPlayground-NPU-1.0-CH
// Copyright (c) 2025 Jose Luis Latorre & Zaid Zaim
//
// ✨ SPECIAL THANKS ✨
// This demo was created in collaboration with Zaid Zaim (@zaidzaim)
// We have done this together as part of a series of discussions we
// have had on MCP integration in regards to graph databases with Neo4j.
// Zaid provided invaluable support with the Neo4j MCP integration,
// environment configuration, and countless ideas.
// His expertise in graph databases and MCP helped bring this detective to life! 🕵️
//
// Collaboration highlights:
// - Neo4j crime database setup and configuration
// - MCP server identification and providing the right resources.
// - Brainstorming together on ideas and motivation for the demo and future
//   talks based on this demo
//
// Thanks Zaid! 🙌

using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using ModelContextProtocol;
using ModelContextProtocol.Client;

namespace MAFPlayground.Demos;

/// <summary>
/// Demo 09: The Graph Database Detective
/// 
/// Demonstrates MCP integration with a Neo4j crime database using a Golden Age detective
/// inspired by the great investigators of classic mystery fiction. This agent can query 
/// the graph database to investigate crimes, find connections between suspects, analyze 
/// patterns, and solve mysteries using the power of graph relationships!
/// 
/// The agent blends traits from legendary detectives:
/// - Methodical reasoning and attention to detail (Poirot-style)
/// - Deductive logic and pattern recognition (Holmes-style)
/// - Understanding of human nature and social connections (Marple-style)
/// 
/// The agent uses MCP to access Neo4j Cypher query capabilities dynamically.
/// 
/// Environment Variables Required:
/// - NEO4J_URI: The Neo4j database connection URI (e.g., neo4j+s://xxxxx.databases.neo4j.io)
/// - NEO4J_USERNAME: Database username (typically "neo4j")
/// - NEO4J_PASSWORD: Database password
/// - NEO4J_DATABASE: Database name (typically "neo4j")
/// 
/// Type 'q' or 'quit' to exit the conversation.
/// </summary>
internal static class Demo09_GraphDatabaseCrimeAgent
{
    public static async Task Execute()
    {
        // Set console encoding to UTF-8 to support emojis and special characters
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        
        Console.WriteLine("\n=== DEMO 09: THE GRAPH DATABASE DETECTIVE ===\n");

        // Step 1: Retrieve Neo4j configuration from environment variables
        Console.WriteLine("🔐 Retrieving Neo4j database credentials...");
        var neo4jUri = Environment.GetEnvironmentVariable("NEO4J_URI");
        var neo4jUsername = Environment.GetEnvironmentVariable("NEO4J_USERNAME");
        var neo4jPassword = Environment.GetEnvironmentVariable("NEO4J_PASSWORD");
        var neo4jDatabase = Environment.GetEnvironmentVariable("NEO4J_DATABASE") ?? "neo4j";

        if (string.IsNullOrWhiteSpace(neo4jUri) || 
            string.IsNullOrWhiteSpace(neo4jUsername) || 
            string.IsNullOrWhiteSpace(neo4jPassword))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("❌ ERROR: Neo4j environment variables are not properly configured!");
            Console.WriteLine();
            Console.WriteLine("Please set the following environment variables:");
            Console.WriteLine("  • NEO4J_URI - Database connection URI");
            Console.WriteLine("  • NEO4J_USERNAME - Database username");
            Console.WriteLine("  • NEO4J_PASSWORD - Database password");
            Console.WriteLine("  • NEO4J_DATABASE - Database name (optional, defaults to 'neo4j')");
            Console.ResetColor();
            return;
        }

        Console.WriteLine($"✅ Connected to: {neo4jUri}");
        Console.WriteLine($"✅ Database: {neo4jDatabase}\n");

        // Step 2: Create an MCP client connected to the Neo4j MCP server
        Console.WriteLine("🔌 Initializing connection to Neo4j MCP Server...");
        await using var mcpClient = await McpClientFactory.CreateAsync(new StdioClientTransport(new()
        {
            Name = "Neo4jCrimeDatabaseMCP",
            Command = "uvx",
            Arguments = ["mcp-neo4j-cypher@0.5.0", "--transport", "stdio"],
            EnvironmentVariables = new Dictionary<string, string?>
            {
                ["NEO4J_URI"] = neo4jUri,
                ["NEO4J_USERNAME"] = neo4jUsername,
                ["NEO4J_PASSWORD"] = neo4jPassword,
                ["NEO4J_DATABASE"] = neo4jDatabase
            }
        }));

        Console.WriteLine("✅ Connected to Neo4j Crime Database via MCP!\n");

        // Step 3: Retrieve the list of tools available from the MCP server
        Console.WriteLine("🔍 Discovering available database investigation tools...");
        var mcpTools = await mcpClient.ListToolsAsync().ConfigureAwait(false);
        
        Console.WriteLine($"✅ Holmsworth Marot has access to {mcpTools.Count} investigation tools!\n");

        // Step 4: Create the Azure OpenAI chat client using the config
        var azureClient = new AzureOpenAIClient(AIConfig.Endpoint, AIConfig.KeyCredential);
        var chatClient = azureClient
            .GetChatClient(AIConfig.ModelDeployment)
            .AsIChatClient();

        // Step 5: Create the agent with a Golden Age detective personality
        AIAgent detective = new ChatClientAgent(
            chatClient,
            new ChatClientAgentOptions
            {
                Name = "HolmsworthMarot",
                Instructions = """
                    You are Holmsworth Marot 🕵️, a distinguished detective in the tradition of the great 
                    Golden Age investigators - methodical, brilliant, and relentlessly logical in pursuit of truth!
                    
                    PERSONALITY & CHARACTER:
                    You embody the finest qualities of legendary detectives, you are a legend within legends:
                    - Methodical and orderly - you appreciate patterns, symmetry, and systematic analysis
                    - Sharp deductive reasoning - you connect disparate facts with logical precision
                    - Keen understanding of human nature - motives, relationships, and psychology
                    - Courteous and professional, yet confident in your investigative abilities
                    - Patient but determined - every detail, no matter how small, may prove significant
                    - You speak with clarity and precision, befitting a master investigator
                    
                    INVESTIGATION APPROACH:
                    - Begin investigations methodically: "Most intriguing. Let us examine the facts..."
                    - Use your Neo4j graph database to uncover relationships and patterns
                    - Apply deductive logic: eliminate the impossible, and what remains must be truth
                    - Look for connections between suspects, victims, locations, and events
                    - Pay attention to temporal patterns (when did crimes occur?)
                    - Analyze social networks and relationships between individuals
                    - Search for common motives: money, revenge, jealousy, passion, or opportunity
                    - Always consider: "Who benefits from this crime?" and "What is the simplest explanation?"
                    
                    DATABASE INVESTIGATION TECHNIQUES:
                    - Query the graph to find direct and indirect relationships
                    - Look for clusters of related crimes or suspects
                    - Trace paths between suspects and victims
                    - Analyze patterns in crime types, locations, or timeframes
                    - Find anomalies or unusual connections that others might miss
                    - Build a complete picture of the criminal network
                    - Cross-reference multiple data points for corroboration
                    
                    COMMUNICATION STYLE:
                    - Structure your findings clearly: Suspects → Evidence → Connections → Conclusion
                    - Use markdown for clarity when presenting complex relationships
                    - Highlight key discoveries with appropriate emphasis
                    - Translate technical database queries into investigative language
                    - Example: "The investigation reveals..." instead of "The query returned..."
                    - Present findings with confidence: "The evidence clearly indicates..."
                    - Use phrases like: "Elementary, yet significant," "Observe the pattern here," 
                      "The connections are most revealing," "Logic dictates that..."
                    
                    PROACTIVE BEHAVIOR:
                    - If asked about a crime, investigate related crimes and patterns
                    - Suggest follow-up questions: "Perhaps we should also examine..."
                    - When you find one suspect, look for accomplices and associates
                    - Cross-reference different aspects: locations, times, relationships
                    - If initial investigation is inconclusive, try different approaches
                    - Consider alternative theories and test them against the evidence
                    
                    Remember: You are not merely querying a database - you are conducting a 
                    masterful investigation using the most modern of tools. Every query is a 
                    question you would ask a witness, every relationship discovered is a clue 
                    that brings you closer to the truth!
                    
                    "The truth is rarely pure and never simple, but it can always be found through 
                    careful observation, deductive reasoning, and systematic investigation."
                    """,
                ChatOptions = new ChatOptions
                {
                    // Cast MCP tools to AITool and add them to the agent
                    Tools = [.. mcpTools.Cast<AITool>()]
                }
            });

        // Step 6: Create a new thread for conversation
        AgentThread thread = detective.GetNewThread();

        // Welcome message with detective flair
        Console.WriteLine("═══════════════════════════════════════════════════════════════════════════════");
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("🎩 Good day! I am Holmsworth Marot, at your service for this investigation.");
        Console.ResetColor();
        Console.WriteLine("═══════════════════════════════════════════════════════════════════════════════");
        Console.WriteLine();
        Console.WriteLine("My deductive faculties and investigative methods are ready to unravel any mystery.");
        Console.WriteLine("With access to the graph database, no criminal connection shall remain hidden.\n");
        Console.WriteLine("💡 I can assist you with:");
        Console.WriteLine("   • 🔍 Investigating specific crimes and their details");
        Console.WriteLine("   • 👥 Analyzing relationships between suspects and victims");
        Console.WriteLine("   • 🗺️  Exploring patterns in crime locations and types");
        Console.WriteLine("   • ⏰ Discovering temporal patterns in criminal activity");
        Console.WriteLine("   • 🕸️  Uncovering hidden connections in criminal networks");
        Console.WriteLine("   • 📊 Finding common motives and patterns across cases");
        Console.WriteLine();
        Console.WriteLine("📝 Examples of what you might ask:");
        Console.WriteLine("   • 'Show me all unsolved murders in the database'");
        Console.WriteLine("   • 'Who are the known associates of suspect John Smith?'");
        Console.WriteLine("   • 'Find all crimes that occurred near the waterfront'");
        Console.WriteLine("   • 'What connections exist between these two victims?'");
        Console.WriteLine("   • 'Show me patterns in theft crimes over the last year'");
        Console.WriteLine("   • 'Who has both a motive and opportunity for this crime?'");
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("Describe the case you wish to investigate, and I shall apply systematic");
        Console.WriteLine("deduction and analysis to uncover the truth!");
        Console.ResetColor();
        Console.WriteLine();
        Console.WriteLine("Type 'q' or 'quit' to conclude our investigation.\n");
        Console.WriteLine(new string('═', 80));
        Console.WriteLine();

        // Step 7: Interactive conversation loop
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
                Console.WriteLine("👋 Farewell! Thank you for this most interesting investigation.");
                Console.WriteLine("   Remember: logic and observation never fail! 🧠✨");
                Console.ResetColor();
                break;
            }

            // Get response from detective
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("Holmsworth Marot 🕵️: ");
            Console.ResetColor();

            try
            {
                var response = await detective.RunAsync(userInput, thread);
                Console.WriteLine(response.Text);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ My apologies! An unexpected obstacle: {ex.Message}");
                Console.WriteLine($"💡 Perhaps we should approach this investigation from a different angle?");
                Console.WriteLine($"   Try being more specific about the crime, suspect, or relationship you seek.");
                Console.ResetColor();
            }

            Console.WriteLine();
        }

        Console.WriteLine();
        Console.WriteLine("✅ Demo Complete: The investigation has concluded.");
        Console.WriteLine();
        Console.WriteLine("💡 Key Takeaways:");
        Console.WriteLine("   • MCP enables dynamic integration with Neo4j graph databases");
        Console.WriteLine("   • Graph databases excel at uncovering complex relationships");
        Console.WriteLine("   • Environment variables keep sensitive credentials secure");
        Console.WriteLine("   • AI agents can be given rich personalities that enhance user experience");
        Console.WriteLine("   • The combination of graph queries and AI reasoning creates powerful investigations!");
    }
}