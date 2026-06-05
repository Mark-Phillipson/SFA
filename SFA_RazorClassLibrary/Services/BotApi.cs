using System.Net.Http;

namespace SFA_RazorClassLibrary.Services
{
    public class BotApiHttpClient
    {
        public HttpClient Client { get; }

        public BotApiHttpClient(HttpClient client)
        {
            Client = client;
        }
    }

    public class BotApiConfig
    {
        public string BotApiUrl { get; set; } = string.Empty;
    }
}
