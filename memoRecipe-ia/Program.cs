using MemoRecipeIA.Application.Interfaces;
using MemoRecipeIA.Application.Pipeline;
using MemoRecipeIA.Infrastructure.OCR;
using MemoRecipeIA.Infrastructure.AI;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

// var host = new HostBuilder()
//     .ConfigureFunctionsWorkerDefaults()
//     .ConfigureServices(services =>
//     {
//         services.AddSingleton<IOcrService, TesseractOcrService>();
//         services.AddSingleton<IChatCompletionClient, FakeChatCompletionClient>();
//         services.AddSingleton<IRecipeAiService, RecipeAiService>();
//         services.AddSingleton<IRecipePipeline, RecipePipeline>();
//     })
//     .Build();

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        // Http client
        services.AddHttpClient();

        // OCR
        services.AddSingleton<IOcrService, TesseractOcrService>();

        // LLM réel (Mistral)
        services.AddSingleton<IChatCompletionClient>(sp =>
        {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = factory.CreateClient();

            var apiKey = Environment.GetEnvironmentVariable("MISTRAL_API_KEY")
                        ?? throw new InvalidOperationException("Missing MISTRAL_API_KEY");

            return new MistralChatCompletionClient(httpClient, apiKey);
        });

        // Parsing IA
        services.AddSingleton<IRecipeAiService, RecipeAiService>();

        // Pipeline
        services.AddSingleton<IRecipePipeline, RecipePipeline>();
    })
    .Build();

host.Run();




