using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;

namespace SFA_PWA;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSfaPwaServices(
        this IServiceCollection services,
        Uri baseAddress,
        Uri botApiBaseUri)
    {
        // Default HttpClient for static/data requests
        services.AddScoped(_ => new HttpClient { BaseAddress = baseAddress });

        services.AddScoped<SFA_PWA.Services.GoogleSheetCafeService>();
        services.AddScoped<SFA_PWA.Services.CafeDataCache>();
        services.AddScoped<SFA_PWA.Services.NetworkStatusService>();

        // Bot API client + config
        services.AddScoped(_ => new BotApiHttpClient(new HttpClient { BaseAddress = botApiBaseUri }));
        services.AddSingleton(new BotApiConfig { BotApiUrl = botApiBaseUri.ToString() });

        return services;
    }
}
