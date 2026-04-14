using System.ComponentModel.DataAnnotations;

namespace web.Models;

public class AuthToken
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }

    [MaxLength(200)]
    public string AccessToken { get; set; } = string.Empty;

    [MaxLength(200)]
    public string RefreshToken { get; set; } = string.Empty;

    public DateTime AccessExpiresAt { get; set; }
    public DateTime RefreshExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
