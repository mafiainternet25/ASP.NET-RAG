using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web.Data;
using web.Models;
using web.Services;

namespace web.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ApiControllerBase
{
    private readonly ApplicationDbContext _db;

    public UsersController(ApplicationDbContext db, CurrentUserResolver userResolver) : base(userResolver)
    {
        _db = db;
    }

    [HttpGet("me")]
    public async Task<IResult> Me()
    {
        var me = await CurrentUserAsync();
        if (me == null)
        {
            return Results.Unauthorized();
        }

        return Results.Ok(new
        {
            me.Id,
            me.Username,
            me.Email,
            me.FullName,
            me.Phone,
            me.Role,
            me.CreatedAt
        });
    }

    [HttpPut("me")]
    public async Task<IResult> UpdateMe([FromBody] UpdateProfileRequest body)
    {
        var me = await CurrentUserAsync();
        if (me == null)
        {
            return Results.Unauthorized();
        }

        if (!string.IsNullOrWhiteSpace(body.NewPassword))
        {
            var currentPasswordOk = false;
            if (!string.IsNullOrWhiteSpace(body.CurrentPassword))
            {
                currentPasswordOk = me.PasswordHash == body.CurrentPassword;
                if (!currentPasswordOk)
                {
                    try
                    {
                        currentPasswordOk = BCrypt.Net.BCrypt.Verify(body.CurrentPassword, me.PasswordHash);
                    }
                    catch
                    {
                        currentPasswordOk = false;
                    }
                }
            }
            if (!currentPasswordOk)
            {
                return Results.BadRequest(new { error = "Mat khau hien tai khong dung" });
            }

            me.PasswordHash = BCrypt.Net.BCrypt.HashPassword(body.NewPassword);
        }

        if (!string.IsNullOrWhiteSpace(body.Email) && body.Email != me.Email)
        {
            var emailExists = await _db.Users.AnyAsync(x => x.Email == body.Email && x.Id != me.Id);
            if (emailExists)
            {
                return Results.BadRequest(new { error = "Email da duoc su dung" });
            }

            me.Email = body.Email.Trim();
        }

        me.FullName = body.FullName?.Trim();
        me.Phone = body.Phone?.Trim();

        await _db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }
}
