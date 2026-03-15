using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Monoplist.Data;
using Monoplist.ViewModels;
using System.Security.Claims;

namespace Monoplist.Pages.Settings;

[Authorize(Roles = "Admin")]
public class ActivityLogModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly ILogger<ActivityLogModel> _logger;

    public ActivityLogModel(AppDbContext context, ILogger<ActivityLogModel> logger)
    {
        _context = context;
        _logger = logger;
    }

    [BindProperty(SupportsGet = true)]
    public string? UserFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? ActionFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? DateFrom { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? DateTo { get; set; }

    public List<ActivityLogViewModel> Logs { get; set; } = new();

    // Свойства для персонализации
    public string Language { get; set; } = "ru";
    public bool CompactMode { get; set; }
    public bool Animations { get; set; } = true;
    public string Theme { get; set; } = "light";
    public string CustomColor { get; set; } = "#FF6B00";

    public async Task OnGetAsync()
    {
        await LoadUserSettings();

        try
        {
            // В реальном проекте здесь должен быть запрос к базе данных
            // Например: _context.ActivityLogs.Where(...).ToListAsync()
            var allLogs = GetDemoLogs();

            // Применяем фильтры
            var filteredLogs = allLogs.AsEnumerable();

            if (!string.IsNullOrEmpty(UserFilter))
            {
                filteredLogs = filteredLogs.Where(l => l.Username.Contains(UserFilter, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(ActionFilter))
            {
                filteredLogs = filteredLogs.Where(l => l.Action.Contains(ActionFilter, StringComparison.OrdinalIgnoreCase));
            }

            if (DateFrom.HasValue)
            {
                filteredLogs = filteredLogs.Where(l => l.Timestamp.Date >= DateFrom.Value.Date);
            }

            if (DateTo.HasValue)
            {
                filteredLogs = filteredLogs.Where(l => l.Timestamp.Date <= DateTo.Value.Date);
            }

            Logs = filteredLogs.OrderByDescending(l => l.Timestamp).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при загрузке журнала активности");
            TempData["Error"] = GetLocalizedMessage(
                "Не удалось загрузить журнал активности.",
                "Failed to load activity log.",
                "Әрекеттер журналын жүктеу мүмкін болмады.");
        }
    }

    private List<ActivityLogViewModel> GetDemoLogs()
    {
        // Демо-данные для разработки
        return new List<ActivityLogViewModel>
        {
            new ActivityLogViewModel
            {
                Id = 1,
                Username = "admin",
                Action = "Вход в систему",
                Details = "Успешный вход с IP 192.168.1.100",
                IpAddress = "192.168.1.100",
                Timestamp = DateTime.Now.AddMinutes(-5)
            },
            new ActivityLogViewModel
            {
                Id = 2,
                Username = "manager",
                Action = "Создание заказа",
                Details = "Создан заказ ORD-2025-001",
                IpAddress = "192.168.1.101",
                Timestamp = DateTime.Now.AddHours(-1)
            },
            new ActivityLogViewModel
            {
                Id = 3,
                Username = "seller",
                Action = "Добавление товара",
                Details = "Добавлен товар 'Цемент М500'",
                IpAddress = "192.168.1.102",
                Timestamp = DateTime.Now.AddHours(-3)
            },
            new ActivityLogViewModel
            {
                Id = 4,
                Username = "admin",
                Action = "Удаление пользователя",
                Details = "Удален пользователь 'test_user'",
                IpAddress = "192.168.1.100",
                Timestamp = DateTime.Now.AddDays(-1)
            },
            new ActivityLogViewModel
            {
                Id = 5,
                Username = "admin",
                Action = "Редактирование товара",
                Details = "Изменена цена товара 'Цемент М500'",
                IpAddress = "192.168.1.100",
                Timestamp = DateTime.Now.AddDays(-2)
            },
            new ActivityLogViewModel
            {
                Id = 6,
                Username = "manager",
                Action = "Вход в систему",
                Details = "Успешный вход с IP 192.168.1.101",
                IpAddress = "192.168.1.101",
                Timestamp = DateTime.Now.AddDays(-3)
            }
        };
    }

    private async Task LoadUserSettings()
    {
        var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
        var user = await _context.Users.FindAsync(userId);
        if (user != null)
        {
            Language = user.Language ?? "ru";
            CompactMode = user.CompactMode;
            Animations = user.Animations;
            Theme = user.Theme ?? "light";
            CustomColor = user.CustomColor ?? "#FF6B00";
        }
    }

    private string GetLocalizedMessage(string ru, string en, string kk)
    {
        return Language switch
        {
            "en" => en,
            "kk" => kk,
            _ => ru
        };
    }
}