using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MemoRecipe.Web;
using MudBlazor.Services;
using MemoRecipe.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
builder.Services.AddAuthorizationCore();

// Add Services
builder.Services.AddMudServices();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<AuthenticationStateProvider, CookieAuthStateProvider>();
builder.Services.AddScoped<IRecipeService, RecipeService>();
builder.Services.AddScoped<IFeatureFlagsService, FeatureFlagsService>();

builder.Services.AddTransient<CookieHandler>();

var apiBaseUrl = builder.Configuration["ApiBaseUrl"];
var apiUri = string.IsNullOrEmpty(apiBaseUrl)
    ? new Uri(builder.HostEnvironment.BaseAddress)
    : new Uri(apiBaseUrl);

builder.Services.AddHttpClient("MemoRecipe", client =>
    client.BaseAddress = apiUri)
    .AddHttpMessageHandler<CookieHandler>();

await builder.Build().RunAsync();
