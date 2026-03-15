using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Monoplist.Data;
using System.Security.Claims;

namespace Monoplist.Pages.Settings;

[Authorize]
public class IndexModel : PageModel
{
    private readonly AppDbContext _context;

    public IndexModel(AppDbContext context)
    {
        _context = context;
    }

    public string Language { get; set; } = "ru";
    public bool CompactMode { get; set; }
    public bool Animations { get; set; } = true;
    public string Theme { get; set; } = "light";
    public string CustomColor { get; set; } = "#FF6B00";

    public async Task OnGetAsync()
    {
        try
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
        catch (Exception ex)
        {
            // Логирование ошибки (можно добавить _logger, если есть)
            TempData["Error"] = GetLocalizedMessage(
                "Не удалось загрузить настройки пользователя.",
                "Failed to load user settings.",
                "Пайдаланушы параметрлерін жүктеу мүмкін болмады.");
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