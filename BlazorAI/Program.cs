using Anthropic;
using Azure.AI.OpenAI;
using BlazorAI;
using BlazorAI.Components;
using BlazorAI.Data;
using BlazorAI.Services;
using BlazorAI.Services.Chatbots;
using BlazorAI.Services.RAG;
using ChatbotSimple.Services;
using CommunityToolkit.VectorData.InMemory;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using OpenAI.Embeddings;
using System.ClientModel;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseSqlite("Data Source=mydb.db"));


builder.Services.AddScoped<IPersonService, PersonService>();

builder.Services.AddKeyedScoped<IChatbot, RealChatbot>("chat");
builder.Services.AddKeyedScoped<IChatbot, ChatbotRAG>("chat-rag");

builder.Services.AddSingleton<DocumentsFromMemoryService>();
builder.Services.AddSingleton<IRAGService, AzureSearchRAGService>();
builder.Services.AddSingleton<InMemoryVectorStore>();

builder.Services.AddTransient<IMarkdownRepository, MarkdownRepository>();

// Register the embedding generator.
builder.Services.AddSingleton<
    IEmbeddingGenerator<string, Embedding<float>>>(sp =>
    {
        var configuration =
            sp.GetRequiredService<IConfiguration>();

        var endpoint = configuration["AzureOpenAI:Endpoint"]
            ?? throw new InvalidOperationException(
                "Azure OpenAI endpoint is missing.");

        var apiKey = configuration["OPENAI_API_KEY"]
            ?? throw new InvalidOperationException(
                "Azure OpenAI API key is missing.");

        var deploymentName =
            configuration["AzureOpenAI:EmbeddingDeployment"]
            ?? throw new InvalidOperationException(
                "Azure OpenAI embedding deployment is missing.");

        var azureClient = new AzureOpenAIClient(
            new Uri(endpoint),
            new ApiKeyCredential(apiKey));

        var embeddingClient =
            azureClient.GetEmbeddingClient(deploymentName);

        return embeddingClient.AsIEmbeddingGenerator();
    });


builder.Services.AddTransient<FakeGetEmailService>();
builder.Services.AddTransient<FakeSendEmailService>();
builder.Services.AddTransient<EvaluateWeatherConditions>();

builder.Services.AddTransient<IndexConfigurationAzureSearchService>();
builder.Services.AddTransient<IVectorStore, VectorStoreAzureSearch>();


builder.Services.AddTransient<IWeatherService, WeatherAPIService>();
builder.Services.AddHttpClient();

builder.Services.AddTransient<IChatClientFactory, ChatClientFactory>();

//builder.Services.AddChatClient(sp =>
//{
//    var configuration = sp.GetRequiredService<IConfiguration>();
//    var provider = "openai";
//    var model = "gpt-5.4-nano";

//    var openAIKey = configuration.GetValue<string>("OpenAIKey");
//    var claudeKey = configuration.GetValue<string>("ClaudeKey");

//    var client = provider switch
//    {
//        "openai" => new OpenAI.Chat.ChatClient(model ?? "gpt-5.4-nano", openAIKey).AsIChatClient(),
//        "claude" => new AnthropicClient()
//        {
//            ApiKey = claudeKey
//        }.AsIChatClient()
//        .AsBuilder()
//        .ConfigureOptions(c => c.ModelId = model ?? "claude-haiku-4-5")
//        .Build(),
//        _ => throw new ArgumentException($"Unknown provider: {provider}")
//    };

//    return client
//            .AsBuilder()
//            .UseFunctionInvocation(null, c =>
//            {
//                c.IncludeDetailedErrors = true;
//            })
//            .Build(sp);

//});

builder.Services.AddTransient<ChatOptions>(sp => new ChatOptions
{
    Tools = [.. Tools.GetTools(sp)],
    Temperature = 0.7f,
    MaxOutputTokens = 2000
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
