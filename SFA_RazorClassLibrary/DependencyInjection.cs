using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Net.Http;

namespace SFA_RazorClassLibrary;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSfaPwaServices(
        this IServiceCollection services,
        Uri baseAddress,
        Uri botApiBaseUri,
        string? googleSheetsApiKey = null)
    {
        services.TryAddSingleton<SFA_RazorClassLibrary.Services.ISfaHostCapabilities, SFA_RazorClassLibrary.Services.BrowserHostCapabilities>();

        // Default HttpClient for static/data requests (hosts may override)
        services.TryAddScoped(_ => new HttpClient { BaseAddress = baseAddress });

        // Default loader for static JSON assets (hosts like MAUI should override)
        services.TryAddScoped<SFA_RazorClassLibrary.Services.IStaticJsonAssetLoader, SFA_RazorClassLibrary.Services.HttpStaticJsonAssetLoader>();

        services.AddScoped(_ => new SFA_RazorClassLibrary.Services.GoogleSheetCafeService(new HttpClient { BaseAddress = baseAddress }, googleSheetsApiKey));
        services.AddScoped<SFA_RazorClassLibrary.Services.CafeDataCache>();
        services.AddScoped<SFA_RazorClassLibrary.Services.CalendarFeedService>();
        services.AddScoped<SFA_RazorClassLibrary.Services.NetworkStatusService>();

        // Bot API client + config
        services.AddScoped(_ => new SFA_RazorClassLibrary.Services.BotApiHttpClient(new HttpClient { BaseAddress = botApiBaseUri }));
        services.AddSingleton(new SFA_RazorClassLibrary.Services.BotApiConfig { BotApiUrl = botApiBaseUri.ToString() });

        return services;
    }
}
