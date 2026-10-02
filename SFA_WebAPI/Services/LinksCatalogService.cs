using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SFA_WebAPI.Models;

namespace SFA_WebAPI.Services;

public interface ILinksCatalogService
{
    Task<LinksCatalogSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default);
}

public sealed record LinksCatalogSnapshot(
    IReadOnlyList<LinkItem> Links,
    string ETag,
    DateTimeOffset LastModifiedUtc);

public sealed class LinksCatalogService : ILinksCatalogService
{
    private readonly ILogger<LinksCatalogService> _logger;
    private readonly string _dataFilePath;
    private readonly SemaphoreSlim _syncLock = new(1, 1);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private LinksCatalogSnapshot? _cachedSnapshot;
    private DateTimeOffset _cachedFileTimestampUtc;

    public LinksCatalogService(IConfiguration configuration, ILogger<LinksCatalogService> logger)
    {
        _logger = logger;
        _dataFilePath = ResolveDataFilePath(configuration["LinksDataPath"]);
        _logger.LogInformation("LinksCatalogService using file: {DataFilePath}", _dataFilePath);
    }

    public async Task<LinksCatalogSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var fileTimestampUtc = File.GetLastWriteTimeUtc(_dataFilePath);
        if (_cachedSnapshot is not null && fileTimestampUtc == _cachedFileTimestampUtc)
        {
            return _cachedSnapshot;
        }

        await _syncLock.WaitAsync(cancellationToken);
        try
        {
            fileTimestampUtc = File.GetLastWriteTimeUtc(_dataFilePath);
            if (_cachedSnapshot is not null && fileTimestampUtc == _cachedFileTimestampUtc)
            {
                return _cachedSnapshot;
            }

            var fileBytes = await File.ReadAllBytesAsync(_dataFilePath, cancellationToken);
            var links = JsonSerializer.Deserialize<List<LinkItem>>(fileBytes, JsonOptions);
            if (links is null)
            {
                throw new InvalidDataException($"Links file '{_dataFilePath}' contains invalid JSON.");
            }

            var eTag = CreateStrongETag(fileBytes);
            _cachedSnapshot = new LinksCatalogSnapshot(links, eTag, fileTimestampUtc);
            _cachedFileTimestampUtc = fileTimestampUtc;
            return _cachedSnapshot;
        }
        finally
        {
            _syncLock.Release();
        }
    }

    private string ResolveDataFilePath(string? configuredPath)
    {
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            var resolvedPath = configuredPath;
            if (!Path.IsPathRooted(resolvedPath))
            {
                resolvedPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), resolvedPath));
            }

            EnsureFileExists(resolvedPath);
            return resolvedPath;
        }

        var candidatePaths = new[]
        {
            Path.Combine(Directory.GetCurrentDirectory(), "data", "links.json"),
            Path.Combine(AppContext.BaseDirectory, "data", "links.json"),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "SFA_WebAPI", "data", "links.json"))
        };

        foreach (var path in candidatePaths)
        {
            if (File.Exists(path))
            {
                return path;
            }
        }

        var defaultPath = candidatePaths[0];
        EnsureFileExists(defaultPath);
        return defaultPath;
    }

    private static void EnsureFileExists(string absolutePath)
    {
        var directory = Path.GetDirectoryName(absolutePath);
        if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        if (!File.Exists(absolutePath))
        {
            throw new FileNotFoundException($"Links file not found: {absolutePath}", absolutePath);
        }
    }

    private static string CreateStrongETag(byte[] payload)
    {
        var hash = SHA256.HashData(payload);
        var base64 = Convert.ToBase64String(hash);
        return $"\"{base64}\"";
    }
}
