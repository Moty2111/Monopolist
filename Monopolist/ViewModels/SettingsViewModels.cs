// ViewModels/SettingsViewModels.cs (добавлено CustomColor)
using System.ComponentModel.DataAnnotations;

namespace Monoplist.ViewModels;

public class ProfileViewModel
{
    public int Id { get; set; }
    [Required(ErrorMessage = "Имя пользователя обязательно")]
    [StringLength(50, MinimumLength = 3)]
    [Display(Name = "Имя пользователя")]
    public string Username { get; set; } = string.Empty;
    [Display(Name = "Текущий пароль")]
    [DataType(DataType.Password)]
    public string? CurrentPassword { get; set; }
    [Display(Name = "Новый пароль")]
    [StringLength(100, MinimumLength = 6)]
    [DataType(DataType.Password)]
    public string? NewPassword { get; set; }
    [Display(Name = "Подтверждение пароля")]
    [DataType(DataType.Password)]
    [Compare("NewPassword", ErrorMessage = "Пароли не совпадают")]
    public string? ConfirmPassword { get; set; }
    [Display(Name = "Email")]
    [EmailAddress]
    public string? Email { get; set; }
    [Display(Name = "Полное имя")]
    [StringLength(100)]
    public string? FullName { get; set; }
    [Display(Name = "Должность")]
    [StringLength(50)]
    public string? Position { get; set; }
    [Display(Name = "Телефон")]
    [Phone]
    public string? PhoneNumber { get; set; }
    [Display(Name = "Аватар (URL)")]
    [Url]
    public string? AvatarUrl { get; set; }
    public string Role { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}

public class SecurityViewModel
{
    public bool TwoFactorEnabled { get; set; }
    public bool EmailConfirmed { get; set; }
    public bool PhoneConfirmed { get; set; }
    public List<SessionInfo> ActiveSessions { get; set; } = new();
}

public class SessionInfo
{
    public string Id { get; set; } = string.Empty;
    public string Device { get; set; } = string.Empty;
    public string Browser { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public DateTime LoginTime { get; set; }
    public bool IsCurrent { get; set; }
}

public class AppearanceViewModel
{
    [Display(Name = "Тема")]
    public string Theme { get; set; } = "light";

    [Display(Name = "Основной цвет")]
    public string CustomColor { get; set; } = "#FF6B00";

    [Display(Name = "Язык")]
    public string Language { get; set; } = "ru";

    [Display(Name = "Компактный режим")]
    public bool CompactMode { get; set; }

    [Display(Name = "Анимации")]
    public bool Animations { get; set; } = true;
}

public class NotificationsViewModel
{
    [Display(Name = "Email уведомления")]
    public bool EmailNotifications { get; set; } = true;
    [Display(Name = "Уведомления о новых заказах")]
    public bool OrderNotifications { get; set; } = true;
    [Display(Name = "Уведомления о низком остатке")]
    public bool StockNotifications { get; set; } = true;
    [Display(Name = "Уведомления о новых клиентах")]
    public bool CustomerNotifications { get; set; } = false;
    [Display(Name = "Ежедневный отчет")]
    public bool DailyReport { get; set; } = false;
}

public class UserSettingsViewModel
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? FullName { get; set; }
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class RoleViewModel
{
    public string Name { get; set; } = string.Empty;
    public int UserCount { get; set; }
    public List<string> Permissions { get; set; } = new();
}

public class ActivityLogViewModel
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}