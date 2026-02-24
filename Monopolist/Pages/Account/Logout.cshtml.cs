using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Monoplist.Pages.Account
{
    public class LogoutModel : PageModel
    {
        private readonly ILogger<LogoutModel> _logger;

        public LogoutModel(ILogger<LogoutModel> logger)
        {
            _logger = logger;
        }

        public IActionResult OnGet()
        {
            return RedirectToPage("/Index");
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var userName = User.Identity?.Name;
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            _logger.LogInformation("Пользователь {UserName} вышел", userName ?? "Неизвестный");

            foreach (var cookie in Request.Cookies.Keys)
            {
                Response.Cookies.Delete(cookie);
            }

            return RedirectToPage("/Account/Login");
        }
    }
}