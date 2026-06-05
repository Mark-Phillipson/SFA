using SFA_RazorClassLibrary.Services;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SFA_MauiApp.Services;

public sealed class MauiStaticJsonAssetLoader : IStaticJsonAssetLoader
{
    public async Task<T?> LoadAsync<T>(string relativePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return default;
        }

        // Map "sample-data/links.json" -> "wwwroot/sample-data/links.json".
        var normalized = relativePath.Replace('\\', '/').TrimStart('/');
        var appPackagePath = normalized.StartsWith("wwwroot/", StringComparison.OrdinalIgnoreCase)
            ? normalized
            : $"wwwroot/{normalized}";

        await using var stream = await FileSystem.OpenAppPackageFileAsync(appPackagePath);
        return await JsonSerializer.DeserializeAsync<T>(
            stream,
            new JsonSerializerOptions(JsonSerializerDefaults.Web),
            cancellationToken);
    }
}
