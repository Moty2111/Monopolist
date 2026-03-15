using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Monoplist.Data;
using Monoplist.ViewModels;
using System.Security.Claims;

namespace Monoplist.Pages.Settings.Users;

[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(AppDbContext context, ILogger<IndexModel> logger)
    {
        _context = context;
        _logger = logger;
    }

    public IList<UserSettingsViewModel> Users { get; set; } = new List<UserSettingsViewModel>();

    // Свойства для персонализации
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
            var currentUser = await _context.Users.FindAsync(userId);
            if (currentUser != null)
            {
                Language = currentUser.Language ?? "ru";
                CompactMode = currentUser.CompactMode;
                Animations = currentUser.Animations;
                Theme = currentUser.Theme ?? "light";
                CustomColor = currentUser.CustomColor ?? "#FF6B00";
            }

            Users = await _context.Users
                .OrderBy(u => u.Username)
                .Select(u => new UserSettingsViewModel
                {
                    Id = u.Id,
                    Username = u.Username,
                    Role = u.Role,
                    IsActive = true, // В реальном проекте можно добавить поле IsActive в модель User
                    CreatedAt = u.CreatedAt,
                    LastLoginAt = u.UpdatedAt
                })
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при загрузке списка пользователей");
            TempData["Error"] = GetLocalizedMessage(
                "Не удалось загрузить список пользователей.",
                "Failed to load users list.",
                "Пайдаланушылар тізімін жүктеу мүмкін болмады.");
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