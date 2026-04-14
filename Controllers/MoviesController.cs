using Microsoft.AspNetCore.Mvc;
using web.Services;

namespace web.Controllers;

[ApiController]
[Route("api")]
public class MoviesController : ControllerBase
{
    private readonly MovieService _movieService;

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
    public async Task<IResult> SearchMovies([FromQuery] string? q, [FromQuery] string? genre, [FromQuery] int? page, [FromQuery] int? size)
    {
        var data = await _movieService.SearchMoviesAsync(q, genre, page, size);
        return Results.Ok(data);
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
