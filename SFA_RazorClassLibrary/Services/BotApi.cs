using System.Net.Http;

// Intentionally in the global namespace to avoid updating existing Razor @inject usages.

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
