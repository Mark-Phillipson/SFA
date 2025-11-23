using System.Text.Json;
using SFA_WebAPI.Models;

namespace SFA_WebAPI.Services
{
    public interface IStartPointRepository
    {
        Task<List<StartPoint>> GetAllAsync();
        Task<StartPoint?> GetByIdAsync(string id);
        Task CreateAsync(StartPoint sp);
        Task UpdateAsync(string id, StartPoint sp);
        Task DeleteAsync(string id);
    }

    public class StartPointRepository : IStartPointRepository
    {
        private readonly string _dataFile;
        private readonly SemaphoreSlim _lock = new(1, 1);
        private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true };
        private readonly ILogger<StartPointRepository> _logger;

        // Constructor reads path from configuration so we can point at the PWA's bundled JSON
        public StartPointRepository(IConfiguration config, ILogger<StartPointRepository> logger)
        {
            _logger = logger;
            var configured = config["StartPointDataPath"] ?? string.Empty;
            if (string.IsNullOrWhiteSpace(configured))
            {
                var dataDir = Path.Combine(Directory.GetCurrentDirectory(), "data");
                if (!Directory.Exists(dataDir)) Directory.CreateDirectory(dataDir);
                _dataFile = Path.Combine(dataDir, "startpoints.json");
            }
            else
            {
                // If relative path, resolve against the WebAPI current directory
                var path = configured;
                if (!Path.IsPathRooted(path))
                {
                    path = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), path));
                }
                _dataFile = path;
                var dir = Path.GetDirectoryName(_dataFile) ?? Directory.GetCurrentDirectory();
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            }
            _logger.LogInformation($"StartPointRepository using file: {_dataFile}");

            if (!File.Exists(_dataFile)) 
            {
                _logger.LogWarning($"File not found, creating empty array at {_dataFile}");
                File.WriteAllText(_dataFile, "[]");
            }
        }

        public async Task<List<StartPoint>> GetAllAsync()
        {
            await _lock.WaitAsync();
            try
            {
                return await GetAllInternalAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAllAsync");
                throw;
            }
            finally { _lock.Release(); }
        }

        private async Task<List<StartPoint>> GetAllInternalAsync()
        {
            if (!File.Exists(_dataFile))
            {
                _logger.LogWarning($"File {_dataFile} does not exist in GetAllAsync");
                return new List<StartPoint>();
            }
            var json = await File.ReadAllTextAsync(_dataFile);
            if (string.IsNullOrWhiteSpace(json)) return new List<StartPoint>();

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.Array)
            {
                var list = JsonSerializer.Deserialize<List<StartPoint>>(json, _jsonOptions) ?? new List<StartPoint>();
                return list;
            }
            else if (root.ValueKind == JsonValueKind.Object)
            {
                // Support wrapped structure { group:..., source:..., startPoints: [ ... ] }
                if (root.TryGetProperty("startPoints", out var spProp) && spProp.ValueKind == JsonValueKind.Array)
                {
                    var list = JsonSerializer.Deserialize<List<StartPoint>>(spProp.GetRawText(), _jsonOptions) ?? new List<StartPoint>();
                    return list;
                }
                // Fallback: try lowercase name
                if (root.TryGetProperty("startpoints", out var spProp2) && spProp2.ValueKind == JsonValueKind.Array)
                {
                    var list = JsonSerializer.Deserialize<List<StartPoint>>(spProp2.GetRawText(), _jsonOptions) ?? new List<StartPoint>();
                    return list;
                }
            }

            return new List<StartPoint>();
        }

        public async Task<StartPoint?> GetByIdAsync(string id)
        {
            // Reuse GetAllAsync which handles locking
            var list = await GetAllAsync();
            return list.FirstOrDefault(s => s.Id == id);
        }

        public async Task CreateAsync(StartPoint sp)
        {
            if (string.IsNullOrWhiteSpace(sp.Id)) sp.Id = Guid.NewGuid().ToString();
            await _lock.WaitAsync();
            try
            {
                var list = await GetAllInternalAsync();
                list.Add(sp);
                await WriteListInternalAsync(list);
            }
            finally { _lock.Release(); }
        }

        public async Task UpdateAsync(string id, StartPoint sp)
        {
            await _lock.WaitAsync();
            try
            {
                var list = await GetAllInternalAsync();
                var idx = list.FindIndex(s => s.Id == id);
                if (idx == -1) throw new KeyNotFoundException("Start point not found");
                sp.Id = id;
                list[idx] = sp;
                await WriteListInternalAsync(list);
            }
            finally { _lock.Release(); }
        }

        public async Task DeleteAsync(string id)
        {
            await _lock.WaitAsync();
            try
            {
                var list = await GetAllInternalAsync();
                var removed = list.RemoveAll(s => s.Id == id);
                if (removed == 0) throw new KeyNotFoundException("Start point not found");
                await WriteListInternalAsync(list);
            }
            finally { _lock.Release(); }
        }

        private async Task WriteListInternalAsync(List<StartPoint> list)
        {
            // Preserve wrapper metadata (group, source) if present; otherwise write a simple array
            var existing = await File.ReadAllTextAsync(_dataFile);
            if (string.IsNullOrWhiteSpace(existing))
            {
                var arrJson = JsonSerializer.Serialize(list, _jsonOptions);
                await File.WriteAllTextAsync(_dataFile, arrJson);
                return;
            }

            try
            {
                using var doc = JsonDocument.Parse(existing);
                var root = doc.RootElement;
                if (root.ValueKind == JsonValueKind.Object)
                {
                    // Extract known metadata if present
                    string? group = null;
                    string? source = null;
                    if (root.TryGetProperty("group", out var g) && g.ValueKind == JsonValueKind.String) group = g.GetString();
                    if (root.TryGetProperty("source", out var s) && s.ValueKind == JsonValueKind.String) source = s.GetString();

                    var wrapper = new
                    {
                        group,
                        source,
                        startPoints = list
                    };
                    var json = JsonSerializer.Serialize(wrapper, _jsonOptions);
                    await File.WriteAllTextAsync(_dataFile, json);
                    return;
                }
            }
            catch
            {
                // ignore parse errors and fall back to array
            }

            var arrayJson = JsonSerializer.Serialize(list, _jsonOptions);
            await File.WriteAllTextAsync(_dataFile, arrayJson);
        }
    }
}
