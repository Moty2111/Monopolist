using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Monoplist.Data;
using System.Security.Claims;

namespace Monoplist.Pages.Account
{
    public class LogoutModel : PageModel
    {
        private readonly ILogger<LogoutModel> _logger;
        private readonly AppDbContext _context;

        public LogoutModel(ILogger<LogoutModel> logger, AppDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult OnGet()
        {
            return RedirectToPage("/Index");
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var userIdClaim = User.FindFirst("UserId");
            var sessionId = Request.Cookies["session_id"];

            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId) && !string.IsNullOrEmpty(sessionId))
            {
                // Удаляем запись о сессии из базы данных
                var userSession = await _context.UserSessions
                    .FirstOrDefaultAsync(us => us.UserId == userId && us.SessionId == sessionId);
                if (userSession != null)
                {
                    _context.UserSessions.Remove(userSession);
                    await _context.SaveChangesAsync();
                }

                // Удаляем куку с аватаркой
                Response.Cookies.Delete($"user_avatar_{userId}");
            }

            // Удаляем все куки, связанные с сессией (включая session_id)
            foreach (var cookie in Request.Cookies.Keys)
            {
                Response.Cookies.Delete(cookie);
            }

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            _logger.LogInformation("Пользователь {UserName} вышел", User.Identity?.Name ?? "Неизвестный");

            return RedirectToPage("/Account/Login");
        }
    }
}