using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;

namespace MAFPlayground.Samples;

internal static class Sample02_ImageAgent
{
    public static async Task Execute()
    {
        // Create the AzureOpenAIClient using the lazy config from AIConfig
        var azureClient = new AzureOpenAIClient(AIConfig.Endpoint, AIConfig.KeyCredential);

        // Create the agent from the chat client
        AIAgent agent = azureClient
            .GetChatClient(AIConfig.ModelDeployment)
            .CreateAIAgent(
                instructions: "You are a helpful agent that can analyze images.", 
                name: "ImageanAlyzer");

        ChatMessage message = new(ChatRole.User, [
            new TextContent("What do you see in this image?"),
            new UriContent("https://upload.wikimedia.org/wikipedia/commons/thumb/d/dd/Gfp-wisconsin-madison-the-nature-boardwalk.jpg/2560px-Gfp-wisconsin-madison-the-nature-boardwalk.jpg", "image/jpeg")
        ]);


        Console.WriteLine(await agent.RunAsync(message));
    }

}
