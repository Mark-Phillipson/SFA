using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using SFA_PWA.Models;

namespace SFA_PWA.Services
{
    /// <summary>
    /// Service that provides cafe data with offline fallback support.
    /// Uses Google Sheets API when online, falls back to cached data when offline.
    /// </summary>
    public class CafeDataCache
    {
        private readonly GoogleSheetCafeService _googleSheetService;
        private readonly IJSRuntime _jsRuntime;
        private const string LocalStorageKey = "cachedCafes";
        private const string LastUpdateKey = "cafesLastUpdate";

        public CafeDataCache(GoogleSheetCafeService googleSheetService, IJSRuntime jsRuntime)
        {
            _googleSheetService = googleSheetService;
            _jsRuntime = jsRuntime;
        }

        /// <summary>
        /// Gets cafe data, attempting online fetch first, falling back to cache if offline.
        /// </summary>
        public async Task<(List<Cafe> Cafes, bool IsFromCache)> GetCafesWithCacheAsync()
        {
            try
            {
                // Try to fetch from Google Sheets
                var cafes = await _googleSheetService.GetCafesAsync();
                
                // Success - cache the data for offline use
                await CacheCafesAsync(cafes);
                
                return (cafes, false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to fetch cafes from Google Sheets: {ex.Message}");
                
                // Network error or API failure - try to get cached data
                var cachedCafes = await GetCachedCafesAsync();
                if (cachedCafes != null && cachedCafes.Count > 0)
                {
                    Console.WriteLine("Using cached cafe data for offline access.");
                    return (cachedCafes, true);
                }
                
                // No cache available, re-throw the exception
                throw;
            }
        }

        private async Task CacheCafesAsync(List<Cafe> cafes)
        {
            try
            {
                var json = JsonSerializer.Serialize(cafes);
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", LocalStorageKey, json);
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", LastUpdateKey, DateTime.UtcNow.ToString("o", System.Globalization.CultureInfo.InvariantCulture));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to cache cafes: {ex.Message}");
            }
        }

        private async Task<List<Cafe>?> GetCachedCafesAsync()
        {
            try
            {
                var json = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", LocalStorageKey);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return null;
                }
                
                return JsonSerializer.Deserialize<List<Cafe>>(json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to retrieve cached cafes: {ex.Message}");
                return null;
            }
        }

        public async Task<DateTime?> GetLastUpdateTimeAsync()
        {
            try
            {
                var timestamp = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", LastUpdateKey);
                if (string.IsNullOrWhiteSpace(timestamp))
                {
                    return null;
                }
                
                if (DateTime.TryParse(timestamp, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind, out var result))
                {
                    return result;
                }
                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}
