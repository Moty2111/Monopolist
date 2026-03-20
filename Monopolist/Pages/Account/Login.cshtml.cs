using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Monoplist.Data;
using Monoplist.Models;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Monoplist.Pages.Account;

public class LoginModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly ILogger<LoginModel> _logger;

    public LoginModel(AppDbContext context, ILogger<LoginModel> logger)
    {
        _context = context;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ReturnUrl { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "Имя пользователя обязательно.")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Пароль обязателен.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
    }

    public void OnGet(string? returnUrl = null)
    {
        ReturnUrl = returnUrl ?? Url.Content("~/");
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");

        if (!ModelState.IsValid)
            return Page();

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == Input.Username && u.Password == Input.Password);

        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Неверное имя пользователя или пароль.");
            return Page();
        }

        // Если 2FA включена
        if (user.TwoFactorEnabled && !string.IsNullOrEmpty(user.TwoFactorSecret))
        {
            TempData["UserId"] = user.Id;
            TempData["RememberMe"] = Input.RememberMe;
            TempData["ReturnUrl"] = returnUrl;
            return RedirectToPage("./Verify2fa");
        }

        // Стандартный вход
        await SignInUser(user, Input.RememberMe);
        return LocalRedirect(returnUrl);
    }

    // Общий метод для входа (используется и в Login, и в Verify2fa)
    public async Task SignInUser(User user, bool rememberMe)
    {
        user.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("UserId", user.Id.ToString())
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var props = new AuthenticationProperties
        {
            IsPersistent = rememberMe,
            ExpiresUtc = rememberMe ? DateTimeOffset.UtcNow.AddDays(7) : null
        };
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), props);

        // Аватарка в куки
        if (!string.IsNullOrEmpty(user.AvatarUrl))
        {
            Response.Cookies.Append($"user_avatar_{user.Id}", user.AvatarUrl, new CookieOptions
            {
                Expires = rememberMe ? DateTimeOffset.UtcNow.AddDays(7) : null,
                HttpOnly = false,
                Secure = true,
                SameSite = SameSiteMode.Lax
            });
        }

        // Сессия
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

        Response.Cookies.Append("session_id", sessionId, new CookieOptions
        {
            Expires = rememberMe ? DateTime.UtcNow.AddDays(30) : null,
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Secure = true
        });
    }

    private string GetDeviceInfo()
    {
        var ua = Request.Headers["User-Agent"].ToString();
        if (ua.Contains("Windows")) return "Windows PC";
        if (ua.Contains("Mac")) return "Mac";
        if (ua.Contains("iPhone")) return "iPhone";
        if (ua.Contains("Android")) return "Android";
        return "Неизвестное устройство";
    }

    private string GetBrowserInfo()
    {
        var ua = Request.Headers["User-Agent"].ToString();
        if (ua.Contains("Chrome") && !ua.Contains("Edg")) return "Chrome";
        if (ua.Contains("Firefox")) return "Firefox";
        if (ua.Contains("Safari") && !ua.Contains("Chrome")) return "Safari";
        if (ua.Contains("Edg")) return "Edge";
        return "Неизвестный браузер";
    }
}