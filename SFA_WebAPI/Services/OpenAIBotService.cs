using Microsoft.Extensions.Configuration;
using OpenAI.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace SFA_WebAPI.Services
{
    public class OpenAIBotService
    {
        private readonly ChatClient _chatClient;
        private readonly HttpClient _httpClient;

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
        private async Task<string> FetchWebpageAsync(string url)
        {
            try
            {
                // Validate and normalize URL
                if (!url.StartsWith("http://") && !url.StartsWith("https://"))
                {
                    url = "https://" + url;
                }

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

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
                if (text.Length > 2000)
                {
                    text = text.Substring(0, 2000) + "\n[... content truncated ...]";
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
            var prompt = $"{knowledgeBase}{fetchedContent}\n\nUser: {message}";
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
