using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Monoplist.Models;

[Index(nameof(Username), IsUnique = true)]
public class User
{
    public int Id { get; set; }

    [Required, StringLength(50, MinimumLength = 3)]
    public string Username { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 4)]
    public string Password { get; set; } = string.Empty;

    [Required, StringLength(20)]
    public string Role { get; set; } = "Seller";

    // Профиль
    [StringLength(100)]
    public string? FullName { get; set; }

    [EmailAddress, StringLength(100)]
    public string? Email { get; set; }

    [Phone, StringLength(20)]
    public string? PhoneNumber { get; set; }

    [StringLength(50)]
    public string? Position { get; set; }

    public DateTime? LastLoginAt { get; set; }

    // Настройки внешнего вида
    public string? Theme { get; set; } = "light";
    public string? Language { get; set; } = "ru";
    public bool CompactMode { get; set; }
    public bool Animations { get; set; } = true;
    public string? CustomColor { get; set; } = "#FF6B00";

    // === УВЕДОМЛЕНИЯ ===
    public bool OrderNotifications { get; set; } = true;
    public bool StockNotifications { get; set; } = true;
    public bool CustomerNotifications { get; set; } = true;
    public bool DailyReport { get; set; } = false;

    // === 2FA ===
    public string? TwoFactorSecret { get; set; }
    public bool TwoFactorEnabled { get; set; }

    // Сброс пароля
    public string? ResetToken { get; set; }
    public DateTime? ResetTokenExpiry { get; set; }

    // Временные метки
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Аватар
    public string? AvatarUrl { get; set; }
}