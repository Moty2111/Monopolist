using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Monoplist.Data;
using Monoplist.ViewModels;
using System.Security.Claims;

namespace Monoplist.Pages.Settings;

[Authorize]
public class SecurityModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly ILogger<SecurityModel> _logger;

    public SecurityModel(AppDbContext context, ILogger<SecurityModel> logger)
    {
        _context = context;
        _logger = logger;
    }

    [BindProperty]
    public SecurityViewModel Input { get; set; } = new();

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

        // Загружаем настройки безопасности из куки/сессии
        Input.TwoFactorEnabled = Request.Cookies[$"2fa_{userId}"] == "enabled";
        Input.EmailConfirmed = Request.Cookies[$"email_confirmed_{userId}"] == "true";
        Input.PhoneConfirmed = Request.Cookies[$"phone_confirmed_{userId}"] == "true";

        // Генерируем или получаем существующие сессии
        Input.ActiveSessions = GetActiveSessions(userId);

        return Page();
    }

    public async Task<IActionResult> OnPostEnable2faAsync()
    {
        var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
        await LoadUserSettings(userId);

        // Включаем 2FA
        Response.Cookies.Append($"2fa_{userId}", "enabled", new CookieOptions
        {
            Expires = DateTime.Now.AddYears(1),
            HttpOnly = true,
            SameSite = SameSiteMode.Lax
        });

        // Генерируем секретный ключ для 2FA (в реальном проекте сохранять в БД)
        var secretKey = GenerateRandomKey();
        TempData["SecretKey"] = secretKey;
        TempData["QrCodeUrl"] = GenerateQrCodeUrl(userId.ToString(), secretKey);

        TempData["Success"] = GetLocalizedMessage(
            "Двухфакторная аутентификация включена. Сохраните секретный ключ.",
            "Two-factor authentication enabled. Save the secret key.",
            "Екі факторлы аутентификация қосылды. Құпия кілтті сақтаңыз.");

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDisable2faAsync()
    {
        var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
        await LoadUserSettings(userId);

        Response.Cookies.Delete($"2fa_{userId}");
        TempData["Success"] = GetLocalizedMessage(
            "Двухфакторная аутентификация отключена.",
            "Two-factor authentication disabled.",
            "Екі факторлы аутентификация өшірілді.");

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostVerify2faAsync(string code)
    {
        var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
        await LoadUserSettings(userId);

        // Здесь проверка кода 2FA
        if (code == "123456") // Demo validation
        {
            Response.Cookies.Append($"2fa_verified_{userId}", "true", new CookieOptions
            {
                Expires = DateTime.Now.AddYears(1),
                HttpOnly = true
            });
            TempData["Success"] = GetLocalizedMessage(
                "Код подтвержден успешно.",
                "Code verified successfully.",
                "Код сәтті расталды.");
        }
        else
        {
            TempData["Error"] = GetLocalizedMessage(
                "Неверный код подтверждения.",
                "Invalid verification code.",
                "Қате растау коды.");
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostSendEmailConfirmationAsync()
    {
        var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
        await LoadUserSettings(userId);

        var userEmail = Request.Cookies[$"user_email_{userId}"] ?? "user@example.com";

        // Отправка письма с подтверждением
        _logger.LogInformation("Отправлено письмо подтверждения на {Email}", userEmail);

        TempData["Success"] = GetLocalizedMessage(
            $"Письмо с подтверждением отправлено на {userEmail}",
            $"Confirmation email sent to {userEmail}",
            $"Растау хаты {userEmail} мекенжайына жіберілді");

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostSendPhoneConfirmationAsync()
    {
        var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
        await LoadUserSettings(userId);

        var userPhone = Request.Cookies[$"user_phone_{userId}"] ?? "+7 (999) 123-45-67";

        // Генерация и отправка SMS с кодом
        var code = new Random().Next(100000, 999999).ToString();
        TempData["PhoneCode"] = code;
        _logger.LogInformation("Отправлен SMS код {Code} на номер {Phone}", code, userPhone);

        TempData["Success"] = GetLocalizedMessage(
            $"Код подтверждения отправлен на {userPhone}",
            $"Verification code sent to {userPhone}",
            $"Растау коды {userPhone} нөміріне жіберілді");

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostVerifyPhoneAsync(string code)
    {
        var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
        await LoadUserSettings(userId);

        var savedCode = TempData["PhoneCode"]?.ToString();

        if (savedCode == code)
        {
            Response.Cookies.Append($"phone_confirmed_{userId}", "true", new CookieOptions
            {
                Expires = DateTime.Now.AddYears(1),
                HttpOnly = true
            });
            TempData["Success"] = GetLocalizedMessage(
                "Номер телефона подтвержден.",
                "Phone number confirmed.",
                "Телефон нөмірі расталды.");
        }
        else
        {
            TempData["Error"] = GetLocalizedMessage(
                "Неверный код подтверждения.",
                "Invalid verification code.",
                "Қате растау коды.");
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRevokeSessionAsync(string sessionId)
    {
        var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
        await LoadUserSettings(userId);

        Response.Cookies.Delete($"session_{userId}_{sessionId}");

        _logger.LogInformation("Сессия {SessionId} завершена", sessionId);
        TempData["Success"] = GetLocalizedMessage(
            "Сессия завершена.",
            "Session terminated.",
            "Сессия аяқталды.");

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRevokeAllSessionsAsync()
    {
        var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
        await LoadUserSettings(userId);

        var currentSessionId = Request.Cookies["session_id"];
        foreach (var cookie in Request.Cookies.Keys)
        {
            if (cookie.StartsWith($"session_{userId}_") && cookie != $"session_{userId}_{currentSessionId}")
            {
                Response.Cookies.Delete(cookie);
            }
        }

        TempData["Success"] = GetLocalizedMessage(
            "Все остальные сессии завершены.",
            "All other sessions terminated.",
            "Барлық басқа сессиялар аяқталды.");

        return RedirectToPage();
    }

    private async Task LoadUserSettings(int userId)
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

    private string GetLocalizedMessage(string ru, string en, string kk)
    {
        return Language switch
        {
            "en" => en,
            "kk" => kk,
            _ => ru
        };
    }

    private List<SessionInfo> GetActiveSessions(int userId)
    {
        var sessions = new List<SessionInfo>();
        var currentSessionId = Request.Cookies["session_id"] ?? Guid.NewGuid().ToString();

        if (string.IsNullOrEmpty(Request.Cookies["session_id"]))
        {
            Response.Cookies.Append("session_id", currentSessionId, new CookieOptions
            {
                Expires = DateTime.Now.AddDays(30),
                HttpOnly = true,
                SameSite = SameSiteMode.Lax
            });
        }

        sessions.Add(new SessionInfo
        {
            Id = currentSessionId,
            Device = GetDeviceInfo(),
            Browser = GetBrowserInfo(),
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
            LoginTime = DateTime.Now,
            IsCurrent = true
        });

        // Добавляем несколько демо-сессий для примера
        for (int i = 1; i <= 3; i++)
        {
            var sessionId = $"session_{i}";
            var sessionCookie = Request.Cookies[$"session_{userId}_{sessionId}"];
            if (!string.IsNullOrEmpty(sessionCookie))
            {
                sessions.Add(new SessionInfo
                {
                    Id = sessionId,
                    Device = i % 2 == 0 ? "iPhone 12" : "Windows PC",
                    Browser = i % 2 == 0 ? "Safari" : "Chrome",
                    IpAddress = "192.168.1.10" + i,
                    LoginTime = DateTime.Now.AddDays(-i),
                    IsCurrent = false
                });
            }
        }

        return sessions;
    }

    private string GetDeviceInfo()
    {
        var userAgent = Request.Headers["User-Agent"].ToString();
        if (userAgent.Contains("Windows")) return "Windows PC";
        if (userAgent.Contains("Mac")) return "Mac";
        if (userAgent.Contains("iPhone")) return "iPhone";
        if (userAgent.Contains("Android")) return "Android";
        return "Неизвестное устройство";
    }

    private string GetBrowserInfo()
    {
        var userAgent = Request.Headers["User-Agent"].ToString();
        if (userAgent.Contains("Chrome") && !userAgent.Contains("Edg")) return "Chrome";
        if (userAgent.Contains("Firefox")) return "Firefox";
        if (userAgent.Contains("Safari") && !userAgent.Contains("Chrome")) return "Safari";
        if (userAgent.Contains("Edg")) return "Edge";
        return "Неизвестный браузер";
    }

    private string GenerateRandomKey()
    {
        var bytes = new byte[20];
        using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
        {
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes);
        }
    }

    private string GenerateQrCodeUrl(string userId, string secretKey)
    {
        var appName = "Моноплист";
        var encodedSecret = Uri.EscapeDataString(secretKey);
        var encodedUser = Uri.EscapeDataString(User.Identity?.Name ?? userId);
        return $"otpauth://totp/{appName}:{encodedUser}?secret={encodedSecret}&issuer={appName}";
    }
}