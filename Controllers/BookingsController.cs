using Microsoft.AspNetCore.Mvc;
using web.Models;
using web.Services;

namespace web.Controllers;

[ApiController]
[Route("api/bookings")]
public class BookingsController : ApiControllerBase
{
    private readonly BookingService _bookingService;

    public BookingsController(BookingService bookingService, CurrentUserResolver userResolver) : base(userResolver)
    {
        _bookingService = bookingService;
    }

    [HttpPost]
    public async Task<IResult> Create([FromBody] CreateBookingRequest body)
    {
        var user = await CurrentUserAsync();
        if (user == null) return Results.Unauthorized();

        return ToResult(await _bookingService.CreateAsync(user, body));
    }

    [HttpGet("my")]
    public async Task<IResult> MyBookings()
    {
        var user = await CurrentUserAsync();
        if (user == null) return Results.Unauthorized();

        return Results.Ok(await _bookingService.GetMyBookingsAsync(user.Id));
    }

    [HttpGet("{bookingCode}")]
    public async Task<IResult> DetailByCode([FromRoute] string bookingCode)
    {
        var user = await CurrentUserAsync();
        if (user == null) return Results.Unauthorized();

        return ToResult(await _bookingService.GetDetailByCodeAsync(user, bookingCode));
    }

    [HttpDelete("{id:int}/cancel")]
    public async Task<IResult> Cancel([FromRoute] int id)
    {
        var user = await CurrentUserAsync();
        if (user == null) return Results.Unauthorized();

        return ToResult(await _bookingService.CancelAsync(user, id));
    }

    [HttpPost("{id:int}/confirm")]
    public async Task<IResult> Confirm([FromRoute] int id)
    {
        var user = await CurrentUserAsync();
        if (user == null) return Results.Unauthorized();

        return ToResult(await _bookingService.ConfirmAsync(user, id));
    }
}
