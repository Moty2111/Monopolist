using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Monoplist.Data;
using Monoplist.ViewModels;
using System.Security.Claims;

namespace Monoplist.Pages.Admin.Users;

[Authorize(Roles = "Admin")]
public class Users : PageModel
{
    private readonly AppDbContext _context;
    private readonly ILogger<IndexModel> _logger;

    public Users(AppDbContext context, ILogger<IndexModel> logger)
    {
        _context = context;
        _logger = logger;
    }

    public List<UserIndexViewModel> users { get; set; } = new();

    public async Task OnGetAsync()
    {
        users = await _context.Users
            .Select(u => new UserIndexViewModel
            {
                Id = u.Id,
                Username = u.Username,
                Role = u.Role
            })
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostChangeRoleAsync(int userId, string role)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            TempData["Error"] = "Пользователь не найден.";
            return RedirectToPage();
        }

        // Проверка на последнего администратора
        if (user.Role == "Admin" && role != "Admin")
        {
            var adminCount = await _context.Users.CountAsync(u => u.Role == "Admin");
            if (adminCount <= 1)
            {
                TempData["Error"] = "Нельзя изменить роль последнего администратора.";
                return RedirectToPage();
            }
        }

        user.Role = role;
        await _context.SaveChangesAsync();
        TempData["Success"] = $"Роль пользователя «{user.Username}» изменена на {role}.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteUserAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            TempData["Error"] = "Пользователь не найден.";
            return RedirectToPage();
        }

        // Запрет на удаление самого себя
        var currentUserId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
        if (user.Id == currentUserId)
        {
            TempData["Error"] = "Вы не можете удалить собственную учётную запись.";
            return RedirectToPage();
        }

        // Запрет на удаление последнего администратора
        var adminCount = await _context.Users.CountAsync(u => u.Role == "Admin");
        if (user.Role == "Admin" && adminCount <= 1)
        {
            TempData["Error"] = "Нельзя удалить последнего администратора.";
            return RedirectToPage();
        }

        try
        {
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Пользователь «{user.Username}» удалён.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при удалении пользователя {UserId}", userId);
            TempData["Error"] = "Не удалось удалить пользователя.";
        }

        return RedirectToPage();
    }
}