using Microsoft.AspNetCore.Mvc;

namespace web.Controllers;

public class BookingViewController : Controller
{
    [HttpGet("/bookings")]
    public IActionResult Index() => View("~/Views/Bookings/Index.cshtml");

    [HttpGet("/pages/booking")]
    public IActionResult Booking() => View("~/Views/Bookings/Index.cshtml");

    [HttpGet("/my-bookings")]
    public IActionResult MyBookings() => View("~/Views/Bookings/MyBookings.cshtml");

    [HttpGet("/pages/my-bookings")]
    public IActionResult MyBookingsLegacy() => View("~/Views/Bookings/MyBookings.cshtml");
}
