using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SFA_PWA.Services
{
    public class CalendarEvent
    {
        public string GroupName { get; set; } = string.Empty;
        public DateTime Start { get; set; }
        public string Summary { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        // OriginalDescription stores the event description (if present) so the UI can show useful text when Location is empty.
        public string OriginalDescription { get; set; } = string.Empty;
        public List<string> RouteLinks { get; set; } = new();
    }

    public class CalendarFeedService
    {
        private readonly HttpClient _http;
        private readonly string _apiBaseUrl;

        public CalendarFeedService(HttpClient http, BotApiConfig apiConfig)
        {
            _http = http;
            _apiBaseUrl = apiConfig?.BotApiUrl?.TrimEnd('/') ?? string.Empty;
        }

        public record ProbeResult(string GroupName, string IcsUrl, bool Success, string? Error, string? Snippet, int? EventsFound);

        public async Task<List<ProbeResult>> ProbeGroupsAsync(IEnumerable<(string Name, string CalendarUrl)> groups)
        {
            var results = new List<ProbeResult>();
            foreach (var g in groups)
            {
                string? ics = null;
                try
                {
                    var probeSrc = ConvertEmbedToIcsUrl(g.CalendarUrl) ?? g.CalendarUrl;
                    ics = !string.IsNullOrWhiteSpace(_apiBaseUrl)
                        ? $"{_apiBaseUrl}/api/calendar/ics?src={Uri.EscapeDataString(probeSrc)}"
                        : $"api/calendar/ics?src={Uri.EscapeDataString(probeSrc)}";
                    using var resp = await _http.GetAsync(ics);
                    if (!resp.IsSuccessStatusCode)
                    {
                        results.Add(new ProbeResult(g.Name, ics, false, $"HTTP {(int)resp.StatusCode}", null, null));
                        continue;
                    }
                    var txt = await resp.Content.ReadAsStringAsync();
                    var snippet = txt.Length > 1000 ? txt.Substring(0, 1000) : txt;
                    var events = ParseIcs(txt, g.Name);
                    results.Add(new ProbeResult(g.Name, ics, true, null, snippet, events?.Count ?? 0));
                }
                catch (Exception ex)
                {
                    results.Add(new ProbeResult(g.Name, ics ?? string.Empty, false, ex.Message, null, null));
                }
            }
            return results;
        }

        public async Task<List<ProbeResult>> ProbeGroupsGcalAsync(IEnumerable<(string Name, string CalendarUrl)> groups)
        {
            var results = new List<ProbeResult>();
            foreach (var g in groups)
            {
                string? endpoint = null;
                try
                {
                    // try to extract calendar id
                    var calendarId = ConvertEmbedToIcsUrl(g.CalendarUrl) ?? g.CalendarUrl;
                    if (string.IsNullOrWhiteSpace(calendarId) || !calendarId.Contains("@"))
                    {
                        var m = Regex.Match(g.CalendarUrl ?? string.Empty, "([\\w.%+-]+@group.calendar.google.com)", RegexOptions.IgnoreCase);
                        if (m.Success) calendarId = Uri.UnescapeDataString(m.Groups[1].Value);
                    }

                    if (string.IsNullOrWhiteSpace(calendarId))
                    {
                        results.Add(new ProbeResult(g.Name, string.Empty, false, "No calendar id found for gcal probe", null, null));
                        continue;
                    }

                    endpoint = !string.IsNullOrWhiteSpace(_apiBaseUrl)
                        ? $"{_apiBaseUrl}/api/calendar/gcal?id={Uri.EscapeDataString(calendarId)}"
                        : $"api/calendar/gcal?id={Uri.EscapeDataString(calendarId)}";

                    using var resp = await _http.GetAsync(endpoint);
                    var txt = await resp.Content.ReadAsStringAsync();
                    if (!resp.IsSuccessStatusCode)
                    {
                        results.Add(new ProbeResult(g.Name, endpoint, false, $"HTTP {(int)resp.StatusCode}", txt.Length > 1000 ? txt.Substring(0, 1000) : txt, null));
                        continue;
                    }

                    // parse JSON array count
                    int count = 0;
                    try
                    {
                        using var doc = JsonDocument.Parse(txt);
                        if (doc.RootElement.ValueKind == JsonValueKind.Array)
                        {
                            count = doc.RootElement.GetArrayLength();
                        }
                    }
                    catch { }

                    var snippet = txt.Length > 1000 ? txt.Substring(0, 1000) : txt;
                    results.Add(new ProbeResult(g.Name, endpoint, true, null, snippet, count));
                }
                catch (Exception ex)
                {
                    results.Add(new ProbeResult(g.Name, endpoint ?? string.Empty, false, ex.Message, null, null));
                }
            }
            return results;
        }

        public async Task<List<CalendarEvent>> GetUpcomingWedSatForGroupsAsync(IEnumerable<(string Name, string CalendarUrl)> groups)
        {
            var results = new List<CalendarEvent>();
            foreach (var g in groups)
            {
                if (string.IsNullOrWhiteSpace(g.CalendarUrl))
                    continue;

                try
                {
                    // Get all candidate events (from gcal or ICS), then pick the single next Wed or Sat
                    List<CalendarEvent>? events = null;
                    string? calendarId = null;
                    try { calendarId = ConvertEmbedToIcsUrl(g.CalendarUrl); } catch { calendarId = null; }
                    if (string.IsNullOrWhiteSpace(calendarId) || !calendarId.Contains("@"))
                    {
                        var m = Regex.Match(g.CalendarUrl ?? string.Empty, "([\\w.%+-]+@group.calendar.google.com)", RegexOptions.IgnoreCase);
                        if (m.Success) calendarId = Uri.UnescapeDataString(m.Groups[1].Value);
                    }

                    if (!string.IsNullOrWhiteSpace(calendarId))
                    {
                        var gcalEndpoint = !string.IsNullOrWhiteSpace(_apiBaseUrl)
                            ? $"{_apiBaseUrl}/api/calendar/gcal?id={Uri.EscapeDataString(calendarId)}"
                            : $"api/calendar/gcal?id={Uri.EscapeDataString(calendarId)}";
                        try
                        {
                            using var resp = await _http.GetAsync(gcalEndpoint);
                            if (resp.IsSuccessStatusCode)
                            {
                                var json = await resp.Content.ReadAsStringAsync();
                                try
                                {
                                    using var doc = JsonDocument.Parse(json);
                                    var list = new List<CalendarEvent>();
                                    if (doc.RootElement.ValueKind == JsonValueKind.Array)
                                    {
                                        foreach (var el in doc.RootElement.EnumerateArray())
                                        {
                                            try
                                            {
                                                var start = el.GetProperty("start").GetDateTime();
                                                var summary = el.TryGetProperty("summary", out var sEl) ? sEl.GetString() ?? string.Empty : string.Empty;
                                                var location = el.TryGetProperty("location", out var lEl) ? lEl.GetString() ?? string.Empty : string.Empty;
                                                var description = el.TryGetProperty("description", out var dEl) ? dEl.GetString() ?? string.Empty : string.Empty;
                                                var links = new List<string>();
                                                if (el.TryGetProperty("links", out var linksEl) && linksEl.ValueKind == JsonValueKind.Array)
                                                {
                                                    foreach (var linkEl in linksEl.EnumerateArray())
                                                    {
                                                        if (linkEl.ValueKind == JsonValueKind.String)
                                                        {
                                                            var u = linkEl.GetString(); if (!string.IsNullOrWhiteSpace(u) && !links.Contains(u)) links.Add(u);
                                                        }
                                                    }
                                                }
                                                list.Add(new CalendarEvent
                                                {
                                                    GroupName = g.Name,
                                                    Start = start.ToUniversalTime(),
                                                    Summary = summary,
                                                    Location = location,
                                                    OriginalDescription = !string.IsNullOrWhiteSpace(description) ? (description.Length > 500 ? description.Substring(0, 500) : description) : string.Empty,
                                                    RouteLinks = links
                                                });
                                            }
                                            catch { }
                                        }
                                    }
                                    if (list.Count > 0) events = list;
                                }
                                catch { /* invalid JSON -> fall back */ }
                            }
                        }
                        catch { /* ignore and fall back to ICS */ }
                    }

                    if (events == null)
                    {
                        // Use server-side proxy to fetch ICS (avoids CORS issues)
                        var probeSrc = g.CalendarUrl;
                        var proxyEndpoint = !string.IsNullOrWhiteSpace(_apiBaseUrl)
                            ? $"{_apiBaseUrl}/api/calendar/ics?src={Uri.EscapeDataString(probeSrc ?? string.Empty)}"
                            : $"api/calendar/ics?src={Uri.EscapeDataString(probeSrc ?? string.Empty)}";
                        var text = await _http.GetStringAsync(proxyEndpoint);
                        events = ParseIcs(text, g.Name);
                    }

                    if (events == null || events.Count == 0)
                    {
                        // no events for this group
                        continue;
                    }

                    // pick the single next event that is Wednesday or Saturday
                    var nowUtc = DateTime.UtcNow;
                    TimeZoneInfo ukZone;
                    try
                    {
                        if (OperatingSystem.IsWindows())
                            ukZone = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");
                        else
                            ukZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/London");
                    }
                    catch
                    {
                        ukZone = TimeZoneInfo.Utc;
                    }

                    var candidates = events
                        .Where(ev => ev.Start >= nowUtc)
                        .Select(ev => new { Ev = ev, EvUk = TimeZoneInfo.ConvertTimeFromUtc(ev.Start, ukZone) })
                        .Where(x => x.EvUk.DayOfWeek == DayOfWeek.Wednesday || x.EvUk.DayOfWeek == DayOfWeek.Saturday)
                        .OrderBy(x => x.Ev.Start)
                        .ToList();

                    if (candidates.Any())
                    {
                        // find the earliest calendar date among the matching Wednesday/Saturday events
                        var earliestDate = candidates.Min(x => x.EvUk.Date);
                        // include all events that fall on that same UK local date (allows multiple rides on the same next Wed/Sat)
                        var sameDay = candidates.Where(x => x.EvUk.Date == earliestDate).Select(x => x.Ev);
                        results.AddRange(sameDay);
                    }
                }
                catch
                {
                    // swallow individual group errors
                }
            }

            // sort by date
            return results.OrderBy(e => e.Start).ToList();
        }

        private string? ConvertEmbedToIcsUrl(string? embedUrl)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(embedUrl)) return null;
                    var m = Regex.Match(embedUrl, "[?&]src=([^&]+)", RegexOptions.IgnoreCase);
                if (!m.Success) return embedUrl;
                var src = Uri.UnescapeDataString(m.Groups[1].Value);
                return src;
            }
            catch
            {
                return null;
            }
        }

        private List<CalendarEvent> ParseIcs(string icsText, string groupName)
        {
            var events = new List<CalendarEvent>();
            var parts = Regex.Split(icsText, "BEGIN:VEVENT", RegexOptions.IgnoreCase);
            foreach (var part in parts.Skip(1))
            {
                try
                {
                    var block = "BEGIN:VEVENT" + part;
                    var dt = ExtractField(block, "DTSTART");
                    if (string.IsNullOrWhiteSpace(dt))
                        continue;

                    // dt is the value after the ':' (e.g. 20251224T100000 or 2025-12-24T10:00:00Z)
                    var dtValue = dt.Trim();
                    if (!TryParseIcsDate(dtValue, out var start))
                    {
                        // skip if we can't parse the date
                        continue;
                    }

                    var summary = ExtractField(block, "SUMMARY") ?? string.Empty;
                    var location = ExtractField(block, "LOCATION") ?? string.Empty;
                    var desc = ExtractField(block, "DESCRIPTION") ?? string.Empty;

                    // extract links from description
                    var links = new List<string>();
                    foreach (Match m in Regex.Matches(desc, "https?://[\\w\\-./?%&=+#:@()~]+", RegexOptions.IgnoreCase))
                    {
                        var u = m.Value.Trim();
                        if (!links.Contains(u)) links.Add(u);
                    }

                    // If location empty, try to find in description lines like 'Location:'
                    if (string.IsNullOrWhiteSpace(location))
                    {
                        var mloc = Regex.Match(desc, "Location:\\s*(.+)", RegexOptions.IgnoreCase);
                        if (mloc.Success) location = mloc.Groups[1].Value.Trim();
                    }

                    events.Add(new CalendarEvent
                    {
                        GroupName = groupName,
                        Start = EnsureUtcAssumeEuropeLondon(start),
                        Summary = summary,
                        Location = location,
                        OriginalDescription = !string.IsNullOrWhiteSpace(desc) ? (desc.Length > 500 ? desc.Substring(0, 500) : desc) : string.Empty,
                        RouteLinks = links
                    });
                }
                catch
                {
                    // ignore single event parse errors
                }
            }
            return events;
        }

        private DateTime EnsureUtcAssumeEuropeLondon(DateTime dt)
        {
            // If DateTime kind is Unspecified, treat as Europe/London and convert to UTC
            if (dt.Kind == DateTimeKind.Utc) return dt;
            if (dt.Kind == DateTimeKind.Local)
            {
                return dt.ToUniversalTime();
            }

            try
            {
                TimeZoneInfo ukZone;
                if (OperatingSystem.IsWindows())
                    ukZone = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");
                else
                    ukZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/London");

                var unspecified = DateTime.SpecifyKind(dt, DateTimeKind.Unspecified);
                var utc = TimeZoneInfo.ConvertTimeToUtc(unspecified, ukZone);
                return utc;
            }
            catch
            {
                return DateTime.SpecifyKind(dt, DateTimeKind.Utc);
            }
        }

        private bool TryParseIcsDate(string s, out DateTime result)
        {
            result = default;
            if (string.IsNullOrWhiteSpace(s)) return false;

            // Common ICS date/time formats
            var formats = new[] {
                "yyyyMMdd'T'HHmmss'Z'",
                "yyyyMMdd'T'HHmmss",
                "yyyyMMdd'T'HHmm'Z'",
                "yyyyMMdd'T'HHmm",
                "yyyyMMdd",
                "yyyy-MM-dd'T'HH:mm:ss'Z'",
                "yyyy-MM-dd'T'HH:mm:ss",
                "yyyy-MM-dd HH:mm:ss"
            };

            // Trim and remove trailing Z for some patterns handled by formats above
            var trimmed = s.Trim();

            // If value contains a timezone offset like +00:00, try parse directly
            if (DateTimeOffset.TryParse(trimmed, out var dto))
            {
                result = dto.UtcDateTime;
                return true;
            }

            foreach (var f in formats)
            {
                if (DateTime.TryParseExact(trimmed, f, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal, out var d))
                {
                    result = d;
                    return true;
                }
                if (DateTime.TryParseExact(trimmed, f, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out d))
                {
                    result = d;
                    return true;
                }
            }

            // fallback to generic parse
            if (DateTime.TryParse(trimmed, out var parsed))
            {
                result = parsed;
                return true;
            }

            return false;
        }

        private string? ExtractField(string block, string field)
        {
            // match lines like FIELD;...:value or FIELD:value
            var m = Regex.Match(block, $"^{field}[^:\r\n]*:(.*)$", RegexOptions.Multiline | RegexOptions.IgnoreCase);
            if (!m.Success) return null;
            var val = m.Groups[1].Value;
            // unfold lines (ICS line folding: lines starting with space are continuation)
            val = Regex.Replace(val, "\\r?\\n[ \\t]", " ");
            return val.Trim();
        }
    }
}
