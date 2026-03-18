using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Monoplist.Data;
using Monoplist.Models;
using Monoplist.ViewModels;
using OtpNet;          // NuGet: Otp.NET
using QRCoder;         // NuGet: QRCoder
using System.Security.Claims;
using System.Text;

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

        // Загружаем настройки безопасности из БД
        Input.TwoFactorEnabled = user.TwoFactorEnabled;
        Input.EmailConfirmed = !string.IsNullOrEmpty(user.Email); // В реальном проекте добавьте поле EmailConfirmed
        Input.PhoneConfirmed = !string.IsNullOrEmpty(user.PhoneNumber); // Добавьте поле PhoneConfirmed

        // Генерируем или получаем существующие сессии
        Input.ActiveSessions = GetActiveSessions(userId);

        return Page();
    }

    // Включение 2FA – генерируем секретный ключ и показываем QR-код
    public async Task<IActionResult> OnPostEnable2faAsync()
    {
        var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return NotFound();

        await LoadUserSettings(userId);

        // Генерируем секретный ключ (20 байт = 160 бит)
        byte[] secretKeyBytes = KeyGeneration.GenerateRandomKey(20);
        string secretKey = Base32Encoding.ToString(secretKeyBytes);
        user.TwoFactorSecret = secretKey;
        user.TwoFactorEnabled = false; // Ещё не подтверждён
        await _context.SaveChangesAsync();

        // Создаём URI для TOTP (otpauth://totp/{issuer}:{account}?secret={secret}&issuer={issuer})
        string issuer = "Моноплист";
        string account = user.Email ?? user.Username;
        string totpUri = $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(account)}?secret={secretKey}&issuer={Uri.EscapeDataString(issuer)}";

        // Генерируем QR-код (Base64)
        using (var qrGenerator = new QRCodeGenerator())
        using (var qrCodeData = qrGenerator.CreateQrCode(totpUri, QRCodeGenerator.ECCLevel.Q))
        using (var qrCode = new Base64QRCode(qrCodeData))
        {
            string qrCodeImageBase64 = qrCode.GetGraphic(20);
            TempData["QrCodeBase64"] = qrCodeImageBase64;
        }

        TempData["SecretKey"] = secretKey;
        TempData["ManualSetupKey"] = secretKey; // Для ручного ввода

        TempData["Success"] = GetLocalizedMessage(
            "Отсканируйте QR-код в приложении Google Authenticator и введите код для подтверждения.",
            "Scan the QR code with Google Authenticator and enter the code to confirm.",
            "QR кодын Google Authenticator қолданбасында сканерлеп, кодты енгізіңіз.");

        return RedirectToPage();
    }

    // Подтверждение 2FA после сканирования QR-кода
    public async Task<IActionResult> OnPostVerify2faAsync(string code)
    {
        var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return NotFound();

        await LoadUserSettings(userId);

        if (string.IsNullOrEmpty(user.TwoFactorSecret))
        {
            TempData["Error"] = GetLocalizedMessage(
                "Сначала включите двухфакторную аутентификацию.",
                "Enable two-factor authentication first.",
                "Алдымен екі факторлы аутентификацияны қосыңыз.");
            return RedirectToPage();
        }

        // Проверяем код с помощью Otp.NET
        var totp = new Totp(Base32Encoding.ToBytes(user.TwoFactorSecret));
        bool isValid = totp.VerifyTotp(code, out long timeStepMatched, VerificationWindow.RfcSpecifiedNetworkDelay);

        if (isValid)
        {
            user.TwoFactorEnabled = true;
            await _context.SaveChangesAsync();

            TempData["Success"] = GetLocalizedMessage(
                "Двухфакторная аутентификация успешно включена.",
                "Two-factor authentication enabled successfully.",
                "Екі факторлы аутентификация сәтті қосылды.");
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

    // Отключение 2FA
    public async Task<IActionResult> OnPostDisable2faAsync()
    {
        var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return NotFound();

        await LoadUserSettings(userId);

        user.TwoFactorEnabled = false;
        user.TwoFactorSecret = null; // Очищаем секрет
        await _context.SaveChangesAsync();

        TempData["Success"] = GetLocalizedMessage(
            "Двухфакторная аутентификация отключена.",
            "Two-factor authentication disabled.",
            "Екі факторлы аутентификация өшірілді.");

        return RedirectToPage();
    }

    // Отправка подтверждения Email (пример)
    public async Task<IActionResult> OnPostSendEmailConfirmationAsync()
    {
        var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return NotFound();

        await LoadUserSettings(userId);

        // Здесь должна быть реальная отправка письма
        _logger.LogInformation("Отправлено письмо подтверждения на {Email}", user.Email);

        TempData["Success"] = GetLocalizedMessage(
            $"Письмо с подтверждением отправлено на {user.Email}",
            $"Confirmation email sent to {user.Email}",
            $"Растау хаты {user.Email} мекенжайына жіберілді");

        return RedirectToPage();
    }

    // Отправка SMS для подтверждения телефона
    public async Task<IActionResult> OnPostSendPhoneConfirmationAsync()
    {
        var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return NotFound();

        await LoadUserSettings(userId);

        // Генерация кода и отправка SMS
        var code = new Random().Next(100000, 999999).ToString();
        TempData["PhoneCode"] = code;
        _logger.LogInformation("Отправлен SMS код {Code} на номер {Phone}", code, user.PhoneNumber);

        TempData["Success"] = GetLocalizedMessage(
            $"Код подтверждения отправлен на {user.PhoneNumber}",
            $"Verification code sent to {user.PhoneNumber}",
            $"Растау коды {user.PhoneNumber} нөміріне жіберілді");

        return RedirectToPage();
    }

    // Подтверждение телефона
    public async Task<IActionResult> OnPostVerifyPhoneAsync(string code)
    {
        var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return NotFound();

        await LoadUserSettings(userId);

        var savedCode = TempData["PhoneCode"]?.ToString();

        if (savedCode == code)
        {
            // В реальном проекте установите флаг PhoneConfirmed в БД
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

    // Завершение сессии
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

    // Завершение всех сессий, кроме текущей
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

        // Демо-сессии для примера (в реальном проекте – из БД)
        for (int i = 1; i <= 3; i++)
        {
            sessions.Add(new SessionInfo
            {
                Id = $"demo_{i}",
                Device = i % 2 == 0 ? "iPhone 12" : "Windows PC",
                Browser = i % 2 == 0 ? "Safari" : "Chrome",
                IpAddress = "192.168.1.10" + i,
                LoginTime = DateTime.Now.AddDays(-i),
                IsCurrent = false
            });
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
}