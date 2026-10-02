using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using SFA_WebAPI.Controllers;
using SFA_WebAPI.Models;
using Xunit;
using SFA_WebAPI.Services;

namespace SFA_PWA.Tests;

public class OpenAIBotServiceTests
{
    private readonly Mock<IConfiguration> _mockConfig;
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly Mock<ILinksCatalogService> _mockLinksCatalogService;
    private readonly Mock<ILogger<OpenAIBotService>> _mockLogger;
    private readonly HttpClient _httpClient;

    public OpenAIBotServiceTests()
    {
        _mockConfig = new Mock<IConfiguration>();
        _mockConfig.Setup(x => x["OpenAI:ApiKey"]).Returns("test-api-key");
        
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object);

        _mockLinksCatalogService = new Mock<ILinksCatalogService>();
        _mockLinksCatalogService
            .Setup(x => x.GetSnapshotAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LinksCatalogSnapshot(
                new List<LinkItem>(),
                "\"test-etag\"",
                DateTimeOffset.UtcNow));

        _mockLogger = new Mock<ILogger<OpenAIBotService>>();
    }

    private OpenAIBotService CreateService()
    {
        return new OpenAIBotService(
            _mockConfig.Object,
            _httpClient,
            _mockLinksCatalogService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public void ExtractUrlsFromMessage_WithSingleUrl_ReturnsUrl()
    {
        // Arrange
        var message = "Can you tell me about https://www.sanfairyanncc.co.uk/club-history";
        var service = CreateService();

        // Act
        var urls = service.ExtractUrlsFromMessagePublic(message);

        // Assert
        Assert.Single(urls);
        Assert.Contains("https://www.sanfairyanncc.co.uk/club-history", urls);
    }

    [Fact]
    public void ExtractUrlsFromMessage_WithMultipleUrls_ReturnsAllUrls()
    {
        // Arrange
        var message = "Check https://www.example.com and also http://www.example.org please";
        var service = CreateService();

        // Act
        var urls = service.ExtractUrlsFromMessagePublic(message);

        // Assert
        Assert.Equal(2, urls.Count);
        Assert.Contains("https://www.example.com", urls);
        Assert.Contains("http://www.example.org", urls);
    }

    [Fact]
    public void ExtractUrlsFromMessage_WithNoUrl_ReturnsEmptyList()
    {
        // Arrange
        var message = "Can you tell me about the club history?";
        var service = CreateService();

        // Act
        var urls = service.ExtractUrlsFromMessagePublic(message);

        // Assert
        Assert.Empty(urls);
    }

    [Fact]
    public void ExtractUrlsFromMessage_WithUrlWithoutProtocol_DoesNotMatch()
    {
        // Arrange
        var message = "Visit www.example.com for more info";
        var service = CreateService();

        // Act
        var urls = service.ExtractUrlsFromMessagePublic(message);

        // Assert
        Assert.Empty(urls);
    }

    [Fact]
    public void ExtractUrlsFromMessage_WithHttpsUrl_ReturnsUrl()
    {
        // Arrange
        var message = "Please visit https://example.com/page?param=value&other=123";
        var service = CreateService();

        // Act
        var urls = service.ExtractUrlsFromMessagePublic(message);

        // Assert
        Assert.Single(urls);
        Assert.Contains("https://example.com/page?param=value&other=123", urls);
    }

    [Fact]
    public async Task FetchWebpageAsync_WithValidUrl_ReturnsTruncatedContent()
    {
        // Arrange
        var htmlContent = @"
            <!DOCTYPE html>
            <html>
            <head>
                <title>Test Page</title>
                <script>alert('This should be removed');</script>
            </head>
            <body>
                <h1>Welcome to Test Page</h1>
                <p>This is some important content that should be extracted.</p>
                <style>.hidden { display: none; }</style>
                <p>More content here.</p>
            </body>
            </html>";

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(htmlContent)
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        var service = CreateService();

        // Act
        var content = await service.FetchWebpageAsyncPublic("https://example.com");

        // Assert
        Assert.NotNull(content);
        Assert.NotEmpty(content);
        Assert.DoesNotContain("script", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("style", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Welcome to Test Page", content);
        Assert.Contains("important content", content);
    }

    [Fact]
    public async Task FetchWebpageAsync_WithInvalidUrl_ReturnsErrorMessage()
    {
        // Arrange
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection failed"));

        var service = CreateService();

        // Act
        var content = await service.FetchWebpageAsyncPublic("https://invalid-domain-12345.com");

        // Assert
        Assert.NotNull(content);
            Assert.Contains("Could not fetch webpage", content);
    }

    [Fact]
    public async Task FetchWebpageAsync_WithLongContent_TruncatesTo2000Characters()
    {
        // Arrange
        var longContent = new string('A', 5000);
        var htmlContent = $"<html><body>{longContent}</body></html>";

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(htmlContent)
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        var service = CreateService();

        // Act
        var content = await service.FetchWebpageAsyncPublic("https://example.com");

        // Assert
        Assert.NotNull(content);
        Assert.True(content.Length <= 2100, $"Content length {content.Length} exceeds 2100 chars (with truncation marker)");
    }

    [Fact]
    public async Task FetchWebpageAsync_NormalizesWhitespace()
    {
        // Arrange
        var htmlContent = @"
            <html>
            <body>
                <p>This    has     multiple     spaces</p>
                <p>And
                    line
                    breaks</p>
            </body>
            </html>";

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(htmlContent)
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        var service = CreateService();

        // Act
        var content = await service.FetchWebpageAsyncPublic("https://example.com");

        // Assert
        Assert.NotNull(content);
        Assert.DoesNotContain("    ", content); // Multiple spaces should be normalized
        Assert.Contains("This has multiple spaces", content);
        Assert.Contains("And line breaks", content);
    }

    [Fact]
    public async Task GetRelevantDocumentContentForMessage_WithNewsletterMonth_ReturnsPdfText()
    {
        // Arrange
        var service = CreateService();

        // Act
        var content = await service.GetRelevantDocumentContentForMessagePublic("Can you summarise the December 2025 newsletter?");

        // Assert
        Assert.NotNull(content);
        Assert.Contains("December 2025", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("San Fairy Ann", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetRelevantDocumentContentForMessage_WithLatestNewsletter_ReturnsPdfText()
    {
        // Arrange
        var service = CreateService();

        // Act
        var content = await service.GetRelevantDocumentContentForMessagePublic("what was in the latest newsletter?");

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(content));
        Assert.Contains("Document: San Fairy Ann Cycling Club Newsletter", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeterministicNewsletterLinkReply_WithFollowUpQuestion_ReturnsNewsletterAndArchiveLinks()
    {
        // Arrange
        _mockLinksCatalogService
            .Setup(x => x.GetSnapshotAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LinksCatalogSnapshot(
                new List<LinkItem>
                {
                    new() { Description = "September 2026 Newsletter", Url = "https://sway.cloud.microsoft/example-september", Category = "Newsletter" },
                    new() { Description = "August 2026 Newsletter", Url = "https://sway.cloud.microsoft/example-august", Category = "Newsletter" },
                    new() { Description = "Magazine / Newsletter", Url = "https://www.sanfairyanncc.co.uk/magazine", Category = "Club Info" }
                },
                "\"test-etag\"",
                DateTimeOffset.UtcNow));

        var service = CreateService();
        var history = new List<ChatHistoryItem>
        {
            new() { Role = "user", Text = "What's in the latest newsletter?" },
            new() { Role = "assistant", Text = "The latest newsletter is the September 2026 edition." }
        };

        // Act
        var reply = await service.TryGetDeterministicNewsletterLinkReplyPublic("and where can I find it?", history);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(reply));
        Assert.Contains("https://sway.cloud.microsoft/example-september", reply, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("https://www.sanfairyanncc.co.uk/magazine", reply, StringComparison.OrdinalIgnoreCase);
    }
}
