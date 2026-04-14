using Microsoft.EntityFrameworkCore;
using web.Data;
using web.Models;

namespace web.Services;

public class BookingService
{
    private readonly ApplicationDbContext _db;

    public BookingService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<ServiceResult> CreateAsync(User user, CreateBookingRequest body)
    {
        if (body.SeatIds == null || body.SeatIds.Length == 0)
        {
            return ServiceResult.BadRequest("Vui long chon it nhat 1 ghe");
        }

        var seatIds = body.SeatIds.Distinct().ToList();

        var showtime = await _db.Showtimes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == body.ShowtimeId);
        if (showtime == null)
        {
            return ServiceResult.BadRequest("Khong tim thay suat chieu");
        }

        var conflict = await _db.BookingSeats
            .AnyAsync(bs => bs.Booking != null
                            && bs.Booking.ShowtimeId == body.ShowtimeId
                            && bs.Booking.Status != "CANCELLED"
                            && seatIds.Contains(bs.SeatId));
        if (conflict)
        {
            return ServiceResult.BadRequest("Mot so ghe da duoc dat");
        }

        var seats = await _db.Seats.Where(x => seatIds.Contains(x.Id) && x.RoomId == showtime.RoomId).ToListAsync();
        if (seats.Count != seatIds.Count)
        {
            return ServiceResult.BadRequest("Ghe khong hop le");
        }

        var snackItems = (body.Snacks ?? Array.Empty<CreateBookingSnackRequest>())
            .Where(x => x.SnackId > 0 && x.Quantity > 0)
            .GroupBy(x => x.SnackId)
            .Select(x => new
            {
                SnackId = x.Key,
                Quantity = x.Sum(item => item.Quantity)
            })
            .ToList();

        var snackLookup = new Dictionary<int, Snack>();
        if (snackItems.Count > 0)
        {
            var snackIds = snackItems.Select(x => x.SnackId).ToList();
            var snacks = await _db.Snacks.Where(x => snackIds.Contains(x.Id)).ToListAsync();
            if (snacks.Count != snackIds.Count)
            {
                return ServiceResult.BadRequest("Mot so do an khong hop le");
            }

            snackLookup = snacks.ToDictionary(x => x.Id);

            foreach (var item in snackItems)
            {
                var snack = snackLookup[item.SnackId];
                if (!snack.IsAvailable || snack.Stock < item.Quantity)
                {
                    return ServiceResult.BadRequest($"{snack.Name} khong du so luong hoac khong kha dung");
                }
            }
        }

        var snackTotal = snackItems.Sum(item => snackLookup[item.SnackId].Price * item.Quantity);
        decimal total = seats.Sum(s => showtime.Price + s.ExtraPrice) + snackTotal;
        Promotion? promotion = null;

        if (!string.IsNullOrWhiteSpace(body.PromotionCode))
        {
            var code = body.PromotionCode.Trim().ToUpper();
            promotion = await _db.Promotions.FirstOrDefaultAsync(x => x.Code == code && x.ValidFrom <= DateTime.UtcNow && x.ValidTo >= DateTime.UtcNow && x.UsedCount < x.MaxUses);
            if (promotion != null)
            {
                total = total * (100 - promotion.DiscountPercent) / 100m;
            }
        }

        var bookingCode = "BK" + Guid.NewGuid().ToString("N")[..8].ToUpper();

        await using var transaction = await _db.Database.BeginTransactionAsync();

        var booking = new Booking
        {
            UserId = user.Id,
            ShowtimeId = body.ShowtimeId,
            BookingCode = bookingCode,
            Status = "PENDING",
            TotalPrice = Math.Round(total, 0),
            PromotionId = promotion?.Id
        };
        _db.Bookings.Add(booking);
        await _db.SaveChangesAsync();

        var bookingSeats = seats.Select(s => new BookingSeat
        {
            BookingId = booking.Id,
            SeatId = s.Id,
            Price = showtime.Price + s.ExtraPrice
        });
        _db.BookingSeats.AddRange(bookingSeats);

        foreach (var item in snackItems)
        {
            var snack = snackLookup[item.SnackId];
            snack.Stock -= item.Quantity;
            _db.BookingSnacks.Add(new BookingSnack
            {
                BookingId = booking.Id,
                SnackId = snack.Id,
                Quantity = item.Quantity,
                Price = snack.Price
            });
        }

