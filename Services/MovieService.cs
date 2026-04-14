using Microsoft.EntityFrameworkCore;
using web.Data;

namespace web.Services;

public class MovieService
{
    private readonly ApplicationDbContext _db;

    public MovieService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<object> GetMoviesAsync(int? page, int? size)
    {
        var pageValue = Math.Max(0, page ?? 0);
        var sizeValue = Math.Clamp(size ?? 12, 1, 100);

        var query = _db.Movies.AsNoTracking().OrderByDescending(x => x.Id);
        var total = await query.CountAsync();
        var items = await query.Skip(pageValue * sizeValue).Take(sizeValue).ToListAsync();

        return new
        {
            content = items,
            totalPages = (int)Math.Ceiling(total / (double)sizeValue),
            totalElements = total
        };
    }

    public async Task<object> SearchMoviesAsync(string? q, string? genre, int? page, int? size)
    {
        var pageValue = Math.Max(0, page ?? 0);
        var sizeValue = Math.Clamp(size ?? 12, 1, 100);

        var query = _db.Movies.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var kw = q.Trim().ToLower();
            query = query.Where(m => m.Title.ToLower().Contains(kw)
                                     || (m.Description != null && m.Description.ToLower().Contains(kw))
                                     || (m.Genre != null && m.Genre.ToLower().Contains(kw)));
        }

        if (!string.IsNullOrWhiteSpace(genre))
        {
            var g = genre.Trim().ToLower();
            query = query.Where(m => m.Genre != null && m.Genre.ToLower().Contains(g));
        }

        query = query.OrderByDescending(x => x.Id);
        var total = await query.CountAsync();
        var items = await query.Skip(pageValue * sizeValue).Take(sizeValue).ToListAsync();

        return new
        {
            content = items,
            totalPages = (int)Math.Ceiling(total / (double)sizeValue),
            totalElements = total
        };
    }

    public Task<List<Models.Movie>> GetNowPlayingAsync()
    {
        return _db.Movies.AsNoTracking()
            .Where(x => x.Status == "NOW_SHOWING")
            .OrderByDescending(x => x.Id)
            .Take(12)
            .ToListAsync();
    }

    public Task<List<Models.Movie>> GetComingSoonAsync()
    {
        return _db.Movies.AsNoTracking()
            .Where(x => x.Status == "COMING_SOON")
            .OrderByDescending(x => x.Id)
            .Take(12)
            .ToListAsync();
    }

    public Task<Models.Movie?> GetMovieDetailAsync(int id)
    {
        return _db.Movies.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
    }

    public Task<List<object>> GetMovieReviewsAsync(int id)
    {
        return _db.Reviews
            .AsNoTracking()
            .Where(x => x.MovieId == id)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => (object)new
            {
                x.Id,
                x.MovieId,
                x.UserId,
                username = x.User != null ? x.User.Username : string.Empty,
                x.Rating,
                x.Comment,
                x.CreatedAt
            })
            .ToListAsync();
    }
}
