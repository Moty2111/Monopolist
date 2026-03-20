// Pages/Settings/Security.cshtml.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Monoplist.Data;
using Monoplist.Models;
using Monoplist.ViewModels;
using OtpNet;
using QRCoder;
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

        // Загружаем настройки безопасности из БД
        Input.TwoFactorEnabled = user.TwoFactorEnabled;
        Input.EmailConfirmed = !string.IsNullOrEmpty(user.Email);
        Input.PhoneConfirmed = !string.IsNullOrEmpty(user.PhoneNumber);

        // Получаем активные сессии из БД
        Input.ActiveSessions = await GetActiveSessionsAsync(userId);

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

        // Создаём URI для TOTP
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
        TempData["ManualSetupKey"] = secretKey;

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
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return NotFound();

        await LoadUserSettings(userId);

        var session = await _context.UserSessions
            .FirstOrDefaultAsync(s => s.UserId == userId && s.SessionId == sessionId);
        if (session != null)
        {
            _context.UserSessions.Remove(session);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Сессия {SessionId} завершена пользователем {UserId}", sessionId, userId);
        }

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
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return NotFound();

        await LoadUserSettings(userId);

        var currentSessionId = Request.Cookies["session_id"];
        var otherSessions = await _context.UserSessions
            .Where(s => s.UserId == userId && s.SessionId != currentSessionId)
            .ToListAsync();

        _context.UserSessions.RemoveRange(otherSessions);
        await _context.SaveChangesAsync();

        TempData["Success"] = GetLocalizedMessage(
            "Все остальные сессии завершены.",
            "All other sessions terminated.",
            "Барлық басқа сессиялар аяқталды.");

        return RedirectToPage();
    }

    // Получение активных сессий из БД
    private async Task<List<SessionInfo>> GetActiveSessionsAsync(int userId)
    {
        var currentSessionId = Request.Cookies["session_id"];

        var sessions = await _context.UserSessions
            .Where(s => s.UserId == userId && s.IsActive)
            .OrderByDescending(s => s.LoginTime)
            .Select(s => new SessionInfo
            {
                Id = s.SessionId,
                Device = s.DeviceInfo ?? "Неизвестно",
                Browser = s.BrowserInfo ?? "",
                IpAddress = s.IpAddress ?? "",
                LoginTime = s.LoginTime,
                IsCurrent = s.SessionId == currentSessionId
            })
            .ToListAsync();

        return sessions;
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
}