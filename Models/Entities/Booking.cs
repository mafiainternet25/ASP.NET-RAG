using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace web.Models;

public class Booking
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }

    public int ShowtimeId { get; set; }
    public Showtime? Showtime { get; set; }

    [MaxLength(20)]
    public string BookingCode { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Status { get; set; } = "PENDING";

    [Column(TypeName = "decimal(12,2)")]
    public decimal TotalPrice { get; set; }

    public int? PromotionId { get; set; }
    public Promotion? Promotion { get; set; }

    public ICollection<BookingSnack> BookingSnacks { get; set; } = new List<BookingSnack>();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
