using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Monoplist.Models;

[Index(nameof(Name), IsUnique = true)]
public class Supplier
{
    public int Id { get; set; }

    [Required, StringLength(200, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string ContactInfo { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<Product> Products { get; set; } = new List<Product>();
}