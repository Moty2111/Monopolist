using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Monoplist.Data;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Monoplist.Pages.Account
{
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
            [Display(Name = "Логин")]
            public string Username { get; set; } = string.Empty;

            [Required(ErrorMessage = "Пароль обязателен.")]
            [DataType(DataType.Password)]
            [Display(Name = "Пароль")]
            public string Password { get; set; } = string.Empty;

            [Display(Name = "Запомнить меня")]
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

            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == Input.Username && u.Password == Input.Password);

                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, "Неверное имя пользователя или пароль.");
                    return Page();
                }

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Role, user.Role),
                    new Claim("UserId", user.Id.ToString())
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = Input.RememberMe,
                    ExpiresUtc = Input.RememberMe ? DateTimeOffset.UtcNow.AddDays(7) : null
                };

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

                _logger.LogInformation("Пользователь {Username} успешно вошёл в систему.", user.Username);
                return LocalRedirect(returnUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при входе пользователя {Username}", Input.Username);
                ModelState.AddModelError(string.Empty, "Произошла ошибка при входе. Попробуйте позже.");
                return Page();
            }
        }
    }
}