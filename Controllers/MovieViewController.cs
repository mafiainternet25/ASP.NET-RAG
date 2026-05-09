using Microsoft.AspNetCore.Mvc;

namespace web.Controllers;

public class MovieViewController : Controller
{
    [HttpGet("/movies")]
    public IActionResult Index() => View("~/Views/Movies/Index.cshtml");

    [HttpGet("/pages/movies")]
    public IActionResult MoviesList() => View("~/Views/Movies/Index.cshtml");

    [HttpGet("/movie/{id}")]
    public IActionResult Detail(int id) => View("~/Views/Movies/Detail.cshtml");

    [HttpGet("/pages/movie-detail")]
    public IActionResult MovieDetail() => View("~/Views/Movies/Detail.cshtml");
}
