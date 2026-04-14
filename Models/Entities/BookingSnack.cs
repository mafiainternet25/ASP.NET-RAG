using System.ComponentModel.DataAnnotations.Schema;

namespace web.Models;

public class BookingSnack
{
    public int Id { get; set; }

    public int BookingId { get; set; }
    public Booking? Booking { get; set; }

    public int SnackId { get; set; }
    public Snack? Snack { get; set; }

    public int Quantity { get; set; } = 1;

    [Column(TypeName = "decimal(12,2)")]
    public decimal Price { get; set; }
}