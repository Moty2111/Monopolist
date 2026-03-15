using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Monoplist.Data;
using Monoplist.Models;
using Monoplist.ViewModels;
using System.Security.Claims;

namespace Monoplist.Pages.Settings;

[Authorize]
public class ProfileModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly ILogger<ProfileModel> _logger;

    public ProfileModel(AppDbContext context, ILogger<ProfileModel> logger)
    {
        _context = context;
        _logger = logger;
    }

    [BindProperty]
    public ProfileViewModel Input { get; set; } = new();

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

        // Заполняем данные профиля
        Input.Id = user.Id;
        Input.Username = user.Username;
        Input.Role = user.Role;
        Input.CreatedAt = user.CreatedAt;

        // Загружаем дополнительные данные из куки (они не хранятся в БД)
        Input.Email = Request.Cookies[$"user_email_{userId}"] ?? "user@example.com";
        Input.FullName = Request.Cookies[$"user_fullname_{userId}"] ?? "Иванов Иван";
        Input.Position = user.Role == "Admin" ? "Администратор" : "Менеджер";
        Input.PhoneNumber = Request.Cookies[$"user_phone_{userId}"] ?? "+7 (999) 123-45-67";
        Input.AvatarUrl = Request.Cookies[$"user_avatar_{userId}"] ?? "";
        Input.LastLoginAt = DateTime.Now.AddDays(-1); // В реальности из журнала

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

        // Обновляем имя пользователя
        user.Username = Input.Username;

        // Смена пароля, если указан
        if (!string.IsNullOrEmpty(Input.NewPassword))
        {
            if (user.Password != Input.CurrentPassword)
            {
                ModelState.AddModelError("Input.CurrentPassword", GetLocalizedMessage(
                    "Неверный текущий пароль",
                    "Invalid current password",
                    "Құпиясөз қате"));
                await LoadUserSettings();
                return Page();
            }

            user.Password = Input.NewPassword;
        }

        user.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _context.SaveChangesAsync();

            // Сохраняем дополнительные данные в куки
            var cookieOptions = new CookieOptions
            {
                Expires = DateTime.Now.AddYears(1),
                HttpOnly = true,
                SameSite = SameSiteMode.Lax
            };
            Response.Cookies.Append($"user_email_{userId}", Input.Email ?? "", cookieOptions);
            Response.Cookies.Append($"user_fullname_{userId}", Input.FullName ?? "", cookieOptions);
            Response.Cookies.Append($"user_phone_{userId}", Input.PhoneNumber ?? "", cookieOptions);
            Response.Cookies.Append($"user_avatar_{userId}", Input.AvatarUrl ?? "", cookieOptions);

            TempData["Success"] = GetLocalizedMessage(
                "Профиль успешно обновлен",
                "Profile updated successfully",
                "Профиль жаңартылды");

            // Обновляем имя пользователя в куки аутентификации при необходимости
            if (user.Username != User.Identity?.Name)
            {
                await RefreshUserClaims(user);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обновлении профиля");
            ModelState.AddModelError(string.Empty, GetLocalizedMessage(
                "Произошла ошибка при сохранении.",
                "An error occurred while saving.",
                "Сақтау кезінде қате орын алды."));
            await LoadUserSettings();
            return Page();
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAccountAsync()
    {
        var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
        var user = await _context.Users.FindAsync(userId);

        if (user == null)
            return NotFound();

        if (user.Role == "Admin")
        {
            var adminCount = await _context.Users.CountAsync(u => u.Role == "Admin");
            if (adminCount <= 1)
            {
                TempData["Error"] = GetLocalizedMessage(
                    "Нельзя удалить последнего администратора",
                    "Cannot delete the last administrator",
                    "Соңғы әкімшіні жою мүмкін емес");
                return RedirectToPage();
            }
        }

        try
        {
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            // Очищаем куки пользователя
            foreach (var cookie in Request.Cookies.Keys)
            {
                if (cookie.StartsWith($"user_{userId}"))
                {
                    Response.Cookies.Delete(cookie);
                }
            }

            await HttpContext.SignOutAsync();

            return RedirectToPage("/Account/Login");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при удалении аккаунта");
            TempData["Error"] = GetLocalizedMessage(
                "Не удалось удалить аккаунт",
                "Failed to delete account",
                "Аккаунтты жою мүмкін болмады");
            return RedirectToPage();
        }
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

    private async Task RefreshUserClaims(User user)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("UserId", user.Id.ToString())
        };

        var claimsIdentity = new ClaimsIdentity(claims, Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
        };

        await HttpContext.SignInAsync(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity), authProperties);
    }
}