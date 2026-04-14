namespace web.Services;

public class ServiceResult
{
    public int StatusCode { get; }
    public object? Data { get; }
    public string? Message { get; }
    public string MessageField { get; }

    private ServiceResult(int statusCode, object? data = null, string? message = null, string messageField = "error")
    {
        StatusCode = statusCode;
        Data = data;
        Message = message;
        MessageField = messageField;
    }

    public static ServiceResult Ok(object data) => new(StatusCodes.Status200OK, data);
    public static ServiceResult Unauthorized() => new(StatusCodes.Status401Unauthorized);
    public static ServiceResult Forbidden() => new(StatusCodes.Status403Forbidden);
    public static ServiceResult BadRequest(string message) => new(StatusCodes.Status400BadRequest, null, message, "error");
    public static ServiceResult NotFound(string message, string field = "message") => new(StatusCodes.Status404NotFound, null, message, field);
}
