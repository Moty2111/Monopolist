using Monoplist.Data;
using Monoplist.Models;
using Microsoft.EntityFrameworkCore;

namespace Monoplist.Services;

public static class NotificationService
{
    /// <summary>
    /// Создать уведомление для конкретного пользователя
    /// </summary>
    public static async Task CreateForUserAsync(AppDbContext context, int userId, string title, string message, NotificationType type, string? link = null)
    {
        var notification = new Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            Type = type,
            Link = link,
            CreatedAt = DateTime.UtcNow,
            IsRead = false
        };
        context.Notifications.Add(notification);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Создать уведомление для всех пользователей с определённой ролью (например, "Admin")
    /// </summary>
    public static async Task CreateForRoleAsync(AppDbContext context, string role, string title, string message, NotificationType type, string? link = null)
    {
        var userIds = await context.Users
            .Where(u => u.Role == role)
            .Select(u => u.Id)
            .ToListAsync();

        foreach (var userId in userIds)
        {
            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                Link = link,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };
            context.Notifications.Add(notification);
        }
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Создать уведомление для всех администраторов
    /// </summary>
    public static async Task CreateForAdminsAsync(AppDbContext context, string title, string message, NotificationType type, string? link = null)
    {
        await CreateForRoleAsync(context, "Admin", title, message, type, link);
    }
}