using Microsoft.EntityFrameworkCore;
using web.Data;
using web.Models;

namespace web.Services;

public class AuthSessionService
{
    private readonly ApplicationDbContext _db;

    public AuthSessionService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<(string AccessToken, string RefreshToken)> CreateSessionAsync(User user)
    {
        var access = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
        var refresh = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");

        var token = new AuthToken
        {
            UserId = user.Id,
            AccessToken = access,
            RefreshToken = refresh,
            AccessExpiresAt = DateTime.UtcNow.AddDays(1),
            RefreshExpiresAt = DateTime.UtcNow.AddDays(30)
        };

        _db.AuthTokens.Add(token);
        await _db.SaveChangesAsync();

        return (access, refresh);
    }

    public async Task<(User? User, AuthToken? Token)> ValidateAccessAsync(string? bearer)
    {
        if (string.IsNullOrWhiteSpace(bearer)) return (null, null);

        var value = bearer.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? bearer[7..].Trim()
            : bearer.Trim();

        if (string.IsNullOrWhiteSpace(value)) return (null, null);

        var token = await _db.AuthTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.AccessToken == value && t.AccessExpiresAt > DateTime.UtcNow);

        return (token?.User, token);
    }

    public async Task<(string AccessToken, string RefreshToken)?> RefreshAsync(string refreshToken)
    {
        var token = await _db.AuthTokens
            .FirstOrDefaultAsync(t => t.RefreshToken == refreshToken && t.RefreshExpiresAt > DateTime.UtcNow);

        if (token == null) return null;

        token.AccessToken = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
        token.RefreshToken = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
        token.AccessExpiresAt = DateTime.UtcNow.AddDays(1);
        token.RefreshExpiresAt = DateTime.UtcNow.AddDays(30);

        await _db.SaveChangesAsync();
        return (token.AccessToken, token.RefreshToken);
    }
}
