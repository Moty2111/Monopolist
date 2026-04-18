using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Monoplist.Data;
using Monoplist.ViewModels;
using System.Security.Claims;

namespace Monoplist.Pages.Settings;

[Authorize]
public class NotificationsModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly ILogger<NotificationsModel> _logger;

    public NotificationsModel(AppDbContext context, ILogger<NotificationsModel> logger)
    {
        _context = context;
        _logger = logger;
    }

    public List<NotificationViewModel> Notifications { get; set; } = new();

    public string Language { get; set; } = "ru";
    public bool CompactMode { get; set; }
    public bool Animations { get; set; } = true;
    public string Theme { get; set; } = "light";
    public string CustomColor { get; set; } = "#FF6B00";

    public async Task OnGetAsync()
    {
        await LoadUserSettings();
        await LoadNotifications();
    }

    private async Task LoadUserSettings()
    {
        var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
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

    private async Task LoadNotifications()
    {
        var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
        if (userId == 0) return;

        var notifications = await _context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .ToListAsync();

        Notifications = notifications.Select(n => new NotificationViewModel
        {
            Id = n.Id,
            Title = n.Title,
            Message = n.Message,
            Link = n.Link,
            Type = n.Type.ToString().ToLower(),
            IsRead = n.IsRead,
            CreatedAt = n.CreatedAt,
            TimeAgo = GetTimeAgo(n.CreatedAt)
        }).ToList();
    }

    private string GetTimeAgo(DateTime dateTime)
    {
        var timeSpan = DateTime.UtcNow - dateTime;
        if (timeSpan.TotalMinutes < 1) return GetLocalizedMessage("только что", "just now", "жаңа ғана");
        if (timeSpan.TotalMinutes < 60) return $"{(int)timeSpan.TotalMinutes} {GetLocalizedMessage("мин", "min", "мин")}";
        if (timeSpan.TotalHours < 24) return $"{(int)timeSpan.TotalHours} {GetLocalizedMessage("ч", "h", "сағ")}";
        if (timeSpan.TotalDays < 7) return $"{(int)timeSpan.TotalDays} {GetLocalizedMessage("дн", "d", "күн")}";
        return dateTime.ToString("dd.MM.yyyy");
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

    public async Task<IActionResult> OnPostMarkAsReadAsync(int id)
    {
        var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);
        if (notification != null && !notification.IsRead)
        {
            notification.IsRead = true;
            await _context.SaveChangesAsync();
        }
        return new OkResult();
    }

    public async Task<IActionResult> OnPostMarkAllAsReadAsync()
    {
        var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
        var unread = await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();
        foreach (var n in unread)
            n.IsRead = true;
        await _context.SaveChangesAsync();
        return new OkResult();
    }
}