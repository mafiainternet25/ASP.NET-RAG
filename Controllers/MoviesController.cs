using Microsoft.AspNetCore.Mvc;
using web.Services;

namespace web.Controllers;

[ApiController]
[Route("api")]
public class MoviesController : Controller
{
    private readonly MovieService _movieService;

    [HttpGet("/movies")]
    public IActionResult Index() => View();

    [HttpGet("/pages/movies")]
    public IActionResult MoviesPage() => View("Index");

    [HttpGet("/movie/{id}")]
    public IActionResult Detail(int id) => View();

    [HttpGet("/pages/movie-detail")]
    public IActionResult MovieDetailPage() => View("Detail");

    public MoviesController(MovieService movieService)
    {
        _movieService = movieService;
    }

    [HttpGet("movies")]
    public async Task<IResult> GetMovies([FromQuery] int? page, [FromQuery] int? size)
    {
        var data = await _movieService.GetMoviesAsync(page, size);
        return Results.Ok(data);
    }

    [HttpGet("movies/search")]
    public async Task<IResult> SearchMovies([FromQuery] string? q, [FromQuery] string? genre, [FromQuery] int? cinemaId, [FromQuery] DateTime? fromDate, [FromQuery] int? page, [FromQuery] int? size)
    {
        var data = await _movieService.SearchMoviesAsync(q, genre, cinemaId, fromDate, page, size);
        return Results.Ok(data);
    }

    [HttpGet("movies/suggestions")]
    public async Task<IResult> GetSearchSuggestions([FromQuery] string? q, [FromQuery] string? genre, [FromQuery] int? cinemaId, [FromQuery] DateTime? fromDate)
    {
        var suggestions = await _movieService.SearchSuggestionsAsync(q, genre, cinemaId, fromDate);
        return Results.Ok(suggestions);
    }

    [HttpGet("movies/genres")]
    public async Task<IResult> GetGenres()
    {
        var genres = await _movieService.GetAllGenresAsync();
        return Results.Ok(genres);
    }

    [HttpGet("movies/now-playing")]
    public async Task<IResult> GetNowPlaying()
    {
        var list = await _movieService.GetNowPlayingAsync();
        return Results.Ok(list);
    }

    [HttpGet("movies/coming-soon")]
    public async Task<IResult> GetComingSoon()
    {
        var list = await _movieService.GetComingSoonAsync();
        return Results.Ok(list);
    }

    [HttpGet("movies/{id:int}")]
    public async Task<IResult> GetMovieDetail([FromRoute] int id)
    {
        var movie = await _movieService.GetMovieDetailAsync(id);
        return movie == null ? Results.NotFound() : Results.Ok(movie);
    }

    [HttpGet("movies/{id:int}/reviews")]
    public async Task<IResult> GetMovieReviews([FromRoute] int id)
    {
        var list = await _movieService.GetMovieReviewsAsync(id);

        return Results.Ok(list);
    }
}
