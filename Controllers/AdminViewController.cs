using Microsoft.AspNetCore.Mvc;

namespace web.Controllers;

public class AdminViewController : Controller
{
    [HttpGet("/admin")]
    public IActionResult Index() => View("~/Views/Admin/Index.cshtml");

    [HttpGet("/pages/admin")]
    public IActionResult Admin() => View("~/Views/Admin/Index.cshtml");
}
