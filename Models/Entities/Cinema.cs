using System.ComponentModel.DataAnnotations;

namespace web.Models;

public class Cinema
{
    public int Id { get; set; }

    [MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(250)]
    public string? Address { get; set; }

    [MaxLength(80)]
    public string? City { get; set; }
}
