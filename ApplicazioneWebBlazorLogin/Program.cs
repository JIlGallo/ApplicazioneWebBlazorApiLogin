using ApplicazioneWebBlazorLogin.Services;
using ApplicazioneWebBlazorLogin;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp =>
{
    // Leggi la BaseAddress da una configurazione, ad esempio appsettings.json
    var apiBaseUrl = builder.Configuration.GetValue<string>("ApiBaseUrl");

    if (string.IsNullOrEmpty(apiBaseUrl))
    {
        // Se non è configurato, usa l'indirizzo di base predefinito
        apiBaseUrl = builder.HostEnvironment.BaseAddress;
    }

    return new HttpClient { BaseAddress = new Uri(apiBaseUrl) };
});
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    new CustomAuthenticationStateProvider(
        sp.GetRequiredService<ILocalStorageService>(),
        sp.GetRequiredService<HttpClient>(),
        sp.GetRequiredService<NavigationManager>() // Passa NavigationManager al costruttore
    )
);
builder.Services.AddScoped<GeolocationService>();


// Blazor WebAssembly in Program.cs

builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();

builder.Services.AddBlazoredLocalStorage();

// Registrazione del Custom AuthenticationStateProvider
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();
builder.Services.AddScoped<UserService>();

builder.Services.AddAuthorizationCore();

await builder.Build().RunAsync();
