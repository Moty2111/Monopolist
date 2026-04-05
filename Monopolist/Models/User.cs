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

    // Новые поля для профиля
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

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public string? AvatarUrl { get; set; }

    // === ДЛЯ 2FA ===
    public string? TwoFactorSecret { get; set; }      // Секретный ключ для TOTP (сохраняется в БД)
    public bool TwoFactorEnabled { get; set; }         // Включена ли 2FA
     
    // Поля для сброса пароля
    public string? ResetToken { get; set; }
    public DateTime? ResetTokenExpiry { get; set; }
}