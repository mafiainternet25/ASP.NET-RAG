using Microsoft.AspNetCore.Mvc;

namespace web.Controllers;


public class PagesController : Controller
{
    [HttpGet("/index.html")]
    public IActionResult LegacyIndex() => RedirectPermanent("/");

    [HttpGet("/login.html")]
    public IActionResult LegacyLogin() => RedirectPermanent("/login");

    [HttpGet("/pages/{slug}.html")]
    public IActionResult LegacyPage(string slug)
    {
        var target = slug switch
        {
            "movies" => "/movies",
            "movie-detail" => "/movie-detail",
            "booking" => "/bookings",
            "my-bookings" => "/my-bookings",
            "payment" => "/payment",
            "profile" => "/profile",
            "admin" => "/admin",
            _ => "/"
        };

        var query = HttpContext.Request.QueryString.HasValue ? HttpContext.Request.QueryString.Value : string.Empty;
        return RedirectPermanent($"{target}{query}");
    }
}
