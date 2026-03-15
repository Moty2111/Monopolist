using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Monoplist.Data;
using Monoplist.ViewModels;
using System.Security.Claims;

namespace Monoplist.Pages.Settings;

[Authorize]
public class AppearanceModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly ILogger<AppearanceModel> _logger;

    public AppearanceModel(AppDbContext context, ILogger<AppearanceModel> logger)
    {
        _context = context;
        _logger = logger;
    }

    [BindProperty]
    public AppearanceViewModel Input { get; set; } = new();

    // Свойства для персонализации (используются в представлении)
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

        // Загружаем настройки внешнего вида из базы данных
        Input.Theme = user.Theme ?? "light";
        Input.Language = user.Language ?? "ru";
        Input.CompactMode = user.CompactMode;
        Input.Animations = user.Animations;
        Input.CustomColor = user.CustomColor ?? "#FF6B00";

        // Копируем в свойства страницы для использования в представлении (локализация, классы body)
        Language = Input.Language;
        CompactMode = Input.CompactMode;
        Animations = Input.Animations;
        Theme = Input.Theme;
        CustomColor = Input.CustomColor;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            // Если ошибка, копируем текущие значения обратно в свойства страницы
            Language = Input.Language;
            CompactMode = Input.CompactMode;
            Animations = Input.Animations;
            Theme = Input.Theme;
            CustomColor = Input.CustomColor;
            return Page();
        }

        var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return NotFound();

        // Обновляем настройки пользователя
        user.Theme = Input.Theme;
        user.Language = Input.Language;
        user.CompactMode = Input.CompactMode;
        user.Animations = Input.Animations;
        user.CustomColor = Input.CustomColor;
        user.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _context.SaveChangesAsync();

            // Сохраняем настройки в куки для быстрого доступа на клиенте
            var cookieOptions = new CookieOptions
            {
                Expires = DateTime.Now.AddYears(1),
                HttpOnly = true,
                SameSite = SameSiteMode.Lax
            };
            Response.Cookies.Append("theme", Input.Theme, cookieOptions);
            Response.Cookies.Append("language", Input.Language, cookieOptions);
            Response.Cookies.Append("compactMode", Input.CompactMode.ToString(), cookieOptions);
            Response.Cookies.Append("animations", Input.Animations.ToString(), cookieOptions);
            Response.Cookies.Append("customColor", Input.CustomColor, cookieOptions);

            TempData["Success"] = GetLocalizedMessage(
                "Настройки внешнего вида сохранены",
                "Appearance settings saved",
                "Сыртқы түр параметрлері сақталды");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при сохранении настроек");
            ModelState.AddModelError(string.Empty, GetLocalizedMessage(
                "Произошла ошибка при сохранении.",
                "An error occurred while saving.",
                "Сақтау кезінде қате орын алды."));
            Language = Input.Language;
            CompactMode = Input.CompactMode;
            Animations = Input.Animations;
            Theme = Input.Theme;
            CustomColor = Input.CustomColor;
            return Page();
        }

        return RedirectToPage();
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