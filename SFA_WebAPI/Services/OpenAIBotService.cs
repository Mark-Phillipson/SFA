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
    var completion = await _chatClient.CompleteChatAsync(message);
    return completion.Value.Content[0].Text;
        }
    }
}
