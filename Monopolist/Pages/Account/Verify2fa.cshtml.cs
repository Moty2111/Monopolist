using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Monoplist.Data;
using Monoplist.Models;
using OtpNet;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Monoplist.Pages.Account;

public class Verify2faModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly ILogger<Verify2faModel> _logger;

    public Verify2faModel(AppDbContext context, ILogger<Verify2faModel> logger)
    {
        _context = context;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required]
        [StringLength(6, MinimumLength = 6)]
        public string Code { get; set; } = string.Empty;
        public bool RememberMe { get; set; }
    }

    public string Language { get; set; } = "ru";
    public bool CompactMode { get; set; }
    public bool Animations { get; set; } = true;
    public string Theme { get; set; } = "light";
    public string CustomColor { get; set; } = "#FF6B00";

    public async Task<IActionResult> OnGetAsync()
    {
        var userIdObj = TempData.Peek("UserId");
        if (userIdObj == null)
            return RedirectToPage("/Account/Login");

        if (userIdObj is int userId)
            await LoadUserSettings(userId);

        TempData.Keep("UserId");
        TempData.Keep("RememberMe");
        TempData.Keep("ReturnUrl");
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        _logger.LogInformation("--- Verify2fa POST ---");
        returnUrl ??= Url.Content("~/");

        if (!ModelState.IsValid)
        {
            await LoadUserSettingsFromTempData();
            TempData.Keep("UserId");
            TempData.Keep("RememberMe");
            TempData.Keep("ReturnUrl");
            return Page();
        }

        var userId = TempData["UserId"] as int?;
        if (userId == null) return RedirectToPage("/Account/Login");

        var user = await _context.Users.FindAsync(userId);
        if (user == null || string.IsNullOrEmpty(user.TwoFactorSecret))
            return RedirectToPage("/Account/Login");

        await LoadUserSettings(user.Id);

        _logger.LogInformation("Проверка 2FA для {Username} (UTC: {Time})", user.Username, DateTime.UtcNow);

        var totp = new Totp(Base32Encoding.ToBytes(user.TwoFactorSecret));

        // Расширенное окно проверки – до 1 дня (1440 интервалов = ~12 часов в каждую сторону)
        bool isValid = totp.VerifyTotp(Input.Code, out long timeStepMatched,
            new VerificationWindow(previous: 1440, future: 1440));

        _logger.LogInformation("Код {Valid}, step: {Step}", isValid, timeStepMatched);

        if (!isValid)
        {
            _logger.LogWarning("Неверный код 2FA для {Username}", user.Username);
            ModelState.AddModelError("Input.Code", "Неверный код подтверждения.");
            TempData.Keep("UserId");
            TempData.Keep("RememberMe");
            TempData.Keep("ReturnUrl");
            return Page();
        }

        try
        {
            await SignInUser(user);
            TempData.Remove("UserId");
            TempData.Remove("RememberMe");
            TempData.Remove("ReturnUrl");
            return LocalRedirect(TempData["ReturnUrl"] as string ?? returnUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при входе через 2FA для {Username}", user.Username);
            ModelState.AddModelError(string.Empty, "Ошибка при входе. Попробуйте позже.");
            TempData.Keep("UserId");
            TempData.Keep("RememberMe");
            TempData.Keep("ReturnUrl");
            return Page();
        }
    }

    private async Task SignInUser(User user)
    {
        user.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("UserId", user.Id.ToString())
        };

        var identity = new ClaimsIdentity(claims, "EmployeeCookie");
        var principal = new ClaimsPrincipal(identity);
        var props = new AuthenticationProperties
        {
            IsPersistent = Input.RememberMe,
            ExpiresUtc = Input.RememberMe ? DateTimeOffset.UtcNow.AddDays(7) : null
        };

        // ✅ ИСПОЛЬЗУЕМ СХЕМУ "EmployeeCookie"
        await HttpContext.SignInAsync("EmployeeCookie", principal, props);

        // Сохранение аватарки в куки
        if (!string.IsNullOrEmpty(user.AvatarUrl))
        {
            Response.Cookies.Append($"user_avatar_{user.Id}", user.AvatarUrl,
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddDays(30), HttpOnly = false, Secure = true, SameSite = SameSiteMode.Lax });
        }

        // Создание сессии
        var sessionId = Guid.NewGuid().ToString();
        _context.UserSessions.Add(new UserSession
        {
            UserId = user.Id,
            SessionId = sessionId,
            DeviceInfo = GetDeviceInfo(),
            BrowserInfo = GetBrowserInfo(),
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            LoginTime = DateTime.UtcNow,
            LastActivityTime = DateTime.UtcNow,
            IsActive = true
        });
        await _context.SaveChangesAsync();

        Response.Cookies.Append("session_id", sessionId,
            new CookieOptions { Expires = DateTimeOffset.UtcNow.AddDays(30), HttpOnly = true, Secure = true, SameSite = SameSiteMode.Lax });

        _logger.LogInformation("Пользователь {Username} вошёл через 2FA. Сессия {SessionId}", user.Username, sessionId);
    }

    // Вспомогательные методы (LoadUserSettings, GetDeviceInfo, GetBrowserInfo) без изменений...
    private async Task LoadUserSettings(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user != null)
        {
            Language = user.Language ?? "ru"; CompactMode = user.CompactMode; Animations = user.Animations;
            Theme = user.Theme ?? "light"; CustomColor = user.CustomColor ?? "#FF6B00";
        }
    }
    private async Task LoadUserSettingsFromTempData()
    {
        var userId = TempData["UserId"] as int?;
        if (userId.HasValue) await LoadUserSettings(userId.Value);
    }
    private string GetDeviceInfo() => Request.Headers["User-Agent"].ToString() switch
    {
        var ua when ua.Contains("Windows") => "Windows PC",
        var ua when ua.Contains("Mac") => "Mac",
        var ua when ua.Contains("iPhone") => "iPhone",
        var ua when ua.Contains("Android") => "Android",
        _ => "Неизвестное устройство"
    };
    private string GetBrowserInfo() => Request.Headers["User-Agent"].ToString() switch
    {
        var ua when ua.Contains("Chrome") && !ua.Contains("Edg") => "Chrome",
        var ua when ua.Contains("Firefox") => "Firefox",
        var ua when ua.Contains("Safari") && !ua.Contains("Chrome") => "Safari",
        var ua when ua.Contains("Edg") => "Edge",
        _ => "Неизвестный браузер"
    };
}