using System.ComponentModel.DataAnnotations;

namespace web.Models;

public class Promotion
{
    public int Id { get; set; }

    [MaxLength(40)]
    public string Code { get; set; } = string.Empty;

    [MaxLength(250)]
    public string? Description { get; set; }

    public int DiscountPercent { get; set; }

    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
    public int MaxUses { get; set; }
    public int UsedCount { get; set; }
}
