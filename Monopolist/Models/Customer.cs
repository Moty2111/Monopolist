// Models/Customer.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Monoplist.Models;

[Index(nameof(Email), IsUnique = true)]
[Index(nameof(Phone))]
public class Customer
{
    public int Id { get; set; }

    [Required, StringLength(200, MinimumLength = 2)]
    public string FullName { get; set; } = string.Empty;

    [StringLength(20)]
    public string? Phone { get; set; }

    [StringLength(100), EmailAddress]
    public string? Email { get; set; }

    [Required, StringLength(100, MinimumLength = 4)]
    public string Password { get; set; } = string.Empty;

    [Column(TypeName = "decimal(5,2)")]
    public decimal Discount { get; set; } = 0;

    public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Новое поле – URL аватара
    [StringLength(500)]
    public string? AvatarUrl { get; set; }

    public ICollection<Order> Orders { get; set; } = new List<Order>();
}