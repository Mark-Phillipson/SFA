using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.Json;
using SFA_PWA.Models;

namespace SFA_PWA.Services
{
	public class GoogleSheetCafeService
	{
		private readonly HttpClient _httpClient;
		private string _apiKey;
		private const string SpreadsheetId = "1DvIsV2Cga-xwxNkZs93Slt-_HnJy0noSZzFsGNCMyzo";
		private const string Range = "'Cafe Data Current'!A1:N250"; // Sheet name with spaces must be in single quotes

		public GoogleSheetCafeService(HttpClient httpClient)
		{
			_httpClient = httpClient;
			_apiKey = string.Empty;
		}

		private async Task EnsureApiKeyAsync()
		{
			if (!string.IsNullOrWhiteSpace(_apiKey)) return;

			// Try to get API key from environment variable (server-side only)
			_apiKey = Environment.GetEnvironmentVariable("GOOGLE_SHEETS_API_KEY") ?? string.Empty;
			if (!string.IsNullOrWhiteSpace(_apiKey)) return;

			// Fallback: fetch config from wwwroot/appsettings.Development.json via HttpClient (local dev)
			try
			{
				var configJson = await _httpClient.GetStringAsync("appsettings.Development.json");
				var configDoc = JsonDocument.Parse(configJson);
				if (configDoc.RootElement.TryGetProperty("GoogleSheetsApiKey", out var apiKeyElement))
				{
					_apiKey = apiKeyElement.GetString() ?? string.Empty;
					return;
				}
			}
			catch (Exception exception)
			{
				System.Console.WriteLine("Google Sheets API key not found in appsettings.Development.json: " + exception.Message);
			}
			// Fallback: fetch config from wwwroot/appsettings.json via HttpClient (production)
			try
			{
				var configJson = await _httpClient.GetStringAsync("appsettings.json");
				var configDoc = JsonDocument.Parse(configJson);
				if (configDoc.RootElement.TryGetProperty("GoogleSheetsApiKey", out var apiKeyElement))
				{
					_apiKey = apiKeyElement.GetString() ?? string.Empty;
				}
			}
			catch (Exception exception)
			{
				System.Console.WriteLine("Google Sheets API key not found in appsettings.json: " + exception.Message);
				// Ignore errors, will throw below if not found
			}
		}

		public async Task<List<Cafe>> GetCafesAsync()
		{
			await EnsureApiKeyAsync();
			if (string.IsNullOrWhiteSpace(_apiKey))
				throw new InvalidOperationException("Google Sheets API key is not configured.");

			var url = $"https://sheets.googleapis.com/v4/spreadsheets/{SpreadsheetId}/values/{Range}?key={_apiKey}";
			var response = await _httpClient.GetAsync(url);
			response.EnsureSuccessStatusCode();
			var json = await response.Content.ReadAsStringAsync();
			var sheetData = JsonDocument.Parse(json);
			var cafes = new List<Cafe>();

			if (sheetData.RootElement.TryGetProperty("values", out var values))
			{
				var rows = values.EnumerateArray().Skip(7); // Skip first 7 rows, data starts at row 8
				foreach (var row in rows)
				{
					var columns = row.EnumerateArray().ToArray();
					var cafe = new Cafe
					{
						ToRWGPS = columns.Length > 0 ? columns[0].GetString() : null,
						Status = columns.Length > 1 ? columns[1].GetString() : null,
						Name = columns.Length > 2 ? columns[2].GetString() : null,
						LatLong = columns.Length > 3 ? columns[3].GetString() : null,
						PlusCode = columns.Length > 4 ? columns[4].GetString() : null,
						GoogleMapLink = columns.Length > 5 ? columns[5].GetString() : null,
						CafeMapLink = columns.Length > 6 ? columns[6].GetString() : null,
						Description = columns.Length > 7 ? columns[7].GetString() : null,
						Place = columns.Length > 8 ? columns[8].GetString() : null,
						Address = columns.Length > 9 ? columns[9].GetString() : null,
						Postcode = columns.Length > 10 ? columns[10].GetString() : null,
						Tel = columns.Length > 11 ? columns[11].GetString() : null,
						OSGrid = columns.Length > 12 ? columns[12].GetString() : null,
						Notes = columns.Length > 13 ? columns[13].GetString() : null
					};
					cafes.Add(cafe);
				}
			}
			return cafes;
		}
	}
}
