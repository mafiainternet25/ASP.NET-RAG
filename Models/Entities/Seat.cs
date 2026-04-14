using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace web.Models;

public class Seat
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public Room? Room { get; set; }

    [MaxLength(4)]
    public string SeatRow { get; set; } = "A";

    public int SeatNumber { get; set; }

    [MaxLength(20)]
    public string SeatType { get; set; } = "NORMAL";

    [Column(TypeName = "decimal(12,2)")]
    public decimal ExtraPrice { get; set; }
}
