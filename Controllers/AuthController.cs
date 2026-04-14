using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web.Data;
using web.Models;
using web.Services;

namespace web.Controllers;

[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly AuthSessionService _auth;

    public AuthController(ApplicationDbContext db, AuthSessionService auth)
    {
        _db = db;
        _auth = auth;
    }

    [HttpPost("register")]
    public async Task<IResult> Register([FromBody] RegisterRequest body)
    {
        if (string.IsNullOrWhiteSpace(body.Username) || string.IsNullOrWhiteSpace(body.Email) || string.IsNullOrWhiteSpace(body.Password))
        {
            return Results.BadRequest(new { error = "Vui long nhap day du thong tin" });
        }

        var existed = await _db.Users.AnyAsync(u => u.Username == body.Username || u.Email == body.Email);
        if (existed)
        {
            return Results.BadRequest(new { error = "Username hoac email da ton tai" });
        }

        var user = new User
        {
            Username = body.Username.Trim(),
            Email = body.Email.Trim(),
            FullName = body.FullName?.Trim(),
            Phone = body.Phone?.Trim(),
            Role = "USER",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(body.Password)
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var tokens = await _auth.CreateSessionAsync(user);
        return Results.Ok(new { accessToken = tokens.AccessToken, refreshToken = tokens.RefreshToken });
    }

    [HttpPost("login")]
    public async Task<IResult> Login([FromBody] LoginRequest body)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Username == body.Username || x.Email == body.Username);
        if (user == null)
        {
            return Results.BadRequest(new { error = "Sai tai khoan hoac mat khau" });
        }

        var password = body.Password ?? string.Empty;
        var passwordOk = user.PasswordHash == password;
        if (!passwordOk)
        {
            try
            {
                passwordOk = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
            }
            catch
            {
                passwordOk = false;
            }
        }
        if (!passwordOk)
        {
            return Results.BadRequest(new { error = "Sai tai khoan hoac mat khau" });
        }

        var tokens = await _auth.CreateSessionAsync(user);
        return Results.Ok(new { accessToken = tokens.AccessToken, refreshToken = tokens.RefreshToken });
    }

    [HttpPost("refresh")]
    public async Task<IResult> Refresh([FromBody] RefreshRequest body)
    {
        if (string.IsNullOrWhiteSpace(body.RefreshToken))
        {
            return Results.BadRequest(new { error = "Thieu refresh token" });
        }

        var refreshed = await _auth.RefreshAsync(body.RefreshToken);
        if (refreshed == null)
        {
            return Results.Unauthorized();
        }

        return Results.Ok(new { accessToken = refreshed.Value.AccessToken, refreshToken = refreshed.Value.RefreshToken });
    }
}
