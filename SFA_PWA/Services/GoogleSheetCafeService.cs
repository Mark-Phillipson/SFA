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
		private const string SpreadsheetId = "1DvIsV2Cga-xwxNkZs93Slt-_HnJy0noSZzFsGNCMyzo";
		private const string Range = "'Cafe Data Current'!A1:N250"; // Sheet name with spaces must be in single quotes
		private const string ApiKey = "AIzaSyCM9YN2xwYjU5vsYD0m73NmiDa8WQIF_rc"; // Google Sheets API key

		public GoogleSheetCafeService(HttpClient httpClient)
		{
			_httpClient = httpClient;
		}

		public async Task<List<Cafe>> GetCafesAsync()
		{
			var url = $"https://sheets.googleapis.com/v4/spreadsheets/{SpreadsheetId}/values/{Range}?key={ApiKey}";
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
