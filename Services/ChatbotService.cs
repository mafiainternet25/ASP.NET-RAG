using System.Text.Json;
using web.Models;

namespace web.Services;

public class ChatbotService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ChatbotService> _logger;
    private const string RagServiceUrl = "http://localhost:8001";
    public ChatbotService(
        IHttpClientFactory httpClientFactory,
        ILogger<ChatbotService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Call Python RAG service on port 8001 to generate chatbot replies
    /// </summary>
    public async Task<string> ChatAsync(ChatRequest? request, CancellationToken cancellationToken = default)
    {
        var userMessage = request?.Message?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(userMessage))
        {
            return "Ban hay nhap noi dung can ho tro.";
        }

        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(30);

            // Convert ChatHistoryItem to Python-compatible format
            var historyForPython = request?.History?.Select(h => new
            {
                role = h.Role,
                content = h.Content
            }).ToList() ?? [];

            var payload = new
            {
                message = userMessage,
                history = historyForPython
            };

            var jsonContent = new StringContent(
                JsonSerializer.Serialize(payload, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }),
                System.Text.Encoding.UTF8,
                "application/json");

            var response = await client.PostAsync(
                $"{RagServiceUrl}/chat",
                jsonContent,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"RAG service returned {response.StatusCode}");
                return "Xin loi, dang co van de voi dich vu AI. Vui long thu lai sau.";
            }

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(responseJson);
            var reply = doc.RootElement.GetProperty("reply").GetString();

            return reply ?? "Khong nhan duoc cau tra loi tu dich vu.";
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError($"HTTP error calling RAG service: {ex.Message}");
            return "Khong the ket noi den dich vu AI. Kiem tra xem Python service co dang chay tren port 8001 khong?";
        }
        catch (TaskCanceledException)
        {
            _logger.LogError("Timeout calling RAG service");
            return "Dich vu AI phan hoi qua lau. Vui long thu lai.";
        }
        catch (Exception ex)
        {
            _logger.LogError($"Unexpected error calling RAG service: {ex.Message}");
            return "Loi bat ngo: " + ex.Message;
        }
    }
}
