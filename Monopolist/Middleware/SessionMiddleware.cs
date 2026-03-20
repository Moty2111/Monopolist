using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Monoplist.Data;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Monoplist.Middleware
{
    public class SessionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<SessionMiddleware> _logger;

        public SessionMiddleware(RequestDelegate next, ILogger<SessionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, AppDbContext dbContext)
        {
            // Если пользователь аутентифицирован
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = context.User.FindFirst("UserId");
                var sessionId = context.Request.Cookies["session_id"];

                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId) && !string.IsNullOrEmpty(sessionId))
                {
                    // Находим сессию в БД
                    var userSession = await dbContext.UserSessions
                        .FirstOrDefaultAsync(us => us.UserId == userId && us.SessionId == sessionId && us.IsActive);

                    if (userSession != null)
                    {
                        // Обновляем время последней активности
                        userSession.LastActivityTime = DateTime.UtcNow;
                        await dbContext.SaveChangesAsync();
                    }
                    else
                    {
                        // Если сессии нет в БД (например, была удалена вручную), то разлогиниваем пользователя
                        _logger.LogWarning("Сессия {SessionId} не найдена в БД. Выход пользователя {UserId}", sessionId, userId);
                        await context.SignOutAsync();
                        context.Response.Cookies.Delete("session_id");
                        context.Response.Redirect("/Account/Login");
                        return;
                    }
                }
            }

            await _next(context);
        }
    }
}