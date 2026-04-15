using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using web.Data;
using web.Models;

namespace web.Services;

public class CurrentUserResolver
{
    private readonly AuthSessionService _auth;
    private readonly ApplicationDbContext _db;

    public CurrentUserResolver(AuthSessionService auth, ApplicationDbContext db)
    {
        _auth = auth;
        _db = db;
    }

    public async Task<User?> ResolveAsync(HttpContext httpContext)
    {
        var principal = httpContext.User;
        if (principal?.Identity?.IsAuthenticated == true)
        {
            var userIdValue = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdValue, out var userId))
            {
                return await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            }
        }

        var header = httpContext.Request.Headers.Authorization.ToString();
        var user = await _auth.ValidateAccessAsync(header);
        return user;
    }
}
