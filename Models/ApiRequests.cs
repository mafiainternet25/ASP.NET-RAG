namespace web.Models;

public record RegisterRequest(string Username, string Email, string Password, string? FullName, string? Phone);
public record LoginRequest(string Username, string Password);
public record RefreshRequest(string RefreshToken);
public record ReviewCreateRequest(int MovieId, int Rating, string? Comment);
public record UpdateProfileRequest(string? FullName, string? Email, string? Phone, string? CurrentPassword, string? NewPassword);
public record CreateBookingSnackRequest(int SnackId, int Quantity);
public record CreateBookingRequest(int ShowtimeId, int[] SeatIds, CreateBookingSnackRequest[]? Snacks, string? PromotionCode);
public record ChatRequest(string Message, List<ChatHistoryItem>? History);
public record ChatHistoryItem(string Role, string Content);
