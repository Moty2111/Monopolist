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

        // Загружаем настройки уведомлений из куки
        Input.EmailNotifications = Request.Cookies[$"notify_email_{userId}"] == "true";
        Input.OrderNotifications = Request.Cookies[$"notify_order_{userId}"] == "true";
        Input.StockNotifications = Request.Cookies[$"notify_stock_{userId}"] == "true";
        Input.CustomerNotifications = Request.Cookies[$"notify_customer_{userId}"] == "true";
        Input.DailyReport = Request.Cookies[$"notify_daily_{userId}"] == "true";

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

        try
        {
            var cookieOptions = new CookieOptions
            {
                Expires = DateTime.Now.AddYears(1),
                HttpOnly = true,
                SameSite = SameSiteMode.Lax
            };

            Response.Cookies.Append($"notify_email_{userId}", Input.EmailNotifications.ToString(), cookieOptions);
            Response.Cookies.Append($"notify_order_{userId}", Input.OrderNotifications.ToString(), cookieOptions);
            Response.Cookies.Append($"notify_stock_{userId}", Input.StockNotifications.ToString(), cookieOptions);
            Response.Cookies.Append($"notify_customer_{userId}", Input.CustomerNotifications.ToString(), cookieOptions);
            Response.Cookies.Append($"notify_daily_{userId}", Input.DailyReport.ToString(), cookieOptions);

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