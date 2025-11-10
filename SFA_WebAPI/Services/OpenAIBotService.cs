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
            // Knowledge base summary with only actual, working links
            var knowledgeBase = @"San Fairy Ann Cycling Club (SFACC) is Kent’s largest and friendliest cycling club. Membership benefits include access to club rides, events, discounts, curated routes, and more.

When asked about membership rates, the AI will read the club’s Join page (https://www.sanfairyanncc.co.uk/join-the-club) and provide the latest information directly from the website, ensuring answers are always up to date.

Useful links:
- Home: https://www.sanfairyanncc.co.uk/
- Ride With Us: https://www.sanfairyanncc.co.uk/ridewithus
- Events: https://www.sanfairyanncc.co.uk/events
- Club Shop: https://www.sanfairyanncc.co.uk/store
- Membership Benefits: https://www.sanfairyanncc.co.uk/membership-benefits
- Try It Out Rides: https://www.sanfairyanncc.co.uk/tryitout
- Club Kit: https://www.sanfairyanncc.co.uk/kit-form
- Club History: https://www.sanfairyanncc.co.uk/club-history
- Group Rides: https://www.sanfairyanncc.co.uk/grouprides
- Magazine/Newsletter: https://www.sanfairyanncc.co.uk/magazine
- Members Page: https://www.sanfairyanncc.co.uk/memberspage
- Safeguarding: https://www.sanfairyanncc.co.uk/safeguarding
- Coaching: https://www.sanfairyanncc.co.uk/coaching
- Curated Routes: https://www.sanfairyanncc.co.uk/curated-routes
- Road Safety Training: https://www.sanfairyanncc.co.uk/cycle-training
- Audax & Sportive: https://www.sanfairyanncc.co.uk/audax-sportive
- Club Updates: https://www.sanfairyanncc.co.uk/club-updates
- Who We Are: https://www.sanfairyanncc.co.uk/whoweare
- Contact Us: https://www.sanfairyanncc.co.uk/whoweare
";

            // Compose the full prompt (no welcome message)
            var prompt = $"{knowledgeBase}\n\nUser: {message}";
            var completion = await _chatClient.CompleteChatAsync(prompt);
            return completion.Value.Content[0].Text;
        }
    }
}
