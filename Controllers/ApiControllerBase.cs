using Microsoft.AspNetCore.Mvc;
using web.Models;
using web.Services;

namespace web.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected readonly CurrentUserResolver CurrentUserResolver;

    protected ApiControllerBase(CurrentUserResolver currentUserResolver)
    {
        CurrentUserResolver = currentUserResolver;
    }

    protected Task<User?> CurrentUserAsync()
    {
        return CurrentUserResolver.ResolveAsync(HttpContext);
    }

    protected static bool IsAdmin(User? user)
    {
        return user?.Role == "ADMIN";
    }

    protected static IResult ToResult(ServiceResult result)
    {
        return result.StatusCode switch
        {
            StatusCodes.Status200OK => Results.Ok(result.Data),
            StatusCodes.Status400BadRequest => Results.BadRequest(new Dictionary<string, string?>
            {
                [result.MessageField] = result.Message
            }),
            StatusCodes.Status401Unauthorized => Results.Unauthorized(),
            StatusCodes.Status403Forbidden => Results.StatusCode(StatusCodes.Status403Forbidden),
            StatusCodes.Status404NotFound => Results.NotFound(new Dictionary<string, string?>
            {
                [result.MessageField] = result.Message
            }),
            _ => Results.StatusCode(result.StatusCode)
        };
    }
}
