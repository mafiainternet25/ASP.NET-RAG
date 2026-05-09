using Microsoft.AspNetCore.Mvc;

namespace web.Controllers;

public class UserViewController : Controller
{
    [HttpGet("/profile")]
    public IActionResult Profile() => View("~/Views/Users/Profile.cshtml");

    [HttpGet("/pages/profile")]
    public IActionResult UserProfile() => View("~/Views/Users/Profile.cshtml");
}
