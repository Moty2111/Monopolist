using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Monoplist.Data;
using System.Security.Claims;

namespace Monoplist.Pages.Settings.Users;

[Authorize(Roles = "Admin")]
public class DeleteModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly ILogger<DeleteModel> _logger;

    public DeleteModel(AppDbContext context, ILogger<DeleteModel> logger)
    {
        _context = context;
        _logger = logger;
    }

    public class UserInfoModel
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public UserInfoModel UserInfo { get; set; } = new();

    // Свойства для персонализации
    public string Language { get; set; } = "ru";
    public bool CompactMode { get; set; }
    public bool Animations { get; set; } = true;
    public string Theme { get; set; } = "light";
    public string CustomColor { get; set; } = "#FF6B00";

    public async Task<IActionResult> OnGetAsync(int id)
    {
        await LoadUserSettings();

        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            TempData["Error"] = GetLocalizedMessage(
                "Пользователь не найден.",
                "User not found.",
                "Пайдаланушы табылмады.");
            return RedirectToPage("./Index");
        }

        // Запрет на удаление самого себя
        var currentUserId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
        if (user.Id == currentUserId)
        {
            TempData["Error"] = GetLocalizedMessage(
                "Вы не можете удалить собственную учётную запись.",
                "You cannot delete your own account.",
                "Сіз өз аккаунтыңызды жоя алмайсыз.");
            return RedirectToPage("./Index");
        }

        UserInfo.Id = user.Id;
        UserInfo.Username = user.Username;
        UserInfo.Role = user.Role;
        UserInfo.CreatedAt = user.CreatedAt;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            TempData["Error"] = GetLocalizedMessage(
                "Пользователь не найден.",
                "User not found.",
                "Пайдаланушы табылмады.");
            return RedirectToPage("./Index");
        }

        var currentUserId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
        if (user.Id == currentUserId)
        {
            TempData["Error"] = GetLocalizedMessage(
                "Вы не можете удалить собственную учётную запись.",
                "You cannot delete your own account.",
                "Сіз өз аккаунтыңызды жоя алмайсыз.");
            return RedirectToPage("./Index");
        }

        if (user.Role == "Admin")
        {
            var adminCount = await _context.Users.CountAsync(u => u.Role == "Admin");
            if (adminCount <= 1)
            {
                TempData["Error"] = GetLocalizedMessage(
                    "Нельзя удалить последнего администратора.",
                    "Cannot delete the last administrator.",
                    "Соңғы әкімшіні жою мүмкін емес.");
                return RedirectToPage("./Index");
            }
        }

        try
        {
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            TempData["Success"] = GetLocalizedMessage(
                $"Пользователь «{user.Username}» удалён.",
                $"User «{user.Username}» deleted.",
                $"«{user.Username}» пайдаланушысы жойылды.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при удалении пользователя {UserId}", id);
            TempData["Error"] = GetLocalizedMessage(
                "Не удалось удалить пользователя.",
                "Failed to delete user.",
                "Пайдаланушыны жою мүмкін болмады.");
        }

        return RedirectToPage("./Index");
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