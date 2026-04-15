using Microsoft.EntityFrameworkCore;
using web.Data;
using web.Models;
using web.Security;

namespace web.Services;

public class AuthSessionService
{
    private readonly ApplicationDbContext _db;
    private readonly JwtUtil _jwtUtil;

    public AuthSessionService(ApplicationDbContext db, JwtUtil jwtUtil)
    {
        _db = db;
        _jwtUtil = jwtUtil;
    }

    public async Task<(string AccessToken, string RefreshToken)> CreateSessionAsync(User user)
    {
        var access = _jwtUtil.GenerateAccessToken(user.Username, user.Role);
        var refresh = _jwtUtil.GenerateRefreshToken(user.Username);

        return (access, refresh);
    }

    public async Task<User?> ValidateAccessAsync(string? bearer)
    {
        if (string.IsNullOrWhiteSpace(bearer)) return null;

        var value = bearer.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? bearer[7..].Trim()
            : bearer.Trim();

        if (string.IsNullOrWhiteSpace(value)) return null;

        var (username, role) = _jwtUtil.ValidateAccessToken(value);
        if (string.IsNullOrWhiteSpace(username)) return null;

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username);
        return user;
    }

    public async Task<(string AccessToken, string RefreshToken)?> RefreshAsync(string refreshToken)
    {
        var username = _jwtUtil.ValidateRefreshToken(refreshToken);
        if (string.IsNullOrWhiteSpace(username)) return null;

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null) return null;

        var access = _jwtUtil.GenerateAccessToken(user.Username, user.Role);
        var refresh = _jwtUtil.GenerateRefreshToken(user.Username);

        return (access, refresh);
    }
}
