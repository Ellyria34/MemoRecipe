using MemoRecipeIA.Application.Interfaces;
using MemoRecipeIA.Application.Pipeline;
using MemoRecipeIA.Infrastructure.OCR;
using MemoRecipeIA.Infrastructure.AI;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddSingleton<IOcrService, TesseractOcrService>();
        services.AddSingleton<IChatCompletionClient, FakeChatCompletionClient>();
        services.AddSingleton<IRecipeAiService, RecipeAiService>();
        services.AddSingleton<IRecipePipeline, RecipePipeline>();
    })
    .Build();

host.Run();



