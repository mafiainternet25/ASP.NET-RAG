using Microsoft.EntityFrameworkCore;
using web.Data;
using web.Models;

namespace web.Services;

public class AdminService
{
    private readonly ApplicationDbContext _db;

    public AdminService(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<List<Movie>> GetMoviesAsync() => _db.Movies.AsNoTracking().OrderByDescending(x => x.Id).ToListAsync();

    public async Task<Movie> CreateMovieAsync(Movie body)
    {
        _db.Movies.Add(body);
        await _db.SaveChangesAsync();
        return body;
    }

    public async Task<ServiceResult> UpdateMovieAsync(int id, Movie body)
    {
        var item = await _db.Movies.FirstOrDefaultAsync(x => x.Id == id);
        if (item == null) return ServiceResult.NotFound("Khong tim thay phim", "error");
        item.Title = body.Title;
        item.Genre = body.Genre;
        item.DurationMin = body.DurationMin;
        item.PosterUrl = body.PosterUrl;
        item.Status = body.Status;
        item.Description = body.Description;
        item.Rating = body.Rating;
        item.TrailerUrl = body.TrailerUrl;
        await _db.SaveChangesAsync();
        return ServiceResult.Ok(item);
    }

    public async Task<ServiceResult> DeleteMovieAsync(int id)
    {
        var item = await _db.Movies.FirstOrDefaultAsync(x => x.Id == id);
        if (item == null) return ServiceResult.NotFound("Khong tim thay phim");
        _db.Movies.Remove(item);
        await _db.SaveChangesAsync();
        return ServiceResult.Ok(new { success = true });
    }

    public Task<List<Cinema>> GetCinemasAsync() => _db.Cinemas.AsNoTracking().OrderBy(x => x.Id).ToListAsync();

    public async Task<Cinema> CreateCinemaAsync(Cinema body)
    {
        _db.Cinemas.Add(body);
        await _db.SaveChangesAsync();
        return body;
    }

    public async Task<ServiceResult> UpdateCinemaAsync(int id, Cinema body)
    {
        var item = await _db.Cinemas.FirstOrDefaultAsync(x => x.Id == id);
        if (item == null) return ServiceResult.NotFound("Khong tim thay rap", "error");
        item.Name = body.Name;
        item.Address = body.Address;
        item.City = body.City;
        await _db.SaveChangesAsync();
        return ServiceResult.Ok(item);
    }

    public async Task<ServiceResult> DeleteCinemaAsync(int id)
    {
        var item = await _db.Cinemas.FirstOrDefaultAsync(x => x.Id == id);
        if (item == null) return ServiceResult.NotFound("Khong tim thay rap");
        _db.Cinemas.Remove(item);
        await _db.SaveChangesAsync();
        return ServiceResult.Ok(new { success = true });
    }

    public async Task<object> GetRoomsAsync()
    {
        var list = await _db.Rooms.AsNoTracking().OrderBy(x => x.Id)
            .Select(r => new
            {
                r.Id,
                r.CinemaId,
                cinemaName = r.Cinema != null ? r.Cinema.Name : string.Empty,
                r.Name,
                r.TotalSeats,
                r.RoomType
            }).ToListAsync();
        return list;
    }

    public async Task<ServiceResult> CreateRoomAsync(Room body)
    {
        if (body.CinemaId <= 0)
        {
            return ServiceResult.BadRequest("CinemaId khong hop le");
        }

        if (string.IsNullOrWhiteSpace(body.Name))
        {
            return ServiceResult.BadRequest("Ten phong khong duoc de trong");
        }

        if (body.TotalSeats <= 0)
        {
            return ServiceResult.BadRequest("Tong so ghe phai lon hon 0");
        }

        var cinemaExists = await _db.Cinemas.AnyAsync(x => x.Id == body.CinemaId);
        if (!cinemaExists)
        {
            return ServiceResult.BadRequest("Rap khong ton tai");
        }

        var roomName = body.Name.Trim();
        var duplicatedRoom = await _db.Rooms.AnyAsync(x => x.CinemaId == body.CinemaId && x.Name == roomName);
        if (duplicatedRoom)
        {
            return ServiceResult.BadRequest("Phong nay da ton tai trong rap");
        }

        body.Name = roomName;
        _db.Rooms.Add(body);
        await _db.SaveChangesAsync();

        var existed = await _db.Seats.CountAsync(x => x.RoomId == body.Id);
        if (existed == 0)
        {
            var cols = 10;
            var rows = Math.Max(1, (int)Math.Ceiling(body.TotalSeats / (double)cols));
            var seats = new List<Seat>();
            for (var row = 0; row < rows; row++)
            {
                var rowName = ((char)('A' + row)).ToString();
                for (var col = 1; col <= cols; col++)
                {
                    if (seats.Count >= body.TotalSeats) break;
                    seats.Add(new Seat
                    {
                        RoomId = body.Id,
                        SeatRow = rowName,
                        SeatNumber = col,
                        SeatType = row >= 2 ? "VIP" : "NORMAL",
                        ExtraPrice = row >= 2 ? 20000 : 0
                    });
                }
            }

            _db.Seats.AddRange(seats);
            await _db.SaveChangesAsync();
        }

        return ServiceResult.Ok(body);
    }

    public async Task<ServiceResult> UpdateRoomAsync(int id, Room body)
    {
        var item = await _db.Rooms.FirstOrDefaultAsync(x => x.Id == id);
        if (item == null) return ServiceResult.NotFound("Khong tim thay phong");
        item.CinemaId = body.CinemaId;
        item.Name = body.Name;
        item.TotalSeats = body.TotalSeats;
        item.RoomType = body.RoomType;
        await _db.SaveChangesAsync();
        return ServiceResult.Ok(item);
    }

    public async Task<ServiceResult> DeleteRoomAsync(int id)
    {
        var item = await _db.Rooms.FirstOrDefaultAsync(x => x.Id == id);
        if (item == null) return ServiceResult.NotFound("Khong tim thay phong");
        _db.Rooms.Remove(item);
        await _db.SaveChangesAsync();
        return ServiceResult.Ok(new { success = true });
    }

    public Task<List<Showtime>> GetShowtimesAsync() => _db.Showtimes.AsNoTracking().OrderByDescending(x => x.StartTime).ToListAsync();

    public async Task<ServiceResult> CreateShowtimeAsync(Showtime body)
    {
        if (body.MovieId <= 0 || body.RoomId <= 0)
        {
            return ServiceResult.BadRequest("MovieId hoac RoomId khong hop le");
        }

        if (body.Price <= 0)
        {
            return ServiceResult.BadRequest("Gia ve phai lon hon 0");
        }

        if (body.StartTime <= DateTime.MinValue)
        {
            return ServiceResult.BadRequest("Thoi gian chieu khong hop le");
        }

        var movieExists = await _db.Movies.AnyAsync(x => x.Id == body.MovieId);
        if (!movieExists)
        {
            return ServiceResult.BadRequest("Phim khong ton tai");
        }

        var roomExists = await _db.Rooms.AnyAsync(x => x.Id == body.RoomId);
        if (!roomExists)
        {
            return ServiceResult.BadRequest("Phong khong ton tai");
        }

        var duplicated = await _db.Showtimes.AnyAsync(x => x.RoomId == body.RoomId && x.StartTime == body.StartTime);
        if (duplicated)
        {
            return ServiceResult.BadRequest("Da ton tai suat chieu cung gio trong phong nay");
        }

        _db.Showtimes.Add(body);
        await _db.SaveChangesAsync();
        return ServiceResult.Ok(body);
    }

    public async Task<ServiceResult> UpdateShowtimeAsync(int id, Showtime body)
    {
        var item = await _db.Showtimes.FirstOrDefaultAsync(x => x.Id == id);
        if (item == null) return ServiceResult.NotFound("Khong tim thay suat chieu");

        if (body.MovieId <= 0 || body.RoomId <= 0)
        {
            return ServiceResult.BadRequest("MovieId hoac RoomId khong hop le");
        }

        if (body.Price <= 0)
        {
            return ServiceResult.BadRequest("Gia ve phai lon hon 0");
        }

        if (body.StartTime <= DateTime.MinValue)
        {
            return ServiceResult.BadRequest("Thoi gian chieu khong hop le");
        }

        var movieExists = await _db.Movies.AnyAsync(x => x.Id == body.MovieId);
        if (!movieExists)
        {
            return ServiceResult.BadRequest("Phim khong ton tai");
        }

        var roomExists = await _db.Rooms.AnyAsync(x => x.Id == body.RoomId);
        if (!roomExists)
        {
            return ServiceResult.BadRequest("Phong khong ton tai");
        }

        var duplicated = await _db.Showtimes.AnyAsync(x => x.Id != id && x.RoomId == body.RoomId && x.StartTime == body.StartTime);
        if (duplicated)
        {
            return ServiceResult.BadRequest("Da ton tai suat chieu cung gio trong phong nay");
        }

        item.MovieId = body.MovieId;
        item.RoomId = body.RoomId;
        item.StartTime = body.StartTime;
        item.Price = body.Price;
        await _db.SaveChangesAsync();
        return ServiceResult.Ok(item);
    }

    public async Task<ServiceResult> DeleteShowtimeAsync(int id)
    {
        var item = await _db.Showtimes.FirstOrDefaultAsync(x => x.Id == id);
        if (item == null) return ServiceResult.NotFound("Khong tim thay suat chieu");

        var hasBookings = await _db.Bookings.AnyAsync(x => x.ShowtimeId == id);
        if (hasBookings)
        {
            return ServiceResult.BadRequest("Khong the xoa suat chieu da co don dat ve");
        }

        _db.Showtimes.Remove(item);
        await _db.SaveChangesAsync();
        return ServiceResult.Ok(new { success = true });
    }

    public Task<List<Promotion>> GetPromotionsAsync() => _db.Promotions.AsNoTracking().OrderByDescending(x => x.Id).ToListAsync();

    public async Task<Promotion> CreatePromotionAsync(Promotion body)
    {
        body.Code = body.Code.Trim().ToUpper();
        _db.Promotions.Add(body);
        await _db.SaveChangesAsync();
        return body;
    }

    public async Task<ServiceResult> UpdatePromotionAsync(int id, Promotion body)
    {
        var item = await _db.Promotions.FirstOrDefaultAsync(x => x.Id == id);
        if (item == null) return ServiceResult.NotFound("Khong tim thay khuyen mai", "error");
        item.Code = body.Code.Trim().ToUpper();
        item.Description = body.Description;
        item.DiscountPercent = body.DiscountPercent;
        item.ValidFrom = body.ValidFrom;
        item.ValidTo = body.ValidTo;
        item.MaxUses = body.MaxUses;
        await _db.SaveChangesAsync();
        return ServiceResult.Ok(item);
    }

    public async Task<ServiceResult> DeletePromotionAsync(int id)
    {
        var item = await _db.Promotions.FirstOrDefaultAsync(x => x.Id == id);
        if (item == null) return ServiceResult.NotFound("Khong tim thay khuyen mai");
        _db.Promotions.Remove(item);
        await _db.SaveChangesAsync();
        return ServiceResult.Ok(new { success = true });
    }

    public Task<List<Snack>> GetSnacksAsync() => _db.Snacks.AsNoTracking().OrderByDescending(x => x.Id).ToListAsync();

    public async Task<Snack> CreateSnackAsync(Snack body)
    {
        body.Id = 0;
        body.BookingSnacks = [];
        body.CreatedAt = DateTime.UtcNow;
        body.Name = body.Name?.Trim() ?? string.Empty;
        body.Category = string.IsNullOrWhiteSpace(body.Category) ? "FOOD" : body.Category.Trim().ToUpperInvariant();
        _db.Snacks.Add(body);
        await _db.SaveChangesAsync();
        return body;
    }

    public async Task<ServiceResult> UpdateSnackAsync(int id, Snack body)
    {
        var item = await _db.Snacks.FirstOrDefaultAsync(x => x.Id == id);
        if (item == null) return ServiceResult.NotFound("Khong tim thay do an", "error");

        item.Name = body.Name?.Trim() ?? string.Empty;
        item.Description = body.Description;
        item.Price = body.Price;
        item.ImageUrl = body.ImageUrl;
        item.Category = string.IsNullOrWhiteSpace(body.Category) ? "FOOD" : body.Category.Trim().ToUpperInvariant();
        item.Stock = body.Stock;
        item.IsAvailable = body.IsAvailable;
        await _db.SaveChangesAsync();
        return ServiceResult.Ok(item);
    }

    public async Task<ServiceResult> DeleteSnackAsync(int id)
    {
        var item = await _db.Snacks.FirstOrDefaultAsync(x => x.Id == id);
        if (item == null) return ServiceResult.NotFound("Khong tim thay do an");

        var hasBookingSnacks = await _db.BookingSnacks.AnyAsync(x => x.SnackId == id);
        if (hasBookingSnacks)
        {
            return ServiceResult.BadRequest("Khong the xoa do an da duoc dat");
        }

        _db.Snacks.Remove(item);
        await _db.SaveChangesAsync();
        return ServiceResult.Ok(new { success = true });
    }

    public async Task<object> GetUsersAsync()
    {
        var users = await _db.Users.AsNoTracking().OrderBy(x => x.Id).Select(x => new
        {
            x.Id,
            x.Username,
            x.Email,
            x.FullName,
            x.Role
        }).ToListAsync();
        return users;
    }

    public async Task<object> GetRevenueAsync(string? from, string? to)
    {
        var fromDate = DateOnly.TryParse(from, out var f) ? f.ToDateTime(TimeOnly.MinValue) : DateTime.UtcNow.Date.AddDays(-30);
        var toDate = DateOnly.TryParse(to, out var t) ? t.ToDateTime(TimeOnly.MaxValue) : DateTime.UtcNow;

        var bookings = await _db.Bookings
            .AsNoTracking()
            .Where(x => x.Status == "CONFIRMED" && x.CreatedAt >= fromDate && x.CreatedAt <= toDate)
            .ToListAsync();

        var byDate = bookings
            .GroupBy(x => x.CreatedAt.Date)
            .ToDictionary(g => g.Key.ToString("yyyy-MM-dd"), g => g.Sum(x => x.TotalPrice));

        return new
        {
            totalRevenue = bookings.Sum(x => x.TotalPrice),
            totalTickets = bookings.Count,
            byDate
        };
    }

    public async Task<object> GetTopMoviesAsync(int? limit)
    {
        var top = await _db.Bookings
            .AsNoTracking()
            .Where(x => x.Status == "CONFIRMED")
            .Where(x => x.Showtime != null && x.Showtime.Movie != null)
            .GroupBy(x => new { x.Showtime!.MovieId, Title = x.Showtime.Movie != null ? x.Showtime.Movie.Title : string.Empty })
            .Select(g => new
            {
                movieId = g.Key.MovieId,
                title = g.Key.Title,
                revenue = g.Sum(x => x.TotalPrice)
            })
            .OrderByDescending(x => x.revenue)
            .Take(Math.Clamp(limit ?? 10, 1, 20))
            .ToListAsync();
        return top;
    }
}
