using MemoRecipeIA.Application.Interfaces;
using MemoRecipeIA.Application.Pipeline;
using MemoRecipeIA.Infrastructure.OCR;
using MemoRecipeIA.Infrastructure.AI;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureLogging((context, logging) =>
    {
        // Sécurité : éviter de logger les URLs HTTP sortantes
        // (elles contiennent les API keys en query string : ?key=...)
        logging.AddFilter("System.Net.Http.HttpClient", LogLevel.Warning);
    })
    .ConfigureServices(services =>
    {
        // Http client
        services.AddHttpClient();

        // OCR
        services.AddSingleton<IOcrService, TesseractOcrService>();

        var aiProvider = Environment.GetEnvironmentVariable("AI_PROVIDER") ?? "Fake";
        var environnement = Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT") ?? "Development";

        if (environnement == "Production" && aiProvider == "Fake")
        {
            throw new InvalidOperationException("AI_PROVIDER cannot be 'Fake' in Production. Set AI_PROVIDER to 'Mistral', 'Groq', 'GeminiVision' or 'MistralVision'.");
        }

        switch (aiProvider)
        {
            case "Fake":
                // No IChatCompletionClient needed — FakeRecipePipeline skips OCR + LLM entirely
                // and returns a hardcoded recipe. See FakeRecipePipeline registration below.
                break;
            case "Gemini":
                services.AddSingleton<IChatCompletionClient>(sp =>
                {
                    var factory = sp.GetRequiredService<IHttpClientFactory>();
                    var httpClient = factory.CreateClient();

                    var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
                                ?? throw new InvalidOperationException("Missing GEMINI_API_KEY");

                    return new GeminiChatCompletionClient(httpClient, apiKey);
                });
                break;
            case "Mistral":
                services.AddSingleton<IChatCompletionClient>(sp =>
                {
                    var factory = sp.GetRequiredService<IHttpClientFactory>();
                    var httpClient = factory.CreateClient();

                    var apiKey = Environment.GetEnvironmentVariable("MISTRAL_API_KEY")
                                ?? throw new InvalidOperationException("Missing MISTRAL_API_KEY");

                    return new MistralChatCompletionClient(httpClient, apiKey);
                });
                break;
            case "Groq":
                services.AddSingleton<IChatCompletionClient>(sp =>
                {
                    var factory = sp.GetRequiredService<IHttpClientFactory>();
                    var httpClient = factory.CreateClient();

                    var apiKey = Environment.GetEnvironmentVariable("GROQ_API_KEY")
                                ?? throw new InvalidOperationException("Missing GROQ_API_KEY");

                    return new GroqChatCompletionClient(httpClient, apiKey);
                });
                break;
            case "MistralVision":
                services.AddSingleton<IVisionCompletionClient>(sp =>
                {
                    var factory = sp.GetRequiredService<IHttpClientFactory>();
                    var httpClient = factory.CreateClient();

                    var apiKey = Environment.GetEnvironmentVariable("MISTRAL_API_KEY")
                                ?? throw new InvalidOperationException("Missing MISTRAL_API_KEY");

                    return new MistralVisionCompletionClient(httpClient, apiKey);
                });
                break;

            case "GeminiVision":
                services.AddSingleton<IVisionCompletionClient>(sp =>
                {
                    var factory = sp.GetRequiredService<IHttpClientFactory>();
                    var httpClient = factory.CreateClient();

                    var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
                                ?? throw new InvalidOperationException("Missing GEMINI_API_KEY");

                    return new GeminiVisionCompletionClient(httpClient, apiKey);
                });
                break;
            default:
                var validValues = environnement == "Production"
                    ? "'Mistral', 'Gemini', 'Groq', 'GeminiVision', 'MistralVision'"
                    : "'Fake', 'Mistral', 'Gemini', 'Groq', 'GeminiVision', 'MistralVision'";
                throw new InvalidOperationException(
                    $"AI_PROVIDER '{aiProvider}' is unknown. Valid values: {validValues}.");
        }

        // Parsing IA
        services.AddSingleton<IRecipeAiService, RecipeAiService>();

        // Pipeline
        // Fake pipeline skips OCR + LLM entirely (returns a hardcoded RecipeDto) — used for E2E
        // tests to avoid the native libleptonica-1.82.0.so dependency in the container image.
        if (aiProvider == "Fake")
            services.AddSingleton<IRecipePipeline, FakeRecipePipeline>();
        else if (aiProvider == "GeminiVision" || aiProvider == "MistralVision")
            services.AddSingleton<IRecipePipeline, VisionRecipePipeline>();
        else
            services.AddSingleton<IRecipePipeline, RecipePipeline>();

    })
    .Build();

host.Run();




