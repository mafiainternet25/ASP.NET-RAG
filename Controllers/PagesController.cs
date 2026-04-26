using Microsoft.AspNetCore.Mvc;

namespace web.Controllers;

public class PagesController : Controller
{
    [HttpGet("/")]
    public IActionResult Index() => View("~/Views/index.cshtml");

    [HttpGet("/login")]
    public IActionResult Login() => View("~/Views/login.cshtml");

    [HttpGet("/auth/login")]
    public IActionResult AuthLogin() => View("~/Views/login.cshtml");

    [HttpGet("/register")]
    public IActionResult Register() => View("~/Views/register.cshtml");

    [HttpGet("/auth/register")]
    public IActionResult AuthRegister() => View("~/Views/register.cshtml");

    [HttpGet("/pages/movies")]
    public IActionResult Movies() => View("~/Views/pages/movies.cshtml");

    [HttpGet("/pages/movie-detail")]
    public IActionResult MovieDetail() => View("~/Views/pages/movie-detail.cshtml");

    [HttpGet("/pages/booking")]
    public IActionResult Booking() => View("~/Views/pages/booking.cshtml");

    [HttpGet("/pages/my-bookings")]
    public IActionResult MyBookings() => View("~/Views/pages/my-bookings.cshtml");

    [HttpGet("/pages/payment")]
    public IActionResult Payment() => View("~/Views/pages/payment.cshtml");

    [HttpGet("/pages/profile")]
    public IActionResult Profile() => View("~/Views/pages/profile.cshtml");

    [HttpGet("/pages/admin")]
    public IActionResult Admin() => View("~/Views/pages/admin.cshtml");

    [HttpGet("/index.html")]
    public IActionResult LegacyIndex() => RedirectPermanent("/");

    [HttpGet("/login.html")]
    public IActionResult LegacyLogin() => RedirectPermanent("/login");

    [HttpGet("/pages/{slug}.html")]
    public IActionResult LegacyPage(string slug)
    {
        var target = slug switch
        {
            "movies" => "/pages/movies",
            "movie-detail" => "/pages/movie-detail",
            "booking" => "/pages/booking",
            "my-bookings" => "/pages/my-bookings",
            "payment" => "/pages/payment",
            "profile" => "/pages/profile",
            "admin" => "/pages/admin",
            _ => "/"
        };

        var query = HttpContext.Request.QueryString.HasValue ? HttpContext.Request.QueryString.Value : string.Empty;
        return RedirectPermanent($"{target}{query}");
    }
}
