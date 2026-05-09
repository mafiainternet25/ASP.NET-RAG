using Microsoft.AspNetCore.Mvc;

namespace web.Controllers;

public class AuthViewController : Controller
{
    [HttpGet("/login")]
    public IActionResult Login() => View("~/Views/Auth/Login.cshtml");

    [HttpGet("/auth/login")]
    public IActionResult AuthLogin() => View("~/Views/Auth/Login.cshtml");

    [HttpGet("/register")]
    public IActionResult Register() => View("~/Views/Auth/Register.cshtml");

    [HttpGet("/auth/register")]
    public IActionResult AuthRegister() => View("~/Views/Auth/Register.cshtml");
}
