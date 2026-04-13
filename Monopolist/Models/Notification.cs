// Models/Notification.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace Monoplist.Models;

public class Notification
{
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }
    public User? User { get; set; }

    [Required, StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(500)]
    public string Message { get; set; } = string.Empty;

    public string? Link { get; set; }          // Ссылка для перехода (например, на заказ)

    public NotificationType Type { get; set; } = NotificationType.Info;

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum NotificationType
{
    Info,
    Success,
    Warning,
    Error,
    Order,
    Stock,
    Customer
}