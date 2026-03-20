using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
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

    // Свойства для персонализации
    public string Language { get; set; } = "ru";
    public bool CompactMode { get; set; }
    public bool Animations { get; set; } = true;
    public string Theme { get; set; } = "light";
    public string CustomColor { get; set; } = "#FF6B00";

    public async Task<IActionResult> OnGetAsync()
    {
        _logger.LogInformation("--- Вход в метод OnGetAsync Verify2fa ---");

        var userIdObj = TempData.Peek("UserId");
        if (userIdObj == null)
        {
            _logger.LogWarning("OnGetAsync: TempData[UserId] is null, redirect to login");
            return RedirectToPage("/Account/Login");
        }

        var userId = userIdObj as int?;
        if (userId.HasValue)
        {
            await LoadUserSettings(userId.Value);
        }

        // Явно сохраняем TempData для следующего запроса (POST)
        TempData.Keep("UserId");
        TempData.Keep("RememberMe");
        TempData.Keep("ReturnUrl");

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        _logger.LogInformation("--- Вход в метод OnPostAsync Verify2fa ---");

        returnUrl ??= Url.Content("~/");

        if (!ModelState.IsValid)
        {
            await LoadUserSettingsFromTempData();
            return Page();
        }

        var userId = TempData["UserId"] as int?;
        _logger.LogInformation("TempData[UserId] = {UserId}", userId);

        if (userId == null)
        {
            _logger.LogWarning("userId == null, перенаправление на логин");
            return RedirectToPage("/Account/Login");
        }

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("Пользователь с Id {UserId} не найден", userId);
            return RedirectToPage("/Account/Login");
        }

        if (string.IsNullOrEmpty(user.TwoFactorSecret))
        {
            _logger.LogWarning("Пользователь {Username} не имеет секрета 2FA", user.Username);
            return RedirectToPage("/Account/Login");
        }

        await LoadUserSettings(user.Id);

        var rememberMe = TempData["RememberMe"] as bool? ?? Input.RememberMe;
        var originalReturnUrl = TempData["ReturnUrl"] as string ?? returnUrl;

        _logger.LogInformation("Проверка 2FA для {Username}, TwoFactorEnabled={Enabled}, SecretExists={SecretExists}",
            user.Username, user.TwoFactorEnabled, !string.IsNullOrEmpty(user.TwoFactorSecret));

        var totp = new Totp(Base32Encoding.ToBytes(user.TwoFactorSecret));
        // Увеличиваем окно верификации до ±2 интервалов (около 60 секунд)
        bool isValid = totp.VerifyTotp(Input.Code, out long timeStepMatched, new VerificationWindow(previous: 2, future: 2));

        _logger.LogInformation("Результат проверки кода: {IsValid}, timeStepMatched={TimeStepMatched}", isValid, timeStepMatched);

        if (isValid)
        {
            _logger.LogInformation("Код 2FA верный для {Username}", user.Username);
            try
            {
                await SignInUser(user, rememberMe);

                TempData.Remove("UserId");
                TempData.Remove("RememberMe");
                TempData.Remove("ReturnUrl");

                _logger.LogInformation("Вход через 2FA выполнен, перенаправление на {ReturnUrl}", originalReturnUrl);
                return LocalRedirect(originalReturnUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при входе через 2FA для {Username}", user.Username);
                ModelState.AddModelError(string.Empty, "Ошибка при входе. Попробуйте позже.");
                return Page();
            }
        }
        else
        {
            _logger.LogWarning("Неверный код 2FA для {Username}", user.Username);
            ModelState.AddModelError(string.Empty, "Неверный код подтверждения.");
            return Page();
        }
    }

    private async Task SignInUser(User user, bool rememberMe)
    {
        // Обновляем время последнего входа
        user.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Создаём claims
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("UserId", user.Id.ToString())
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = rememberMe,
            ExpiresUtc = rememberMe ? DateTimeOffset.UtcNow.AddDays(7) : null
        };

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

        // Сохраняем аватарку в куку
        if (!string.IsNullOrEmpty(user.AvatarUrl))
        {
            var cookieOptions = new CookieOptions
            {
                Expires = rememberMe ? DateTimeOffset.UtcNow.AddDays(7) : null,
                HttpOnly = false,
                Secure = true,
                SameSite = SameSiteMode.Lax
            };
            Response.Cookies.Append($"user_avatar_{user.Id}", user.AvatarUrl, cookieOptions);
        }

        // Создаём сессию в БД
        var sessionId = Guid.NewGuid().ToString();
        var userSession = new UserSession
        {
            UserId = user.Id,
            SessionId = sessionId,
            DeviceInfo = GetDeviceInfo(),
            BrowserInfo = GetBrowserInfo(),
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
            LoginTime = DateTime.UtcNow,
            LastActivityTime = DateTime.UtcNow,
            IsActive = true
        };
        _context.UserSessions.Add(userSession);
        await _context.SaveChangesAsync();

        // Сохраняем session_id в куки
        var cookieOptionsSession = new CookieOptions
        {
            Expires = rememberMe ? DateTime.UtcNow.AddDays(30) : null,
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Secure = true
        };
        Response.Cookies.Append("session_id", sessionId, cookieOptionsSession);

        _logger.LogInformation("Пользователь {Username} вошёл через 2FA. Сессия {SessionId}", user.Username, sessionId);
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

    private async Task LoadUserSettingsFromTempData()
    {
        var userId = TempData["UserId"] as int?;
        if (userId.HasValue) await LoadUserSettings(userId.Value);
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