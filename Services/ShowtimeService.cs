using Microsoft.EntityFrameworkCore;
using web.Data;

namespace web.Services;

public class ShowtimeService
{
    private readonly ApplicationDbContext _db;

    public ShowtimeService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<object> GetShowtimesAsync(int? movieId, string? date, int? cinemaId)
    {
        var query = _db.Showtimes
            .AsNoTracking()
            .Include(s => s.Room)
            .ThenInclude(r => r!.Cinema)
            .AsQueryable();

        if (movieId.HasValue)
        {
            query = query.Where(x => x.MovieId == movieId.Value);
        }

        if (cinemaId.HasValue)
        {
            query = query.Where(x => x.Room != null && x.Room.CinemaId == cinemaId.Value);
        }

        if (DateOnly.TryParse(date, out var d))
        {
            var from = d.ToDateTime(TimeOnly.MinValue);
            var to = d.ToDateTime(TimeOnly.MaxValue);
            query = query.Where(x => x.StartTime >= from && x.StartTime <= to);
        }

        return await query
            .Select(s => new
            {
                s.Id,
                s.MovieId,
                s.RoomId,
                s.StartTime,
                s.Price,
                roomName = s.Room != null ? s.Room.Name : string.Empty,
                cinemaName = s.Room != null && s.Room.Cinema != null ? s.Room.Cinema.Name : string.Empty
            })
            .OrderBy(x => x.StartTime)
            .ToListAsync();
    }

    public async Task<ServiceResult> GetSeatsByShowtimeAsync(int showtimeId)
    {
        var showtime = await _db.Showtimes
            .AsNoTracking()
            .Include(s => s.Room)
            .FirstOrDefaultAsync(x => x.Id == showtimeId);
        if (showtime == null)
        {
            return ServiceResult.NotFound("Khong tim thay suat chieu");
        }

        var bookedSeatIds = await _db.BookingSeats
            .Where(bs => bs.Booking != null && bs.Booking.ShowtimeId == showtimeId && bs.Booking.Status != "CANCELLED")
            .Select(bs => bs.SeatId)
            .Distinct()
            .ToHashSetAsync();

        var seats = await _db.Seats
            .AsNoTracking()
            .Where(x => x.RoomId == showtime.RoomId)
            .OrderBy(x => x.SeatRow)
            .ThenBy(x => x.SeatNumber)
            .ToListAsync();

        var grouped = seats
            .GroupBy(x => x.SeatRow)
            .Select(g => new
            {
                row = g.Key,
                seats = g.Select(s => new
                {
                    id = s.Id,
                    number = s.SeatNumber,
                    type = s.SeatType,
                    price = showtime.Price + s.ExtraPrice,
                    status = bookedSeatIds.Contains(s.Id) ? "BOOKED" : "AVAILABLE"
                })
            });

        return ServiceResult.Ok(new
        {
            roomName = showtime.Room != null ? showtime.Room.Name : string.Empty,
            rows = grouped
        });
    }
}
