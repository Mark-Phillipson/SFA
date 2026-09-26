using Microsoft.Extensions.Configuration;
using OpenAI.Chat;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SFA_WebAPI.Services
{
    public class OpenAIBotService
    {
        private readonly ChatClient _chatClient;

        public OpenAIBotService(IConfiguration configuration)
        {
            var apiKey = configuration["OpenAI:ApiKey"];
            var model = "gpt-4.1-mini";
            // var model = "gpt-4o-mini";
            _chatClient = new ChatClient(model, apiKey);
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

            // Compose the full prompt (no welcome message)
            var prompt = $"{knowledgeBase}\n\nUser: {message}";
            var completion = await _chatClient.CompleteChatAsync(prompt);
            var reply = completion.Value.Content[0].Text;

            // Remove trailing punctuation from URLs (e.g., ., ,, ;, !, ?)
            reply = System.Text.RegularExpressions.Regex.Replace(
                reply,
                @"(https?://[\w\-./?%&=+#]+)([.,;!?])(?=\s|$)",
                "$1"
            );

            return reply;
        }
    }
}
