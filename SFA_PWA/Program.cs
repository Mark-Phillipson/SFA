using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SFA_RazorClassLibrary;
using System.Text.Json;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

var httpClient = new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };
var configResponse = await httpClient.GetAsync("appsettings.json");
string configJson = await configResponse.Content.ReadAsStringAsync();
var configDoc = JsonDocument.Parse(configJson);

string botApiUrl = configDoc.RootElement.TryGetProperty("BotApiUrl", out var botApiUrlElement)
    ? botApiUrlElement.GetString() ?? string.Empty
    : string.Empty;

builder.RootComponents.Add<SFA_PWA.App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// All app services now live in the Razor Class Library (shared with future MAUI host).
Uri botApiBaseUri = new Uri(botApiUrl);
builder.Services.AddSfaPwaServices(new Uri(builder.HostEnvironment.BaseAddress), botApiBaseUri);

await builder.Build().RunAsync();
