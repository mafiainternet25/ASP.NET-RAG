using Microsoft.EntityFrameworkCore;
using web.Data;
using web.Models;

namespace web.Services;

public class ReviewService
{
    private readonly ApplicationDbContext _db;

    public ReviewService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<ServiceResult> CreateReviewAsync(int userId, ReviewCreateRequest req)
    {
        if (req.Rating < 1 || req.Rating > 5)
        {
            return ServiceResult.BadRequest("Rating phai tu 1 den 5");
        }

        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == userId);
        if (user == null)
        {
            return ServiceResult.NotFound("User khong ton tai");
        }

        var movie = await _db.Movies.FirstOrDefaultAsync(x => x.Id == req.MovieId);
        if (movie == null)
        {
            return ServiceResult.BadRequest("Phim khong ton tai");
        }

        var duplicated = await _db.Reviews.AnyAsync(x => x.UserId == userId && x.MovieId == req.MovieId);
        if (duplicated)
        {
            return ServiceResult.BadRequest("Ban da danh gia phim nay roi");
        }

        var review = new Review
        {
            UserId = userId,
            MovieId = req.MovieId,
            Rating = req.Rating,
            Comment = req.Comment?.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _db.Reviews.Add(review);
        await _db.SaveChangesAsync();
        await UpdateMovieRatingAsync(req.MovieId);

        return ServiceResult.Ok(new { success = true });
    }

    public async Task<object> GetReviewsByMovieIdAsync(int movieId)
    {
        return await _db.Reviews
            .AsNoTracking()
            .Where(x => x.MovieId == movieId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new
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

    public async Task<ServiceResult> DeleteReviewAsync(int reviewId, int userId, bool isAdmin)
    {
        var review = await _db.Reviews.FirstOrDefaultAsync(x => x.Id == reviewId);
        if (review == null)
        {
            return ServiceResult.NotFound("Khong tim thay review", "error");
        }

        if (review.UserId != userId && !isAdmin)
        {
            return ServiceResult.Forbidden();
        }

        var movieId = review.MovieId;
        _db.Reviews.Remove(review);
        await _db.SaveChangesAsync();
        await UpdateMovieRatingAsync(movieId);

        return ServiceResult.Ok(new { success = true });
    }

    private async Task UpdateMovieRatingAsync(int movieId)
    {
        var movie = await _db.Movies.FirstOrDefaultAsync(x => x.Id == movieId);
        if (movie == null)
        {
            return;
        }

        var avg = await _db.Reviews.Where(x => x.MovieId == movieId).Select(x => (decimal?)x.Rating).AverageAsync();
        movie.Rating = avg ?? 0m;
        await _db.SaveChangesAsync();
    }
}
