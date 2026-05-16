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

    [HttpPost("ingest")]
    public async Task<IResult> Ingest()
    {
        try
        {
            _logger.LogInformation("Manual ingest request triggered");
            var success = await _chatbotService.IngestAsync(HttpContext.RequestAborted);
            if (success)
            {
                return Results.Ok(new { status = "ok", message = "RAG data updated successfully" });
            }
            else
            {
                return Results.BadRequest(new { status = "error", message = "Failed to update RAG data" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ingest operation failed");
            return Results.Json(new { status = "error", message = ex.Message }, statusCode: 500);
        }
    }
}
