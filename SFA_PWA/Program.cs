using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SFA_PWA;
using System.Text.Json;
using System.Net.Http;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

var httpClient = new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };
var configResponse = await httpClient.GetAsync("appsettings.json");
string configJson = await configResponse.Content.ReadAsStringAsync();
var configDoc = JsonDocument.Parse(configJson);

string botApiUrl = configDoc.RootElement.TryGetProperty("BotApiUrl", out var botApiUrlElement)
    ? botApiUrlElement.GetString() ?? string.Empty
    : string.Empty;

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Register default HttpClient for PWA static/data requests
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Register GoogleSheetCafeService for DI
builder.Services.AddScoped<SFA_PWA.Services.GoogleSheetCafeService>();

// Register CafeDataCache for offline support
builder.Services.AddScoped<SFA_PWA.Services.CafeDataCache>();

// Register NetworkStatusService for connectivity detection
builder.Services.AddScoped<SFA_PWA.Services.NetworkStatusService>();

// Register BotApi HttpClient for chatbot requests
// If BotApiUrl is not set or points to localhost (common when running locally),m
// fall back to the site's origin so production static hosting can call the API
// via the same origin (e.g. proxied /api endpoints).
Uri botApiBaseUri = new Uri(botApiUrl);
builder.Services.AddScoped<BotApiHttpClient>(sp => new BotApiHttpClient(new HttpClient { BaseAddress = botApiBaseUri }));

// Optionally, register resolved BotApiUrl for DI (helps with diagnostics)
builder.Services.AddSingleton(new BotApiConfig { BotApiUrl = botApiBaseUri.ToString() });

await builder.Build().RunAsync();

// BotApiHttpClient wrapper class
public class BotApiHttpClient
{
    public HttpClient Client { get; }
    public BotApiHttpClient(HttpClient client)
    {
        Client = client;
    }
}

// BotApiConfig class for DI
public class BotApiConfig
{
    public string BotApiUrl { get; set; } = string.Empty;
}
