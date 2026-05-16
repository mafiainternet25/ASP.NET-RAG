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
    private readonly ChatbotService _chatbotService;

    [AllowAnonymous]
    [HttpGet("/admin")]
    public IActionResult Index() => View();

    [AllowAnonymous]
    [HttpGet("/pages/admin")]
    public IActionResult AdminPage() => View("Index");

    public AdminController(AdminService adminService, ChatbotService chatbotService, CurrentUserResolver userResolver) : base(userResolver)
    {
        _adminService = adminService;
        _chatbotService = chatbotService;
    }

    [HttpGet("movies")]
    public async Task<IResult> Movies()
    {
        return Results.Ok(await _adminService.GetMoviesAsync());
    }

    [HttpPost("movies")]
    public async Task<IResult> CreateMovie([FromBody] Movie body)
    {
        var result = await _adminService.CreateMovieAsync(body);
        _ = Task.Run(() => _chatbotService.IngestAsync());
        return Results.Ok(result);
    }

    [HttpPut("movies/{id:int}")]
    public async Task<IResult> UpdateMovie([FromRoute] int id, [FromBody] Movie body)
    {
        var result = await _adminService.UpdateMovieAsync(id, body);
        _ = Task.Run(() => _chatbotService.IngestAsync());
        return ToResult(result);
    }

    [HttpDelete("movies/{id:int}")]
    public async Task<IResult> DeleteMovie([FromRoute] int id)
    {
        var result = await _adminService.DeleteMovieAsync(id);
        _ = Task.Run(() => _chatbotService.IngestAsync());
        return ToResult(result);
    }

    [HttpGet("cinemas")]
    public async Task<IResult> Cinemas()
    {
        return Results.Ok(await _adminService.GetCinemasAsync());
    }

    [HttpPost("cinemas")]
    public async Task<IResult> CreateCinema([FromBody] Cinema body)
    {
        var result = await _adminService.CreateCinemaAsync(body);
        _ = Task.Run(() => _chatbotService.IngestAsync());
        return Results.Ok(result);
    }

    [HttpPut("cinemas/{id:int}")]
    public async Task<IResult> UpdateCinema([FromRoute] int id, [FromBody] Cinema body)
    {
        var result = await _adminService.UpdateCinemaAsync(id, body);
        _ = Task.Run(() => _chatbotService.IngestAsync());
        return ToResult(result);
    }

    [HttpDelete("cinemas/{id:int}")]
    public async Task<IResult> DeleteCinema([FromRoute] int id)
    {
        var result = await _adminService.DeleteCinemaAsync(id);
        _ = Task.Run(() => _chatbotService.IngestAsync());
        return ToResult(result);
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
        var result = await _adminService.CreateShowtimeAsync(body);
        _ = Task.Run(() => _chatbotService.IngestAsync());
        return ToResult(result);
    }

    [HttpPut("showtimes/{id:int}")]
    public async Task<IResult> UpdateShowtime([FromRoute] int id, [FromBody] Showtime body)
    {
        var result = await _adminService.UpdateShowtimeAsync(id, body);
        _ = Task.Run(() => _chatbotService.IngestAsync());
        return ToResult(result);
    }

    [HttpDelete("showtimes/{id:int}")]
    public async Task<IResult> DeleteShowtime([FromRoute] int id)
    {
        var result = await _adminService.DeleteShowtimeAsync(id);
        _ = Task.Run(() => _chatbotService.IngestAsync());
        return ToResult(result);
    }

    [HttpGet("promotions")]
    public async Task<IResult> Promotions()
    {
        return Results.Ok(await _adminService.GetPromotionsAsync());
    }

    [HttpPost("promotions")]
    public async Task<IResult> CreatePromotion([FromBody] Promotion body)
    {
        var result = await _adminService.CreatePromotionAsync(body);
        _ = Task.Run(() => _chatbotService.IngestAsync());
        return Results.Ok(result);
    }

    [HttpPut("promotions/{id:int}")]
    public async Task<IResult> UpdatePromotion([FromRoute] int id, [FromBody] Promotion body)
    {
        var result = await _adminService.UpdatePromotionAsync(id, body);
        _ = Task.Run(() => _chatbotService.IngestAsync());
        return ToResult(result);
    }

    [HttpDelete("promotions/{id:int}")]
    public async Task<IResult> DeletePromotion([FromRoute] int id)
    {
        var result = await _adminService.DeletePromotionAsync(id);
        _ = Task.Run(() => _chatbotService.IngestAsync());
        return ToResult(result);
    }

    [HttpGet("snacks")]
    public async Task<IResult> Snacks()
    {
        return Results.Ok(await _adminService.GetSnacksAsync());
    }

    [HttpPost("snacks")]
    public async Task<IResult> CreateSnack([FromBody] Snack body)
    {
        var result = await _adminService.CreateSnackAsync(body);
        _ = Task.Run(() => _chatbotService.IngestAsync());
        return Results.Ok(result);
    }

    [HttpPut("snacks/{id:int}")]
    public async Task<IResult> UpdateSnack([FromRoute] int id, [FromBody] Snack body)
    {
        var result = await _adminService.UpdateSnackAsync(id, body);
        _ = Task.Run(() => _chatbotService.IngestAsync());
        return ToResult(result);
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
