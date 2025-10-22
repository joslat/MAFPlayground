using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using System;
using System.Threading.Tasks;

namespace MAFPlayground.Samples;

/// <summary>
/// This sample demonstrates creating a simple AI agent for creative writing.
/// 
/// Key concepts:
/// 1. Creating a ChatClientAgent with specific instructions
/// 2. Running a simple agent request with RunAsync
/// 3. Getting a single response without streaming
/// 
/// This is useful for:
/// - Quick prototyping with AI agents
/// - Simple single-turn agent interactions
/// - Testing agent instructions and behavior
/// </summary>
/// <remarks>
/// Pre-requisites:
/// - An Azure OpenAI chat completion deployment must be configured.
/// </remarks>
internal static class Sample12A_WriterChatAgent
{
    public static async Task Execute()
    {
        Console.WriteLine("=== Sample 12A: Writer Chat Agent ===");
        Console.WriteLine("Demonstrates simple agent interaction for creative writing.\n");

        // Set up the Azure OpenAI client using AIConfig
        var azureClient = new AzureOpenAIClient(AIConfig.Endpoint, AIConfig.KeyCredential);
        var deploymentName = AIConfig.ModelDeployment;
        IChatClient chatClient = azureClient.GetChatClient(deploymentName).AsIChatClient();

        // ====================================
        // Step 1: Create Writer agent
        // ====================================
        Console.WriteLine("Creating Writer agent...\n");

        ChatClientAgent writer = new(
            chatClient,
            name: "Writer",
            instructions: @"You are a creative writer who crafts engaging stories.
Focus on creating vivid descriptions, interesting characters, and compelling narratives.
When you receive feedback from the editor, carefully incorporate their suggestions to improve your work.
Build upon the previous version of the story while addressing the editor's critiques."
        );

        Console.WriteLine("✅ Writer agent created.\n");

        // ====================================
        // Step 2: Run agent with a prompt
        // ====================================
        Console.WriteLine("--- Sending Prompt to Writer Agent ---\n");
        Console.WriteLine("Prompt: Write a short story about a haunted house.\n");
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine();

        AgentRunResponse response = await writer.RunAsync("Write a short story about a haunted house.");

        Console.WriteLine(response.Text);

        Console.WriteLine();
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine();

        // ====================================
        // Step 3: Additional example
        // ====================================
        Console.WriteLine("--- Additional Example ---\n");
        Console.WriteLine("Prompt: Write a twist ending for the haunted house story.\n");
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine();

        AgentRunResponse twistResponse = await writer.RunAsync(
            "Write a twist ending for the haunted house story. Keep it under 100 words.");

        Console.WriteLine(twistResponse.Text);

        Console.WriteLine();
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine();

        Console.WriteLine("✅ Sample 12A Complete!");
        Console.WriteLine();
        Console.WriteLine("Key Takeaways:");
        Console.WriteLine("- ChatClientAgent works with Azure OpenAI");
        Console.WriteLine("- RunAsync provides simple single-turn interactions");
        Console.WriteLine("- Agent instructions guide the behavior and style");
        Console.WriteLine("- Same agent can be reused for multiple requests");
    }
}