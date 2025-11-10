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
            // Knowledge base summary
            var knowledgeBase = @"San Fairy Ann Cycling Club (SFACC) is Kent’s largest and friendliest cycling club. Membership benefits include access to club rides, events, discounts, curated routes, and more. To join, visit: https://www.sanfairyanncc.co.uk/join-the-club

SFACC has a club account with RideWithGPS, providing curated cycling routes for all riders in Kent. Members can access special features and routes via the club’s RideWithGPS organization page: https://ridewithgps.com/organizations/633-san-fairy-ann-cycling-club

Strava is a social network for athletes, popular for tracking cycling and running activities. To use Strava: Download the Strava app or visit https://www.strava.com/ and join the SFACC club on Strava to connect with other members.

For club events, rides, membership, kit, and more, visit the club website: https://www.sanfairyanncc.co.uk/";

            // Compose the full prompt (no welcome message)
            var prompt = $"{knowledgeBase}\n\nUser: {message}";
            var completion = await _chatClient.CompleteChatAsync(prompt);
            return completion.Value.Content[0].Text;
        }
    }
}
