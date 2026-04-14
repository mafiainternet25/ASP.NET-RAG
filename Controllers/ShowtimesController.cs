using Microsoft.AspNetCore.Mvc;
using web.Services;

namespace web.Controllers;

[ApiController]
[Route("api/showtimes")]
public class ShowtimesController : ControllerBase
{
    private readonly ShowtimeService _showtimeService;

    public ShowtimesController(ShowtimeService showtimeService)
    {
        _showtimeService = showtimeService;
    }

    [HttpGet]
    public async Task<IResult> Get([FromQuery] int? movieId, [FromQuery] string? date, [FromQuery] int? cinemaId)
    {
        return Results.Ok(await _showtimeService.GetShowtimesAsync(movieId, date, cinemaId));
    }

    [HttpGet("{showtimeId:int}/seats")]
    public async Task<IResult> GetSeats([FromRoute] int showtimeId)
    {
        var result = await _showtimeService.GetSeatsByShowtimeAsync(showtimeId);
        return result.StatusCode switch
        {
            StatusCodes.Status200OK => Results.Ok(result.Data),
            StatusCodes.Status404NotFound => Results.NotFound(new { message = result.Message }),
            _ => Results.StatusCode(result.StatusCode)
        };
    }
}
