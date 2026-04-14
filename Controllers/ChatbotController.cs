using Microsoft.AspNetCore.Mvc;
using web.Models;
using web.Services;

namespace web.Controllers;

[ApiController]
[Route("api/chat")]
public class ChatbotController : ControllerBase
{
    private readonly ChatbotService _chatbotService;
    private readonly ILogger<ChatbotController> _logger;

    public ChatbotController(ChatbotService chatbotService, ILogger<ChatbotController> logger)
    {
        _chatbotService = chatbotService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IResult> Ask([FromBody] ChatRequest body)
    {
        if (string.IsNullOrWhiteSpace(body.Message))
        {
            return Results.Ok(new { reply = "Ban hay nhap noi dung can ho tro." });
        }

        try
        {
            var reply = await _chatbotService.ChatAsync(body, HttpContext.RequestAborted);
            return Results.Ok(new { reply });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chatbot processing failed");
            return Results.Ok(new { reply = "Da co loi khi goi AI. Vui long thu lai sau." });
        }
    }
}
