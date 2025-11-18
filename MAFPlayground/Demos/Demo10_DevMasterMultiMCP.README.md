# Demo 10: The Dev Master - Multi-MCP Learning Assistant

## Overview

This demo showcases the power of combining **multiple Model Context Protocol (MCP) servers** to create a comprehensive AI-powered development and learning assistant. By integrating both GitHub and Microsoft Learn MCP servers, the agent can provide both practical code examples and official documentation in a single, coherent response.

## Architecture

```
???????????????????????????????????????????????????????????????
?                      Dev Master Agent                         ?
?                   ???? Your AI Mentor                         ?
???????????????????????????????????????????????????????????????
              ?                               ?
              ?                               ?
    ????????????????????          ????????????????????????
    ?  GitHub MCP      ?          ?  Microsoft Learn MCP ?
    ?  (stdio)         ?          ?  (HTTP)              ?
    ?                  ?          ?                      ?
    ?  Tools:          ?          ?  Tools:              ?
    ?  • Repos         ?          ?  • Search docs       ?
    ?  • Commits       ?          ?  • Fetch articles    ?
    ?  • Issues        ?          ?  • Best practices    ?
    ?  • PRs           ?          ?  • Tutorials         ?
    ????????????????????          ????????????????????????
```

## What Makes This Special?

### 1. **Multi-MCP Integration**
Unlike Demo08 (GitHub only) or single-source agents, Demo10 combines:
- **GitHub MCP** - Real-world code examples, patterns, and implementations
- **Microsoft Learn MCP** - Official documentation, best practices, and guidelines

### 2. **Different Transport Types**
Demonstrates using both MCP transport mechanisms:
- **stdio transport** - For GitHub MCP (local process via npx)
- **HTTP transport** - For Microsoft Learn MCP (remote REST API)

### 3. **Intelligent Tool Selection**
The agent intelligently decides which tools to use based on context:
- Learning questions ? Microsoft Learn
- Code examples ? GitHub
- Comprehensive answers ? Both!

## How It Works

### Step 1: GitHub MCP Server Connection (stdio)
```csharp
await using var githubMcpClient = await McpClientFactory.CreateAsync(new StdioClientTransport(new()
{
    Name = "GitHubMCPServer",
    Command = "npx",
    Arguments = ["-y", "--verbose", "@modelcontextprotocol/server-github"],
}));

var githubTools = await githubMcpClient.ListToolsAsync();
```

### Step 2: Microsoft Learn MCP Server Connection (HTTP)
```csharp
await using var learnMcpClient = await McpClientFactory.CreateAsync(new HttpClientTransport(new()
{
    Name = "MicrosoftLearnMCPServer",
    Endpoint = new Uri("https://learn.microsoft.com/api/mcp")
}));

var learnTools = await learnMcpClient.ListToolsAsync();
```

### Step 3: Combine All Tools
```csharp
var allTools = new List<AITool>();
allTools.AddRange(githubTools.Cast<AITool>());
allTools.AddRange(learnTools.Cast<AITool>());

// Agent now has access to ALL tools from both MCPs!
```

## Use Cases

### ?? Learning & Documentation
**Query:** "How do I implement dependency injection in .NET?"

**Agent's Approach:**
1. Search Microsoft Learn for official DI documentation
2. Explain concepts from official sources
3. Optionally show GitHub examples from Microsoft repos

### ?? Code Examples
**Query:** "Show me how Microsoft implements authentication"

**Agent's Approach:**
1. Search GitHub repos (microsoft/aspnetcore, microsoft/identity)
2. Find relevant code examples
3. Explain patterns found in real implementations

### ?? Combined Learning
**Query:** "Teach me about Azure Functions with real examples"

**Agent's Approach:**
1. **Learn MCP**: Get official Azure Functions documentation
2. **GitHub MCP**: Find microsoft/azure-functions examples
3. **Synthesize**: Connect theory with practice

### ?? Research & Discovery
**Query:** "What are the best practices for AI agents according to Microsoft?"

**Agent's Approach:**
1. Search Microsoft Learn for AI agent guidance
2. Search GitHub for microsoft/semantic-kernel or microsoft/agent-framework
3. Compare official guidance with implementations

## Running the Demo

### Prerequisites

1. **Node.js and npx** (for GitHub MCP)
   ```bash
   node --version  # Should be v18+
   ```

2. **Azure OpenAI Configuration**
   - Set `AZURE_OPENAI_ENDPOINT` environment variable
   - Configure `AIConfig` with your deployment

3. **Internet Connection**
   - GitHub MCP: Downloads via npx
   - Microsoft Learn MCP: Remote HTTP endpoint

### Execution

1. **Uncomment in Program.cs:**
   ```csharp
   await Demo10_DevMasterMultiMCP.Execute();
   ```

2. **Run:**
   ```bash
   dotnet run
   ```

3. **Interact:**
   - Ask questions mixing theory and practice
   - Request code examples
   - Explore documentation
   - Type `q` or `quit` to exit

## Example Interactions

### Example 1: Learning Topic
```
You: How do I use async/await in C#?

Dev Master ??: 
Great question! Let me search Microsoft Learn for the official guidance...

[Searches Microsoft Learn]

According to Microsoft's documentation, async/await is the recommended 
pattern for asynchronous programming in C#. Here are the key points:

1. async modifier: Marks a method as asynchronous
2. await operator: Suspends execution until the awaited task completes
3. Task return type: Required for async methods

Want to see real-world examples from Microsoft's repositories?
```

