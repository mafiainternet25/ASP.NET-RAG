using Microsoft.AspNetCore.Mvc;
using web.Models;
using web.Services;

namespace web.Controllers;

[ApiController]
[Route("api/reviews")]
public class ReviewsController : ApiControllerBase
{
    private readonly ReviewService _reviewService;

    public ReviewsController(ReviewService reviewService, CurrentUserResolver userResolver) : base(userResolver)
    {
        _reviewService = reviewService;
    }

    [HttpPost]
    public async Task<IResult> Create([FromBody] ReviewCreateRequest body)
    {
        var me = await CurrentUserAsync();
        if (me == null) return Results.Unauthorized();
        return ToResult(await _reviewService.CreateReviewAsync(me.Id, body));
    }

    [HttpDelete("{id:int}")]
    public async Task<IResult> Delete([FromRoute] int id)
    {
        var me = await CurrentUserAsync();
        if (me == null) return Results.Unauthorized();
        return ToResult(await _reviewService.DeleteReviewAsync(id, me.Id, IsAdmin(me)));
    }
}
