using System.ComponentModel.DataAnnotations.Schema;

namespace web.Models;

public class Showtime
{
    public int Id { get; set; }
    public int MovieId { get; set; }
    public Movie? Movie { get; set; }

    public int RoomId { get; set; }
    public Room? Room { get; set; }

    public DateTime StartTime { get; set; }

    [Column(TypeName = "decimal(12,2)")]
    public decimal Price { get; set; }
}
