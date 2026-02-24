using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Monoplist.Models;

namespace Monoplist.Models;

[Index(nameof(OrderId), nameof(ProductId))]
public class OrderItem
{
    public int Id { get; set; }

    public int OrderId { get; set; }
    public Order? Order { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal PriceAtSale { get; set; }

    [NotMapped]
    public decimal TotalPrice => Quantity * PriceAtSale;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}