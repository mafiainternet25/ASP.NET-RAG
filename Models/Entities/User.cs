using System.ComponentModel.DataAnnotations;

namespace web.Models;

public class User
{
    public int Id { get; set; }

    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [MaxLength(120)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(120)]
    public string PasswordHash { get; set; } = string.Empty;

    [MaxLength(120)]
    public string? FullName { get; set; }

    [MaxLength(30)]
    public string? Phone { get; set; }

    [MaxLength(20)]
    public string Role { get; set; } = "USER";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
