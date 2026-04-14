using Microsoft.EntityFrameworkCore;
using web.Data;
using web.Models;

namespace web.Services;

public class PaymentService
{
    private readonly ApplicationDbContext _db;

    public PaymentService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<ServiceResult> SimplePaymentAsync(User currentUser, int bookingId)
    {
        var booking = await _db.Bookings.FirstOrDefaultAsync(x => x.Id == bookingId);
        if (booking == null)
        {
            return ServiceResult.NotFound("Khong tim thay don", "message");
        }

        if (booking.UserId != currentUser.Id && currentUser.Role != "ADMIN")
        {
            return ServiceResult.Forbidden();
        }

        if (booking.Status != "PENDING")
        {
            return ServiceResult.BadRequest("Booking da thanh toan hoac da huy");
        }

        booking.Status = "CONFIRMED";
        await _db.SaveChangesAsync();

        var transactionId = $"SIMPLE_{bookingId}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
        return ServiceResult.Ok(new
        {
            success = "true",
            message = "Thanh toan thanh cong",
            transactionId
        });
    }
}