### Example 2: Code Examples
```
You: Show me examples of using Semantic Kernel

Dev Master ??:
I'll search GitHub for Semantic Kernel examples from Microsoft's repos...

[Searches GitHub: microsoft/semantic-kernel]

Found several great examples! Here are implementations from the official 
Semantic Kernel repository:

1. Basic Kernel Setup (samples/00-basic)
2. Plugin Integration (samples/plugins)
3. Memory & RAG patterns (samples/memory)

Would you like me to dive deeper into any of these patterns?
```

### Example 3: Combined Query
```
You: Explain Model Context Protocol and show me how it's implemented

Dev Master ??:
Perfect! Let me combine official docs with real implementations...

[Searches Microsoft Learn + GitHub]

**Official Documentation** (from Microsoft Learn):
MCP (Model Context Protocol) is a standardized protocol for connecting
AI models with external tools and data sources...

**Real Implementation** (from GitHub):
Here's how the microsoft/mcp-dotnet library implements it:
[Shows code examples from the repo]

This demonstrates the connection between theory and practice!
```

## Key Features

### ? Intelligent Tool Selection
The agent's instructions guide it to:
- Use Learn MCP for "how to" questions
- Use GitHub MCP for "show me" requests
- Combine both for comprehensive answers

### ?? Dynamic Tool Discovery
Both MCP servers are queried at startup:
```
Found 8 GitHub tools
Found 2 Microsoft Learn tools
Total tools available: 10
```

### ?? Multiple Transports
- **stdio**: Local process (GitHub via npx)
- **HTTP**: Remote API (Microsoft Learn)

### ?? Context-Aware Responses
The agent maintains conversation context and can:
- Reference previous answers
- Build upon earlier examples
- Suggest related topics

## Technical Details

### GitHub MCP Tools
- `github_search_repositories`
- `github_get_file_contents`
- `github_list_commits`
- `github_get_issue`
- `github_list_issues`
- `github_create_issue`
- `github_search_code`
- ... and more

### Microsoft Learn MCP Tools
- `microsoft_docs_search` - Search Microsoft Learn documentation
- `microsoft_docs_fetch` - Fetch specific doc articles

### Agent Personality
The Dev Master agent is designed to be:
- **Educational** - Explains concepts clearly
- **Practical** - Shows real-world examples
- **Proactive** - Suggests next steps
- **Honest** - Admits limitations
- **Encouraging** - Celebrates learning

## Comparison with Other Demos

| Feature | Demo08 (GitHub) | Demo09 (Neo4j) | Demo10 (Multi-MCP) |
|---------|-----------------|----------------|-------------------|
| MCP Servers | 1 | 1 | 2 |
| Transport Types | stdio | stdio | stdio + HTTP |
| Knowledge Source | Code repos | Graph DB | Docs + Code |
| Use Case | Code exploration | Investigation | Learning |
| Complexity | Medium | Medium | High |
| Value | Practical | Specialized | Comprehensive |

## Benefits

1. **Comprehensive Learning** - Theory + Practice in one place
2. **Authoritative Sources** - Official docs + Real implementations
3. **Time Saving** - No need to search separately
4. **Context Synthesis** - Agent connects the dots
5. **Up-to-Date** - Both sources regularly updated

## Limitations

1. **Network Required** - Both MCPs need internet access
2. **Rate Limits** - GitHub API and Learn API may have limits
3. **Response Time** - Multiple tool calls can be slower
4. **Token Usage** - More tools = more tokens

## Future Enhancements

Potential additions:
- **Stack Overflow MCP** - Community solutions
- **NuGet MCP** - Package information
- **Azure MCP** - Cloud service docs
- **Caching** - Store frequent queries
- **Preferences** - User can choose favorite sources

## Best Practices

### Effective Queries

**? Good:**
- "How do I implement X according to Microsoft, and show me examples"
- "What are Microsoft's best practices for Y with real code"
- "Teach me Z using official docs and practical examples"

**? Less Effective:**
- "Tell me about programming" (too vague)
- "Show me everything about X" (too broad)
- Single-word queries (not enough context)

### Making the Most of Multi-MCP

1. **Be Specific**: Mention if you want docs, code, or both
2. **Build Context**: Ask follow-up questions
3. **Explore Connections**: Ask agent to relate concepts
4. **Request Comparisons**: "Official way vs common practice"

## Troubleshooting

### GitHub MCP Not Connecting
- Check Node.js installation (`node --version`)
- Ensure npx is available
- Check internet connection
- Look for GitHub API rate limits

### Microsoft Learn MCP Not Connecting
- Verify internet connection
- Check if https://learn.microsoft.com is accessible
- Endpoint: `https://learn.microsoft.com/api/mcp`

### Agent Not Using Both Sources
- Be explicit: "Check both docs and code"
- Rephrase question to indicate need for both
- Agent may choose most relevant source first

## Learn More

- [MCP Specification](https://modelcontextprotocol.io/)
- [Microsoft Learn MCP](https://learn.microsoft.com/en-us/training/support/mcp)
- [GitHub MCP Server](https://github.com/modelcontextprotocol/servers)
- [Microsoft Agent Framework](https://github.com/microsoft/agent-framework)

## Conclusion

Demo10 demonstrates the true power of the Model Context Protocol - the ability to seamlessly integrate multiple knowledge sources into a single, intelligent agent. By combining GitHub's practical code examples with Microsoft Learn's official documentation, developers get a comprehensive learning experience that bridges theory and practice.

This pattern can be extended to virtually any combination of MCP servers, enabling agents that can access and synthesize information from diverse sources to provide truly intelligent assistance.

Happy Learning! ??????
