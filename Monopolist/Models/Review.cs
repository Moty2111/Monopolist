using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Monoplist.Models
{
    public class Review
    {
        public int Id { get; set; }

        public int ProductId { get; set; }
        [ForeignKey("ProductId")]
        public Product Product { get; set; } = null!;

        public int CustomerId { get; set; }
        [ForeignKey("CustomerId")]
        public Customer Customer { get; set; } = null!;

        [Required(ErrorMessage = "Введите текст отзыва")]
        [StringLength(2000, MinimumLength = 2)]
        public string Text { get; set; } = string.Empty;

        [Range(1, 5)]
        public int Rating { get; set; } = 5;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Поля модерации
        public bool IsApproved { get; set; } = false;     // отзыв виден только после одобрения
        public string? AdminReply { get; set; }            // ответ администратора
        public DateTime? RepliedAt { get; set; }          // дата ответа
    }
}