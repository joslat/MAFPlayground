using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using OpenAI;

namespace MAFPlayground.Samples;

internal static class Sample01_BasicAgent
{
    public static async Task Execute()
    {
        // Create the AzureOpenAIClient using the lazy config from AIConfig
        var azureClient = new AzureOpenAIClient(AIConfig.Endpoint, AIConfig.KeyCredential);

        // Create the agent from the chat client
        AIAgent agent = azureClient
            .GetChatClient(AIConfig.ModelDeployment)
            .CreateAIAgent(instructions: "You are good at telling jokes.");

        // (optional) demonstrate another agent using CLI credentials
        AIAgent agent2 = new AzureOpenAIClient(
          AIConfig.Endpoint,
          new AzureCliCredential())
            .GetChatClient(AIConfig.ModelDeployment)
            .CreateAIAgent(instructions: "You are good at telling jokes.");

        Console.WriteLine(await agent.RunAsync("Tell me a joke about a pirate."));
        Console.WriteLine(await agent2.RunAsync("Tell me a joke about a pirate but tell it like a pirate parrot."));
    }

}
