using Microsoft.Extensions.Configuration;
using OpenAI.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace SFA_WebAPI.Services
{
    public class OpenAIBotService
    {
        private readonly ChatClient _chatClient;
        private readonly HttpClient _httpClient;
        private static readonly SemaphoreSlim WebsiteSnapshotLock = new(1, 1);
        private static string? _websiteSnapshot;
        private static DateTimeOffset _websiteSnapshotFetchedAtUtc;

        private const bool AlwaysRefreshWebsiteSnapshot = true;
        private static readonly TimeSpan WebsiteSnapshotTtl = TimeSpan.FromMinutes(30);
        private const int DefaultFetchCharLimit = 2000;
        private const int DeterministicSectionCharLimit = 3000;

        private static readonly (string Topic, string Url)[] DeterministicClubSources =
        {
            ("Club History", "https://www.sanfairyanncc.co.uk/club-history"),
            ("Past Magazine and Newsletters", "https://www.sanfairyanncc.co.uk/magazine"),
            ("Minutes", "https://www.sanfairyanncc.co.uk/club-updates/agm-report-2024?rq=minutes"),
            ("Ride Etiquette and Rules", "https://www.sanfairyanncc.co.uk/group-rules-etiquette")
        };
// I need to figure out where these are located maybe Duncan can help
// ("Records and Achievements", "https://www.sanfairyanncc.co.uk/club-records"),
// ("Articles and Constitution", "https://www.sanfairyanncc.co.uk/memberspage"),
// Note the magazine is just links to pdf need to figure out how to get this working
        public OpenAIBotService(IConfiguration configuration, HttpClient httpClient)
        {
            var apiKey = configuration["OpenAI:ApiKey"];
            var model = "gpt-5.6-terra";
            // var model = "gpt-4o-mini";
            _chatClient = new ChatClient(model, apiKey);
            _httpClient = httpClient;
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
        }

        public static List<string> GetDefaultSampleQuestions()
        {
            return new List<string>
            {
                "What is the club history?",
                "Where am I going on Saturday?",
                "Where is the next B ride from on Saturday?",
                "How much is membership?",
                "What ride groups are there?",
                "Do I need insurance to ride?",
                "How do I try a club ride?",
                "Where are the next club events?",
                "How do I contact the club?",
                "What is the latest club newsletter?"
            };
        }

        public async Task<List<string>> GetSampleQuestionsAsync()
        {
            var questions = GetDefaultSampleQuestions();

            var knowledgeBasePath = "knowledgebase.txt";
            try
            {
                var content = await System.IO.File.ReadAllTextAsync(knowledgeBasePath);
                if (!string.IsNullOrWhiteSpace(content))
                {
                    var extracted = System.Text.RegularExpressions.Regex.Matches(
                        content,
                        @"(?im)^\s*[-*]?\s*\*?\s*(?:Sample question|Suggested question|Question):\s*(.+)$")
                        .Select(m => m.Groups[1].Value.Trim())
                        .Where(q => !string.IsNullOrWhiteSpace(q))
                        .ToList();

                    if (extracted.Count > 0)
                    {
                        questions = extracted.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                    }
                }
            }
            catch
            {
                // Fall back to the default sample questions in case the knowledge file is unavailable.
            }

            return questions.Take(10).ToList();
        }

        /// <summary>
        /// Fetches and extracts clean text content from a webpage URL.
        /// Removes HTML tags, scripts, styles, and returns readable text.
        /// </summary>
        private async Task<string> FetchWebpageAsync(string url, int maxChars = DefaultFetchCharLimit)
        {
            try
            {
                // Validate and normalize URL
                if (!url.StartsWith("http://") && !url.StartsWith("https://"))
                {
                    url = "https://" + url;
                }

                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36 Edg/126.0.0.0");
                request.Headers.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8");
                request.Headers.AcceptLanguage.ParseAdd("en-GB,en;q=0.9");
                request.Headers.Referrer = new Uri("https://www.sanfairyanncc.co.uk/");

                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    return $"Could not fetch webpage: {(int)response.StatusCode} {response.ReasonPhrase}";
                }

                var htmlContent = await response.Content.ReadAsStringAsync();
                var doc = new HtmlDocument();
                doc.LoadHtml(htmlContent);

                // Remove script and style tags
                foreach (var node in doc.DocumentNode.SelectNodes("//script | //style")?.ToList() ?? new List<HtmlNode>())
                {
                    node.Remove();
                }

                // Get all text and normalize whitespace
                var text = doc.DocumentNode.InnerText;
                text = Regex.Replace(text, @"\s+", " ");
                text = Regex.Replace(text, @"^\s+|\s+$", "");

                // Limit to first 2000 characters to avoid token overflow
                if (text.Length > maxChars)
                {
                    text = text.Substring(0, maxChars) + "\n[... content truncated ...]";
                }

                return text;
            }
            catch (Exception ex)
            {
                return $"Could not fetch webpage: {ex.Message}";
            }
        }

        /// <summary>
        /// Extracts URLs from user message using regex.
        /// </summary>
        private List<string> ExtractUrlsFromMessage(string message)
        {
            var urlPattern = @"https?://[^\s]+";
            var matches = Regex.Matches(message, urlPattern);
            return matches.Cast<Match>().Select(m => m.Value).ToList();
        }

        private async Task<string> GetDeterministicWebsiteSnapshotAsync()
        {
            var now = DateTimeOffset.UtcNow;
            if (!AlwaysRefreshWebsiteSnapshot &&
                !string.IsNullOrWhiteSpace(_websiteSnapshot) &&
                now - _websiteSnapshotFetchedAtUtc <= WebsiteSnapshotTtl)
            {
                return _websiteSnapshot;
            }

            await WebsiteSnapshotLock.WaitAsync();
            try
            {
                now = DateTimeOffset.UtcNow;
                if (!AlwaysRefreshWebsiteSnapshot &&
                    !string.IsNullOrWhiteSpace(_websiteSnapshot) &&
                    now - _websiteSnapshotFetchedAtUtc <= WebsiteSnapshotTtl)
                {
                    return _websiteSnapshot;
                }

                var builder = new StringBuilder();
                builder.AppendLine("Deterministic SFACC website snapshot (authoritative source content):");
                var successfulFetchCount = 0;

                foreach (var source in DeterministicClubSources)
                {
                    var content = await FetchWebpageAsync(source.Url, DeterministicSectionCharLimit);
                    var isError = content.StartsWith("Could not fetch webpage:", StringComparison.OrdinalIgnoreCase);

                    builder.AppendLine();
                    builder.AppendLine($"[{source.Topic}]");
                    builder.AppendLine($"Source: {source.Url}");
                    if (isError)
                    {
                        builder.AppendLine($"Status: unavailable ({content})");
                    }
                    else
                    {
                        successfulFetchCount++;
                        builder.AppendLine("Status: fresh");
                        builder.AppendLine(content);
                    }
                }

                builder.AppendLine();
                builder.AppendLine($"SnapshotFetchedAtUtc: {DateTimeOffset.UtcNow:O}");

                if (successfulFetchCount == 0 && !string.IsNullOrWhiteSpace(_websiteSnapshot))
                {
                    return _websiteSnapshot + $"\nSnapshotStatus: stale-fallback-used at {DateTimeOffset.UtcNow:O}";
                }

                _websiteSnapshot = builder.ToString();
                _websiteSnapshotFetchedAtUtc = DateTimeOffset.UtcNow;
                return _websiteSnapshot;
            }
            finally
            {
                WebsiteSnapshotLock.Release();
            }
        }

        public async Task<string> GetBotReplyAsync(string message)
        {
            // Read knowledge base from external file
            var knowledgeBasePath = "knowledgebase.txt";
            string knowledgeBase;
            try
            {
                knowledgeBase = await System.IO.File.ReadAllTextAsync(knowledgeBasePath);
            }
            catch
            {
                knowledgeBase = "Knowledge base file not found.";
            }

            var deterministicSnapshot = await GetDeterministicWebsiteSnapshotAsync();

            // Extract URLs from message and fetch content if any are present
            var urls = ExtractUrlsFromMessage(message);
            string fetchedContent = "";
            if (urls.Count > 0)
            {
                foreach (var url in urls.Take(2)) // Limit to first 2 URLs to save tokens
                {
                    var content = await FetchWebpageAsync(url);
                    fetchedContent += $"\n\nContent from {url}:\n{content}";
                }
            }

            // Compose the full prompt (no welcome message)
            var prompt = $"{knowledgeBase}\n\n{deterministicSnapshot}{fetchedContent}\n\nUser: {message}";
            var completion = await _chatClient.CompleteChatAsync(prompt);
            var reply = completion.Value.Content[0].Text;

            // Remove trailing punctuation from URLs (e.g., ., ,, ;, !, ?)
            reply = Regex.Replace(
                reply,
                @"(https?://[\w\-./?%&=+#]+)([.,;!?])(?=\s|$)",
                "$1"
            );

            return reply;
        }

            // Test helper methods - expose private methods for unit testing
            public async Task<string> FetchWebpageAsyncPublic(string url)
            {
                return await FetchWebpageAsync(url);
            }

            public List<string> ExtractUrlsFromMessagePublic(string message)
            {
                return ExtractUrlsFromMessage(message);
            }
    }
}