        if (promotion != null)
        {
            promotion.UsedCount += 1;
        }

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return ServiceResult.Ok(new
        {
            bookingId = booking.Id,
            bookingCode = booking.BookingCode,
            totalPrice = booking.TotalPrice,
            snackTotalPrice = Math.Round(snackTotal, 0)
        });
    }

    public async Task<object> GetMyBookingsAsync(int userId)
    {
        var list = await _db.Bookings
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                x.Id,
                x.BookingCode,
                x.Status,
                x.TotalPrice,
                x.ShowtimeId,
                movieId = x.Showtime != null ? x.Showtime.MovieId : 0,
                movieTitle = x.Showtime != null && x.Showtime.Movie != null ? x.Showtime.Movie.Title : string.Empty,
                roomName = x.Showtime != null && x.Showtime.Room != null ? x.Showtime.Room.Name : string.Empty,
                cinemaName = x.Showtime != null && x.Showtime.Room != null && x.Showtime.Room.Cinema != null ? x.Showtime.Room.Cinema.Name : string.Empty,
                showtimeStartTime = x.Showtime != null ? x.Showtime.StartTime : default,
                seatIds = _db.BookingSeats
                    .Where(bs => bs.BookingId == x.Id)
                    .OrderBy(bs => bs.Seat != null ? bs.Seat.SeatRow : string.Empty)
                    .ThenBy(bs => bs.Seat != null ? bs.Seat.SeatNumber : 0)
                    .Select(bs => bs.SeatId)
                    .ToArray(),
                seatLabels = _db.BookingSeats
                    .Where(bs => bs.BookingId == x.Id)
                    .OrderBy(bs => bs.Seat != null ? bs.Seat.SeatRow : string.Empty)
                    .ThenBy(bs => bs.Seat != null ? bs.Seat.SeatNumber : 0)
                    .Select(bs => bs.Seat != null ? bs.Seat.SeatRow + bs.Seat.SeatNumber : string.Empty)
                    .ToArray(),
                snacks = _db.BookingSnacks
                    .Where(bs => bs.BookingId == x.Id)
                    .OrderBy(bs => bs.Snack != null ? bs.Snack.Name : string.Empty)
                    .Select(bs => new
                    {
                        bs.SnackId,
                        name = bs.Snack != null ? bs.Snack.Name : string.Empty,
                        bs.Quantity,
                        bs.Price,
                        totalPrice = bs.Price * bs.Quantity
                    })
                    .ToArray()
            })
            .ToListAsync();

        return list;
    }

    public async Task<ServiceResult> GetDetailByCodeAsync(User user, string bookingCode)
    {
        var booking = await _db.Bookings.AsNoTracking().FirstOrDefaultAsync(x => x.BookingCode == bookingCode);
        if (booking == null)
        {
            return ServiceResult.NotFound("Khong tim thay don");
        }

        if (booking.UserId != user.Id && user.Role != "ADMIN")
        {
            return ServiceResult.Forbidden();
        }

        var detail = await _db.Bookings
            .AsNoTracking()
            .Where(x => x.Id == booking.Id)
            .Select(x => new
            {
                x.Id,
                x.BookingCode,
                x.Status,
                x.TotalPrice,
                movieTitle = x.Showtime != null && x.Showtime.Movie != null ? x.Showtime.Movie.Title : string.Empty,
                cinemaName = x.Showtime != null && x.Showtime.Room != null && x.Showtime.Room.Cinema != null ? x.Showtime.Room.Cinema.Name : string.Empty,
                roomName = x.Showtime != null && x.Showtime.Room != null ? x.Showtime.Room.Name : string.Empty,
                showtimeStartTime = x.Showtime != null ? x.Showtime.StartTime : default,
                snacks = _db.BookingSnacks
                    .Where(bs => bs.BookingId == x.Id)
                    .OrderBy(bs => bs.Snack != null ? bs.Snack.Name : string.Empty)
                    .Select(bs => new
                    {
                        bs.SnackId,
                        name = bs.Snack != null ? bs.Snack.Name : string.Empty,
                        bs.Quantity,
                        bs.Price,
                        totalPrice = bs.Price * bs.Quantity
                    })
                    .ToArray()
            })
            .FirstAsync();

        return ServiceResult.Ok(detail);
    }

    public async Task<ServiceResult> CancelAsync(User user, int id)
    {
        var booking = await _db.Bookings.FirstOrDefaultAsync(x => x.Id == id);
        if (booking == null)
        {
            return ServiceResult.NotFound("Khong tim thay don", "error");
        }

        if (booking.UserId != user.Id && user.Role != "ADMIN")
        {
            return ServiceResult.Forbidden();
        }

        if (booking.Status == "CANCELLED")
        {
            return ServiceResult.BadRequest("Don da bi huy");
        }

        if (booking.Status == "CONFIRMED")
        {
            return ServiceResult.BadRequest("Don da thanh toan khong the huy");
        }

        await using var transaction = await _db.Database.BeginTransactionAsync();

        var bookingSnacks = await _db.BookingSnacks
            .Include(x => x.Snack)
            .Where(x => x.BookingId == id)
            .ToListAsync();

        foreach (var bookingSnack in bookingSnacks)
        {
            if (bookingSnack.Snack != null)
            {
                bookingSnack.Snack.Stock += bookingSnack.Quantity;
            }
        }

        booking.Status = "CANCELLED";
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
        return ServiceResult.Ok(new { success = true });
    }

    public async Task<ServiceResult> ConfirmAsync(User user, int id)
    {
        var booking = await _db.Bookings.FirstOrDefaultAsync(x => x.Id == id);
        if (booking == null)
        {
            return ServiceResult.NotFound("Khong tim thay don");
        }

        if (booking.UserId != user.Id && user.Role != "ADMIN")
        {
            return ServiceResult.Forbidden();
        }

        if (booking.Status == "CANCELLED")
        {
            return ServiceResult.BadRequest("Don da huy");
        }

        booking.Status = "CONFIRMED";
        await _db.SaveChangesAsync();

        return ServiceResult.Ok(new
        {
            success = true,
            booking = new
            {
                booking.Id,
                booking.BookingCode,
                booking.Status,
                booking.TotalPrice
            }
        });
    }
}
