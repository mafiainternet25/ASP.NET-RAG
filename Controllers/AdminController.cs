using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using web.Models;
using web.Security;
using web.Services;

namespace web.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Policy = TokenAuthenticationDefaults.AdminOnlyPolicy)]
public class AdminController : ApiControllerBase
{
    private readonly AdminService _adminService;

    public AdminController(AdminService adminService, CurrentUserResolver userResolver) : base(userResolver)
    {
        _adminService = adminService;
    }

    [HttpGet("movies")]
    public async Task<IResult> Movies()
    {
        return Results.Ok(await _adminService.GetMoviesAsync());
    }

    [HttpPost("movies")]
    public async Task<IResult> CreateMovie([FromBody] Movie body)
    {
        return Results.Ok(await _adminService.CreateMovieAsync(body));
    }

    [HttpPut("movies/{id:int}")]
    public async Task<IResult> UpdateMovie([FromRoute] int id, [FromBody] Movie body)
    {
        return ToResult(await _adminService.UpdateMovieAsync(id, body));
    }

    [HttpDelete("movies/{id:int}")]
    public async Task<IResult> DeleteMovie([FromRoute] int id)
    {
        return ToResult(await _adminService.DeleteMovieAsync(id));
    }

    [HttpGet("cinemas")]
    public async Task<IResult> Cinemas()
    {
        return Results.Ok(await _adminService.GetCinemasAsync());
    }

    [HttpPost("cinemas")]
    public async Task<IResult> CreateCinema([FromBody] Cinema body)
    {
        return Results.Ok(await _adminService.CreateCinemaAsync(body));
    }

    [HttpPut("cinemas/{id:int}")]
    public async Task<IResult> UpdateCinema([FromRoute] int id, [FromBody] Cinema body)
    {
        return ToResult(await _adminService.UpdateCinemaAsync(id, body));
    }

    [HttpDelete("cinemas/{id:int}")]
    public async Task<IResult> DeleteCinema([FromRoute] int id)
    {
        return ToResult(await _adminService.DeleteCinemaAsync(id));
    }

    [HttpGet("rooms")]
    public async Task<IResult> Rooms()
    {
        return Results.Ok(await _adminService.GetRoomsAsync());
    }

    [HttpPost("rooms")]
    public async Task<IResult> CreateRoom([FromBody] Room body)
    {
        return ToResult(await _adminService.CreateRoomAsync(body));
    }

    [HttpPut("rooms/{id:int}")]
    public async Task<IResult> UpdateRoom([FromRoute] int id, [FromBody] Room body)
    {
        return ToResult(await _adminService.UpdateRoomAsync(id, body));
    }

    [HttpDelete("rooms/{id:int}")]
    public async Task<IResult> DeleteRoom([FromRoute] int id)
    {
        return ToResult(await _adminService.DeleteRoomAsync(id));
    }

    [HttpGet("showtimes")]
    public async Task<IResult> Showtimes()
    {
        return Results.Ok(await _adminService.GetShowtimesAsync());
    }

    [HttpPost("showtimes")]
    public async Task<IResult> CreateShowtime([FromBody] Showtime body)
    {
        return ToResult(await _adminService.CreateShowtimeAsync(body));
    }

    [HttpPut("showtimes/{id:int}")]
    public async Task<IResult> UpdateShowtime([FromRoute] int id, [FromBody] Showtime body)
    {
        return ToResult(await _adminService.UpdateShowtimeAsync(id, body));
    }

    [HttpDelete("showtimes/{id:int}")]
    public async Task<IResult> DeleteShowtime([FromRoute] int id)
    {
        return ToResult(await _adminService.DeleteShowtimeAsync(id));
    }

    [HttpGet("promotions")]
    public async Task<IResult> Promotions()
    {
        return Results.Ok(await _adminService.GetPromotionsAsync());
    }

    [HttpPost("promotions")]
    public async Task<IResult> CreatePromotion([FromBody] Promotion body)
    {
        return Results.Ok(await _adminService.CreatePromotionAsync(body));
    }

    [HttpPut("promotions/{id:int}")]
    public async Task<IResult> UpdatePromotion([FromRoute] int id, [FromBody] Promotion body)
    {
        return ToResult(await _adminService.UpdatePromotionAsync(id, body));
    }

    [HttpDelete("promotions/{id:int}")]
    public async Task<IResult> DeletePromotion([FromRoute] int id)
    {
        return ToResult(await _adminService.DeletePromotionAsync(id));
    }

    [HttpGet("snacks")]
    public async Task<IResult> Snacks()
    {
        return Results.Ok(await _adminService.GetSnacksAsync());
    }

    [HttpPost("snacks")]
    public async Task<IResult> CreateSnack([FromBody] Snack body)
    {
        return Results.Ok(await _adminService.CreateSnackAsync(body));
    }

    [HttpPut("snacks/{id:int}")]
    public async Task<IResult> UpdateSnack([FromRoute] int id, [FromBody] Snack body)
    {
        return ToResult(await _adminService.UpdateSnackAsync(id, body));
    }

    [HttpDelete("snacks/{id:int}")]
    public async Task<IResult> DeleteSnack([FromRoute] int id)
    {
        return ToResult(await _adminService.DeleteSnackAsync(id));
    }

    [HttpGet("users")]
    public async Task<IResult> Users()
    {
        return Results.Ok(await _adminService.GetUsersAsync());
    }

    [HttpGet("reports/revenue")]
    public async Task<IResult> Revenue([FromQuery] string? from, [FromQuery] string? to)
    {
        return Results.Ok(await _adminService.GetRevenueAsync(from, to));
    }

    [HttpGet("reports/top-movies")]
    public async Task<IResult> TopMovies([FromQuery] int? limit)
    {
        return Results.Ok(await _adminService.GetTopMoviesAsync(limit));
    }
}
