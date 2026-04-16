// Models/Product.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Monoplist.Models;

[Index(nameof(Article), IsUnique = true)]
public class Product
{
    public int Id { get; set; }

    [Required, StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string Article { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }  // <-- добавлено

    [Required]
    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    [Required, StringLength(20)]
    public string Unit { get; set; } = "шт";

    [Column(TypeName = "decimal(18,2)")]
    public decimal PurchasePrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal SalePrice { get; set; }

    public int CurrentStock { get; set; }
    public int MinimumStock { get; set; }

    public int? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    public int? WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }

    [StringLength(500)]
    public string? ImageUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}