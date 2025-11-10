using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SFA_PWA;
using System.Text.Json;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

var httpClient = new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };
var configResponse = await httpClient.GetAsync("appsettings.json");
string configJson = await configResponse.Content.ReadAsStringAsync();
var configDoc = JsonDocument.Parse(configJson);
string botApiUrl = "";
if (configDoc.RootElement.TryGetProperty("BotApiUrl", out var botApiUrlElement))
{
    botApiUrl = botApiUrlElement.GetString() ?? "";
}

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Register HttpClient for API calls to Bot API
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(botApiUrl) });

// Optionally, register BotApiUrl for DI
builder.Services.AddSingleton(new BotApiConfig { BotApiUrl = botApiUrl });

await builder.Build().RunAsync();

// BotApiConfig class for DI
public class BotApiConfig
{
    public string BotApiUrl { get; set; } = string.Empty;
}
