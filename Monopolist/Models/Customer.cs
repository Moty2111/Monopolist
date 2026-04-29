using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Monoplist.Models
{
    public class Customer
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Поле FullName обязательно")]
        [StringLength(200, MinimumLength = 2)]
        public string FullName { get; set; } = string.Empty;

        [Phone]
        [StringLength(50)]
        public string? Phone { get; set; }

        [EmailAddress]
        [StringLength(200)]
        public string? Email { get; set; }

        [StringLength(200, MinimumLength = 4)]
        public string Password { get; set; } = string.Empty;

        [Range(0, 100)]
        public decimal Discount { get; set; } = 0;        // ручная скидка (админ)

        public decimal LoyaltyDiscount { get; set; } = 0; // автоматическая по заказам
        public int TotalCompletedOrders { get; set; } = 0; // количество завершённых заказов
        public decimal TotalSpent { get; set; } = 0;       // общая сумма завершённых заказов

        [NotMapped]
        public decimal EffectiveDiscount => Math.Min(Discount + LoyaltyDiscount, 100);

        public string? AvatarUrl { get; set; }
        public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}