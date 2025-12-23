using Microsoft.AspNetCore.Mvc;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SFA_WebAPI.Controllers
{
    [ApiController]
    [Route("api/calendar")]
    public class CalendarProxyController : ControllerBase
    {
        private readonly ILogger<CalendarProxyController> _logger;
        private readonly IConfiguration _config;

        public CalendarProxyController(ILogger<CalendarProxyController> logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;
        }

        // GET api/calendar/ics?src={encoded}
        [HttpGet("ics")]
        public async Task<IActionResult> GetIcs([FromQuery] string src)
        {
            _logger.LogInformation("GetIcs called with src={src}", src);
            if (string.IsNullOrWhiteSpace(src))
            {
                _logger.LogWarning("GetIcs called with empty src");
                return BadRequest("src required");
            }

            // Determine ICS url
            string icsUrl = src;
            try
            {
                var lowered = src.ToLowerInvariant();
                if (lowered.Contains("calendar.google.com") && lowered.Contains("/ical/"))
                {
                    icsUrl = src;
                }
                else if (lowered.Contains("embed") || lowered.Contains("embed?src="))
                {
                    // src may be a full embed URL or an already-extracted src value.
                    try
                    {
                        var uri = new Uri(src);
                        var qs = System.Web.HttpUtility.ParseQueryString(uri.Query);
                        var s = qs.Get("src");
                        if (!string.IsNullOrWhiteSpace(s))
                        {
                            // decode any percent-encoding
                            s = System.Net.WebUtility.UrlDecode(s);
                            icsUrl = $"https://calendar.google.com/calendar/ical/{Uri.EscapeDataString(s)}/public/basic.ics";
                        }
                        else
                        {
                            // if no src query param, fallback to using host/path to detect calendar id
                            // e.g., some embed pages may include calendar id in path segments
                            icsUrl = src;
                        }
                    }
                    catch
                    {
                        // not a full URI; try to parse as query string or raw src
                        var q = System.Web.HttpUtility.ParseQueryString(src);
                        var s = q.Get("src") ?? src;
                        s = System.Net.WebUtility.UrlDecode(s);
                        icsUrl = $"https://calendar.google.com/calendar/ical/{Uri.EscapeDataString(s)}/public/basic.ics";
                    }
                }
                else if (src.Contains("@"))
                {
                    icsUrl = $"https://calendar.google.com/calendar/ical/{Uri.EscapeDataString(src)}/public/basic.ics";
                }
                else
                {
                    // fallback: if src looks like a path, attempt to use it unchanged
                    icsUrl = src;
                }

                using var client = new HttpClient();
                _logger.LogInformation("Fetching ICS from {icsUrl}", icsUrl);
                // Use a browser-like User-Agent to avoid some simple bot-blocking/redirects
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0 Safari/537.36");

                using var resp = await client.GetAsync(icsUrl);
                if (!resp.IsSuccessStatusCode)
                {
                    return StatusCode((int)resp.StatusCode, $"Failed fetching ICS: {(int)resp.StatusCode}");
                }
                var txt = await resp.Content.ReadAsStringAsync();

                // If result looks like HTML or doesn't contain VCALENDAR, try fallback ICS URL patterns
                if (txt.IndexOf("BEGIN:VCALENDAR", StringComparison.OrdinalIgnoreCase) < 0 || txt.TrimStart().StartsWith("<!DOCTYPE", StringComparison.OrdinalIgnoreCase))
                {
                    // Try decode src and construct basic.ics / full.ics
                    var decoded = System.Net.WebUtility.UrlDecode(src);
                    var calendarId = decoded;
                    // If query string or embed URL, try to extract src param
                    try
                    {
                        var u = new Uri(decoded);
                        var qs = System.Web.HttpUtility.ParseQueryString(u.Query);
                        var s = qs.Get("src");
                        if (!string.IsNullOrWhiteSpace(s)) calendarId = System.Net.WebUtility.UrlDecode(s);
                    }
                    catch { /* ignore */ }

                    var candidates = new[] {
                        $"https://calendar.google.com/calendar/ical/{Uri.EscapeDataString(calendarId)}/public/basic.ics",
                        $"https://calendar.google.com/calendar/ical/{Uri.EscapeDataString(calendarId)}/public/full.ics",
                        $"https://calendar.google.com/calendar/ical/{calendarId}/public/basic.ics",
                        $"https://calendar.google.com/calendar/ical/{calendarId}/public/full.ics"
                    };

                    foreach (var cand in candidates)
                    {
                        try
                        {
                            using var r2 = await client.GetAsync(cand);
                            if (!r2.IsSuccessStatusCode) continue;
                            var t2 = await r2.Content.ReadAsStringAsync();
                            if (t2.IndexOf("BEGIN:VCALENDAR", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                return Content(t2, "text/plain");
                            }
                        }
                        catch { }
                    }

                    // none of the fallbacks returned VCALENDAR; return original content for inspection
                }

                return Content(txt, "text/plain");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching ICS for src={src}", src);
                return StatusCode(500, ex.Message);
            }
        }

        // GET api/calendar/ping
        [HttpGet("ping")]
        public IActionResult Ping()
        {
            _logger.LogInformation("Ping received");
            return Ok("pong");
        }

        // GET api/calendar/debug?src={encoded}
        [HttpGet("debug")]
        public async Task<IActionResult> DebugIcs([FromQuery] string src)
        {
            if (string.IsNullOrWhiteSpace(src)) return BadRequest("src required");
            var attempts = new List<object>();
            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");

                // primary candidate built from src
                var primary = src;
                var candList = new List<string> { primary };

                // try to extract calendar id
                try
                {
                    var lowered = src.ToLowerInvariant();
                    if (lowered.Contains("embed") || lowered.Contains("src=") || src.Contains("@"))
                    {
                        var decoded = System.Net.WebUtility.UrlDecode(src);
                        string calendarId = decoded;
                        try
                        {
                            var u = new Uri(decoded);
                            var qs = System.Web.HttpUtility.ParseQueryString(u.Query);
                            var s = qs.Get("src");
                            if (!string.IsNullOrWhiteSpace(s)) calendarId = System.Net.WebUtility.UrlDecode(s);
                        }
                        catch { }
                        candList.Add($"https://calendar.google.com/calendar/ical/{Uri.EscapeDataString(calendarId)}/public/basic.ics");
                        candList.Add($"https://calendar.google.com/calendar/ical/{Uri.EscapeDataString(calendarId)}/public/full.ics");
                    }
                }
                catch { }

                foreach (var url in candList.Distinct())
                {
                    try
                    {
                        using var r = await client.GetAsync(url);
                        var s = await r.Content.ReadAsStringAsync();
                        var snippet = s.Length > 200 ? s.Substring(0, 200) : s;
                        var isVcal = s.IndexOf("BEGIN:VCALENDAR", StringComparison.OrdinalIgnoreCase) >= 0;
                        attempts.Add(new { url, status = (int)r.StatusCode, isVcal, snippet });
                    }
                    catch (Exception ex)
                    {
                        attempts.Add(new { url, status = (int?)null, isVcal = false, snippet = ex.Message });
                    }
                }

                return Ok(new { src, attempts });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DebugIcs error for src={src}", src);
                return StatusCode(500, ex.Message);
            }
        }

        // GET api/calendar/gcal?id={calendarId}
        [HttpGet("gcal")]
        public async Task<IActionResult> GetGoogleEvents([FromQuery] string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest("id required");
            var apiKey = _config["GoogleApiKey"] ?? _config["GoogleCalendarApi"]; // support both config names
            if (string.IsNullOrWhiteSpace(apiKey)) return BadRequest("GoogleApiKey not configured on server");

            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");
                var timeMin = DateTime.UtcNow.ToString("o");
                var url = $"https://www.googleapis.com/calendar/v3/calendars/{Uri.EscapeDataString(id)}/events?key={Uri.EscapeDataString(apiKey)}&timeMin={Uri.EscapeDataString(timeMin)}&singleEvents=true&orderBy=startTime&maxResults=250";
                using var resp = await client.GetAsync(url);
                if (!resp.IsSuccessStatusCode)
                {
                    var txt = await resp.Content.ReadAsStringAsync();
                    return StatusCode((int)resp.StatusCode, txt);
                }
                var json = await resp.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var items = new List<object>();
                if (doc.RootElement.TryGetProperty("items", out var arr) && arr.ValueKind == JsonValueKind.Array)
                {
                    foreach (var it in arr.EnumerateArray())
                    {
                        try
                        {
                            string summary = it.GetProperty("summary").GetString() ?? string.Empty;
                            string location = it.TryGetProperty("location", out var locEl) ? locEl.GetString() ?? string.Empty : string.Empty;
                            string description = it.TryGetProperty("description", out var dEl) ? dEl.GetString() ?? string.Empty : string.Empty;
                            DateTime? startDt = null;
                            if (it.TryGetProperty("start", out var sEl))
                            {
                                if (sEl.TryGetProperty("dateTime", out var dtEl))
                                {
                                    if (DateTime.TryParse(dtEl.GetString(), out var dtp)) startDt = dtp.ToUniversalTime();
                                }
                                else if (sEl.TryGetProperty("date", out var dDate))
                                {
                                    if (DateTime.TryParse(dDate.GetString(), out var dtp)) startDt = DateTime.SpecifyKind(dtp, DateTimeKind.Unspecified);
                                }
                            }
                            if (!startDt.HasValue) continue;

                            // convert to Europe/London for weekday check
                            TimeZoneInfo ukZone;
                            try { ukZone = OperatingSystem.IsWindows() ? TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time") : TimeZoneInfo.FindSystemTimeZoneById("Europe/London"); } catch { ukZone = TimeZoneInfo.Utc; }
                            var evUk = TimeZoneInfo.ConvertTimeFromUtc(startDt.Value.Kind == DateTimeKind.Utc ? startDt.Value : startDt.Value.ToUniversalTime(), ukZone);
                            var dow = evUk.DayOfWeek;
                            if (dow == DayOfWeek.Wednesday || dow == DayOfWeek.Saturday)
                            {
                                // extract links from description, but keep only route links
                                var links = new List<string>();
                                var desc = description ?? string.Empty;
                                foreach (Match m in Regex.Matches(desc, "https?://[\\w\\-./?%&=+#:@()~]+", RegexOptions.IgnoreCase))
                                {
                                    var u = m.Value.Trim();
                                    // Only treat links as route links if they mention ridewithgps or connect.garmin
                                    if (u.IndexOf("ridewithgps", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                        u.IndexOf("connect.garmin", StringComparison.OrdinalIgnoreCase) >= 0)
                                    {
                                        if (!links.Contains(u)) links.Add(u);
                                    }
                                }
                                items.Add(new { start = startDt.Value, summary, location, links });
                            }
                        }
                        catch { }
                    }
                }

                return Ok(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetGoogleEvents error for id={id}", id);
                return StatusCode(500, ex.Message);
            }
        }
    }
}
