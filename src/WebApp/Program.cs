using Blazored.LocalStorage;

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

using MudBlazor.Services;

using WeeklyUp.WebApp;
using WeeklyUp.WebApp.Extensions;
using WeeklyUp.WebApp.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddMudServices();
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddAuthorizationCore();
builder.Services.AddSingleton<JwtAuthenticationStateProvider>();
builder.Services.AddSingleton<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<JwtAuthenticationStateProvider>());
builder.Services.AddSingleton<IAuthStateNotifier>(sp =>
    sp.GetRequiredService<JwtAuthenticationStateProvider>());
builder.Services.AddSingleton<IAuthService, AuthService>();
builder.Services.AddWeeklyUpHttpClient(builder.Configuration);

await builder.Build().RunAsync();
