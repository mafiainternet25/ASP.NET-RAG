using System.ComponentModel.DataAnnotations;

namespace web.Models;

public class Room
{
    public int Id { get; set; }
    public int CinemaId { get; set; }
    public Cinema? Cinema { get; set; }

    [MaxLength(60)]
    public string Name { get; set; } = string.Empty;

    public int TotalSeats { get; set; }

    [MaxLength(20)]
    public string RoomType { get; set; } = "NORMAL";
}
