using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Monoplist.Models;

[Index(nameof(Article), IsUnique = true)]
[Index(nameof(Name))]
public class Product
{
    public int Id { get; set; }

    [Required, StringLength(200, MinimumLength = 3)]
    public string Name { get; set; } = string.Empty;

    [StringLength(50)]
    public string? Article { get; set; }

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

    public int? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    public int MinimumStock { get; set; } = 10;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    // Добавьте эти поля в существующий класс Product
    public int? WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }
}