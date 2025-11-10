using Microsoft.AspNetCore.Mvc;
using SFA_WebAPI.Services;

namespace SFA_WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]

    public class BotController : ControllerBase
    {
        private readonly SFA_WebAPI.Services.OpenAIBotService _botService;

        public BotController(SFA_WebAPI.Services.OpenAIBotService botService)
        {
            _botService = botService;
        }

        [HttpPost("chat")]
        public async Task<ActionResult<ChatResponse>> Chat([FromBody] ChatRequest request)
        {
            try
            {
                var reply = await _botService.GetBotReplyAsync(request.Message);
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
    }

    public class ChatResponse
    {
        public string Reply { get; set; } = string.Empty;
    }
}
