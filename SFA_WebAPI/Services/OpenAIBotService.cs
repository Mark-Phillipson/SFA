using Microsoft.Extensions.Configuration;
using OpenAI.Chat;
using System.Threading.Tasks;

namespace SFA_WebAPI.Services
{
    public class OpenAIBotService
    {
        private readonly ChatClient _chatClient;

        public OpenAIBotService(IConfiguration configuration)
        {
            var apiKey = configuration["OpenAI:ApiKey"];
            var model = "gpt-3.5-turbo";
            _chatClient = new ChatClient(model, apiKey);
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
