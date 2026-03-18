using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Monoplist.Data;
using System.Security.Claims;

namespace Monoplist.Pages.Account;

[AllowAnonymous] // чтобы даже неаутентифицированные могли видеть (хотя обычно они не попадают)
public class AccessDeniedModel : PageModel
{
    private readonly AppDbContext _context;

    public AccessDeniedModel(AppDbContext context)
    {
        _context = context;
    }

    // Свойства для персонализации
    public string Language { get; set; } = "ru";
    public bool CompactMode { get; set; }
    public bool Animations { get; set; } = true;
    public string Theme { get; set; } = "light";
    public string CustomColor { get; set; } = "#FF6B00";

    public async Task OnGet()
    {
        await LoadUserSettings();
    }

    private async Task LoadUserSettings()
    {
        // Пытаемся получить настройки пользователя, если он аутентифицирован
        var userIdClaim = User.FindFirst("UserId");
        if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
        {
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
        // Если пользователь не аутентифицирован, оставляем значения по умолчанию
    }
}