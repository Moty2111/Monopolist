using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Monoplist.Data;
using Monoplist.ViewModels;
using System.Security.Claims;

namespace Monoplist.Pages.Settings;

[Authorize]
public class NotificationsModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly ILogger<NotificationsModel> _logger;

    public NotificationsModel(AppDbContext context, ILogger<NotificationsModel> logger)
    {
        _context = context;
        _logger = logger;
    }

    [BindProperty]
    public NotificationsViewModel Input { get; set; } = new();

    // Свойства для персонализации
    public string Language { get; set; } = "ru";
    public bool CompactMode { get; set; }
    public bool Animations { get; set; } = true;
    public string Theme { get; set; } = "light";
    public string CustomColor { get; set; } = "#FF6B00";

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return NotFound();

        // Загружаем настройки внешнего вида
        Language = user.Language ?? "ru";
        CompactMode = user.CompactMode;
        Animations = user.Animations;
        Theme = user.Theme ?? "light";
        CustomColor = user.CustomColor ?? "#FF6B00";

        // Загружаем настройки уведомлений из БД
        Input.OrderNotifications = user.OrderNotifications;
        Input.StockNotifications = user.StockNotifications;
        Input.CustomerNotifications = user.CustomerNotifications;
        Input.DailyReport = user.DailyReport;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadUserSettings();
            return Page();
        }

        var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return NotFound();

        try
        {
            // Обновляем настройки уведомлений в БД
            user.OrderNotifications = Input.OrderNotifications;
            user.StockNotifications = Input.StockNotifications;
            user.CustomerNotifications = Input.CustomerNotifications;
            user.DailyReport = Input.DailyReport;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] = GetLocalizedMessage(
                "Настройки уведомлений сохранены",
                "Notification settings saved",
                "Хабарландыру параметрлері сақталды");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при сохранении настроек уведомлений");
            ModelState.AddModelError(string.Empty, GetLocalizedMessage(
                "Произошла ошибка при сохранении.",
                "An error occurred while saving.",
                "Сақтау кезінде қате орын алды."));
            await LoadUserSettings();
            return Page();
        }

        return RedirectToPage();
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