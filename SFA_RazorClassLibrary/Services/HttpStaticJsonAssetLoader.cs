using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SFA_RazorClassLibrary.Services;

public sealed class HttpStaticJsonAssetLoader(HttpClient http) : IStaticJsonAssetLoader
{
    public async Task<T?> LoadAsync<T>(string relativePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return default;
        }

        // This implementation is intended for browser/WASM hosts where relative URLs are served by the host.
        // In Hybrid hosts (MAUI), this should be overridden.
        return await http.GetFromJsonAsync<T>(relativePath, cancellationToken);
    }
}
