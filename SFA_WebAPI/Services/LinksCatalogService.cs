using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SFA_WebAPI.Models;

namespace SFA_WebAPI.Services;

public interface ILinksCatalogService
{
    Task<LinksCatalogSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default);
    Task<LinkItem> CreateAsync(LinkItem link, CancellationToken cancellationToken = default);
    Task UpdateAsync(int index, LinkItem link, CancellationToken cancellationToken = default);
    Task DeleteAsync(int index, CancellationToken cancellationToken = default);
}

public sealed record LinksCatalogSnapshot(
    IReadOnlyList<LinkItem> Links,
    string ETag,
    DateTimeOffset LastModifiedUtc);

public sealed class LinksCatalogService : ILinksCatalogService
{
    private readonly ILogger<LinksCatalogService> _logger;
    private readonly string? _dataFilePath;
    private readonly SemaphoreSlim _syncLock = new(1, 1);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private LinksCatalogSnapshot? _cachedSnapshot;
    private DateTimeOffset _cachedFileTimestampUtc;

    public LinksCatalogService(IConfiguration configuration, ILogger<LinksCatalogService> logger)
    {
        _logger = logger;
        _dataFilePath = ResolveDataFilePath(configuration["LinksDataPath"]);
        if (_dataFilePath is null)
        {
            _logger.LogWarning("LinksCatalogService has no persisted links file configured for this environment. Writes are disabled until LinksDataPath points to a valid file.");
            return;
        }

        _logger.LogInformation("LinksCatalogService using file: {DataFilePath}", _dataFilePath);
    }

    public async Task<LinksCatalogSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        if (_dataFilePath is null)
        {
            throw new InvalidOperationException("Links catalog file is not configured for this environment.");
        }

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

    public async Task<LinkItem> CreateAsync(LinkItem link, CancellationToken cancellationToken = default)
    {
        await _syncLock.WaitAsync(cancellationToken);
        try
        {
            var links = await ReadLinksUnsafeAsync(cancellationToken);
            links.Add(Normalize(link));
            await WriteLinksUnsafeAsync(links, cancellationToken);
            return links[^1];
        }
        finally
        {
            _syncLock.Release();
        }
    }

    public async Task UpdateAsync(int index, LinkItem link, CancellationToken cancellationToken = default)
    {
        await _syncLock.WaitAsync(cancellationToken);
        try
        {
            var links = await ReadLinksUnsafeAsync(cancellationToken);
            if (index < 0 || index >= links.Count)
            {
                throw new KeyNotFoundException($"Link index {index} was not found.");
            }

            links[index] = Normalize(link);
            await WriteLinksUnsafeAsync(links, cancellationToken);
        }
        finally
        {
            _syncLock.Release();
        }
    }

    public async Task DeleteAsync(int index, CancellationToken cancellationToken = default)
    {
        await _syncLock.WaitAsync(cancellationToken);
        try
        {
            var links = await ReadLinksUnsafeAsync(cancellationToken);
            if (index < 0 || index >= links.Count)
            {
                throw new KeyNotFoundException($"Link index {index} was not found.");
            }

            links.RemoveAt(index);
            await WriteLinksUnsafeAsync(links, cancellationToken);
        }
        finally
        {
            _syncLock.Release();
        }
    }

    private string? ResolveDataFilePath(string? configuredPath)
    {
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            var resolvedPath = configuredPath;
            if (!Path.IsPathRooted(resolvedPath))
            {
                resolvedPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), resolvedPath));
            }

            ensureParentDirectoryExists(resolvedPath);
            return resolvedPath;
        }

        var candidatePaths = new[]
        {
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "SFA_WebAPI", "data", "links.json"))
        };

        foreach (var path in candidatePaths)
        {
            if (File.Exists(path))
            {
                return path;
            }
        }

        return null;
    }

    private static void ensureParentDirectoryExists(string absolutePath)
    {
        var directory = Path.GetDirectoryName(absolutePath);
        if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
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
            File.WriteAllText(absolutePath, "[]");
        }
    }

    private static string CreateStrongETag(byte[] payload)
    {
        var hash = SHA256.HashData(payload);
        var base64 = Convert.ToBase64String(hash);
        return $"\"{base64}\"";
    }

    private async Task<List<LinkItem>> ReadLinksUnsafeAsync(CancellationToken cancellationToken)
    {
        if (_dataFilePath is null)
        {
            throw new InvalidOperationException("Links catalog file is not configured for this environment.");
        }

        var fileBytes = await File.ReadAllBytesAsync(_dataFilePath, cancellationToken);
        var links = JsonSerializer.Deserialize<List<LinkItem>>(fileBytes, JsonOptions);
        if (links is null)
        {
            throw new InvalidDataException($"Links file '{_dataFilePath}' contains invalid JSON.");
        }

        return links;
    }

    private async Task WriteLinksUnsafeAsync(List<LinkItem> links, CancellationToken cancellationToken)
    {
        if (_dataFilePath is null)
        {
            throw new InvalidOperationException("Links catalog file is not configured for this environment.");
        }

        if (!File.Exists(_dataFilePath))
        {
            throw new InvalidOperationException($"Links catalog file does not exist and writes are disabled: {_dataFilePath}");
        }

        var payload = JsonSerializer.SerializeToUtf8Bytes(links, new JsonSerializerOptions(JsonOptions)
        {
            WriteIndented = true
        });
        await File.WriteAllBytesAsync(_dataFilePath, payload, cancellationToken);

        var fileTimestampUtc = File.GetLastWriteTimeUtc(_dataFilePath);
        var eTag = CreateStrongETag(payload);
        _cachedSnapshot = new LinksCatalogSnapshot(links, eTag, fileTimestampUtc);
        _cachedFileTimestampUtc = fileTimestampUtc;
    }

    private static LinkItem Normalize(LinkItem link)
    {
        return new LinkItem
        {
            Description = link.Description?.Trim(),
            Url = link.Url?.Trim(),
            Emoji = link.Emoji?.Trim(),
            Category = link.Category?.Trim()
        };
    }
}
