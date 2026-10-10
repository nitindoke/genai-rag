using Anthropic;
using BlazorAI.Utilities;
using Microsoft.Extensions.AI;
using Azure.AI.OpenAI;
using Azure;

namespace BlazorAI.Services.Chatbots
{
    public class ChatClientFactory(IConfiguration configuration, IServiceProvider sp) : IChatClientFactory
    {
        public IChatClient Create(string model)
        {
            var openAIKey = configuration.GetValue<string>("OpenAIKey");
            var claudeKey = configuration.GetValue<string>("ClaudeKey");

            var provider = AIModels.GetProvider(model);

            var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? throw new InvalidOperationException("Missing Azure OpenAI API key.");

            // ------------------------------------------------------------
            // Azure OpenAI configuration
            // ------------------------------------------------------------

            var azureOpenAIEndpoint = configuration["AzureOpenAI:Endpoint"];
            var chatDeployment = configuration["AzureOpenAI:ChatDeployment"];
            var embeddingDeployment = configuration["AzureOpenAI:EmbeddingDeployment"];

            // ------------------------------------------------------------
            // Azure OpenAI client
            // ------------------------------------------------------------

            //var azureOpenAIClient = new AzureOpenAIClient(
            //    new Uri(azureOpenAIEndpoint),
            //    credential);

            var azureOpenAIClient = new AzureOpenAIClient(
                new Uri(azureOpenAIEndpoint ?? throw new InvalidOperationException("Missing Azure OpenAI endpoint.")),
                new AzureKeyCredential(apiKey));

            // ------------------------------------------------------------
            // Chat model
            // ------------------------------------------------------------

            var chatClient = azureOpenAIClient
                            .GetChatClient(chatDeployment)
                            .AsIChatClient();



            //return chatClient;

            //var client = provider switch
            //{
            //    "openai" => new OpenAI.Chat.ChatClient(model ?? "gpt-5.4-nano", openAIKey).AsIChatClient(),
            //    "claude" => new AnthropicClient()
            //    {
            //        ApiKey = claudeKey
            //    }.AsIChatClient()
            //    .AsBuilder()
            //    .ConfigureOptions(c => c.ModelId = model ?? "claude-haiku-4-5")
            //    .Build(),
            //    _ => throw new ArgumentException($"Unknown provider: {provider}")
            //};

            return chatClient
                    .AsBuilder()
                    .UseFunctionInvocation(null, c =>
                    {
                        c.IncludeDetailedErrors = true;
                    })
                    .Build(sp);
        }
    }
}
