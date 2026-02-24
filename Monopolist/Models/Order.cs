using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Monoplist.Models;

[Index(nameof(OrderNumber), IsUnique = true)]
[Index(nameof(OrderDate))]
[Index(nameof(Status))]
public class Order
{
    public int Id { get; set; }

    [Required, StringLength(20, MinimumLength = 5)]
    public string OrderNumber { get; set; } = string.Empty;

    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public DateTime OrderDate { get; set; } = DateTime.UtcNow;

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }

    [Required, StringLength(20)]
    public string Status { get; set; } = "Pending";

    [StringLength(50)]
    public string? PaymentMethod { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}