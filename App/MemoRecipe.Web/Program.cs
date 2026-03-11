using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MemoRecipe.Web;
using MudBlazor.Services;
using MemoRecipe.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

//Add Services
builder.Services.AddMudServices();
// builder.Services.AddScoped<IAuthService, AuthService>();
// builder.Services.AddScoped<ILocalStorageService, LocalStorageService>();
builder.Services.AddScoped<IAuthService, FakeAuthService>();


builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("http://localhost:5131") });

await builder.Build().RunAsync();
