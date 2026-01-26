using memorecipe_ia.Infrastructure.OCR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using MemoRecipeIA.Application.Interfaces;


var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    
    .ConfigureServices(services =>
    {
        services.AddSingleton<IOcrService, TesseractOcrService>();
    })
    .Build();

host.Run();


