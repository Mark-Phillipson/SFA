using Microsoft.AspNetCore.Mvc;
using SFA_WebAPI.Services;

namespace SFA_WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]

    public class BotController : ControllerBase
    {
        private readonly SFA_WebAPI.Services.OpenAIBotService _botService;
        private readonly IBotQuestionLogService _questionLogService;

        public BotController(
            SFA_WebAPI.Services.OpenAIBotService botService,
            IBotQuestionLogService questionLogService)
        {
            _botService = botService;
            _questionLogService = questionLogService;
        }

        [HttpGet("sample-questions")]
        public async Task<ActionResult<List<string>>> GetSampleQuestions()
        {
            try
            {
                var questions = await _botService.GetSampleQuestionsAsync();
                return Ok(questions);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error in BotController.GetSampleQuestions: {ex.Message}\n{ex.StackTrace}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("chat")]
        public async Task<ActionResult<ChatResponse>> Chat([FromBody] ChatRequest request, CancellationToken cancellationToken)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Message))
                {
                    return BadRequest(new ChatResponse { Reply = "Message must not be empty." });
                }

                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                var userAgent = HttpContext.Request.Headers.UserAgent.ToString();
                var reply = await _botService.GetBotReplyAsync(request.Message, request.History);
                await _questionLogService.LogQuestionAsync(
                    request.Message,
                    reply,
                    ipAddress,
                    userAgent,
                    cancellationToken);
                return Ok(new ChatResponse { Reply = reply });
            }
            catch (Exception ex)
            {
                // Log the error (can be replaced with a proper logging framework)
                Console.Error.WriteLine($"Error in BotController.Chat: {ex.Message}\n{ex.StackTrace}");
                return StatusCode(500, new ChatResponse { Reply = $"Internal server error: {ex.Message}" });
            }
        }
    }

    public class ChatRequest
    {
        public string Message { get; set; } = string.Empty;
        public List<ChatHistoryItem>? History { get; set; }
    }

    public class ChatHistoryItem
    {
        public string Role { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
    }

    public class ChatResponse
    {
        public string Reply { get; set; } = string.Empty;
    }
}
