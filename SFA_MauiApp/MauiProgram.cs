using Microsoft.Extensions.Logging;
using SFA_MauiApp.Services;
using SFA_PWA;
using SFA_PWA.Services;
using System.Text.Json;

namespace SFA_MauiApp;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
			});

		builder.Services.AddMauiBlazorWebView();

		builder.Services.AddSingleton<ISfaHostCapabilities, MauiHostCapabilities>();
		builder.Services.AddScoped<SFA_PWA.Services.IStaticJsonAssetLoader, MauiStaticJsonAssetLoader>();

		// Load the same wwwroot/appsettings.json shape as the PWA host.
		var baseAddress = new Uri("https://0.0.0.0/");
		var botApiBaseUri = GetBotApiBaseUriFromAppSettings();
		builder.Services.AddSfaPwaServices(baseAddress, botApiBaseUri);
		// Ensure Razor components that @inject HttpClient get a BaseAddress-backed client.
		builder.Services.AddScoped(_ => new HttpClient { BaseAddress = baseAddress });

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}

	private static Uri GetBotApiBaseUriFromAppSettings()
	{
		try
		{
			using var stream = FileSystem.OpenAppPackageFileAsync("wwwroot/appsettings.json").GetAwaiter().GetResult();
			using var doc = JsonDocument.Parse(stream);
			if (doc.RootElement.TryGetProperty("BotApiUrl", out var botApiUrlElement))
			{
				var url = botApiUrlElement.GetString();
				if (!string.IsNullOrWhiteSpace(url))
				{
					return new Uri(url);
				}
			}
		}
		catch
		{
			// Ignore and fall back.
		}

		// Fallback to the current production host.
		return new Uri("https://sfawebapi-c9bgawf9evfkg6dp.westeurope-01.azurewebsites.net/");
	}
}
