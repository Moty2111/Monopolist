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
            // Проверяем, аутентифицирован ли сотрудник (схема EmployeeCookie)
            var userIdClaim = context.User.FindFirst("UserId");
            var isEmployee = userIdClaim != null && context.User.Identity?.IsAuthenticated == true;

            if (isEmployee)
            {
                var sessionId = context.Request.Cookies["session_id"];

                if (int.TryParse(userIdClaim.Value, out int userId) && !string.IsNullOrEmpty(sessionId))
                {
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
                        // Сессия не найдена – выходим из системы сотрудника
                        _logger.LogWarning("Сессия {SessionId} не найдена в БД. Выход пользователя {UserId}", sessionId, userId);
                        await context.SignOutAsync("EmployeeCookie");
                        context.Response.Cookies.Delete("session_id");
                        context.Response.Redirect("/Account/Login");
                        return;
                    }
                }
            }

            // Если аутентифицирован клиент (схема CustomerCookie), ничего не делаем,
            // так как для клиентов сессии не отслеживаются в этой версии.

            await _next(context);
        }
    }
}