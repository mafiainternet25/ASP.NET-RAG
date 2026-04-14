using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace web.Models;

public class Movie
{
    public int Id { get; set; }

    [MaxLength(180)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(80)]
    public string? Genre { get; set; }

    public int? DurationMin { get; set; }

    [MaxLength(500)]
    public string? PosterUrl { get; set; }

    [MaxLength(500)]
    public string? TrailerUrl { get; set; }

    [MaxLength(20)]
    public string Status { get; set; } = "NOW_SHOWING";

    [Column(TypeName = "decimal(3,1)")]
    public decimal? Rating { get; set; }

    public string? Description { get; set; }
}
