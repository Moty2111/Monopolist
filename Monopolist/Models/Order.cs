using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Monoplist.Models
{
    public class Order
    {
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string OrderNumber { get; set; } = string.Empty;

        public int CustomerId { get; set; }
        public Customer Customer { get; set; } = null!;

        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        public decimal TotalAmount { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "Pending";

        [StringLength(20)]
        public string? PaymentMethod { get; set; }

        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public DateTime UpdatedAt { get; internal set; }
    }
}