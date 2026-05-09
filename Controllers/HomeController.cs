using Microsoft.AspNetCore.Mvc;

namespace web.Controllers;

public class HomeController : Controller
{
    [HttpGet("/")]
    public IActionResult Index() => View("~/Views/Home/Index.cshtml");
}
