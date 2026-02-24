using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Monoplist.Data;
using System.Security.Claims;
using Monoplist.ViewModels;

namespace Monoplist.Pages.Admin.Users;

[Authorize(Roles = "Admin")]
public class DeleteModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly ILogger<DeleteModel> _logger;

    public DeleteModel(AppDbContext context, ILogger<DeleteModel> logger)
    {
        _context = context;
        _logger = logger;
    }

    // Переименовано, чтобы не конфликтовать с User из PageModel
    public UserDeleteViewModel UserToDelete { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
            return NotFound();

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id);
        if (user == null)
            return NotFound();

        UserToDelete = new UserDeleteViewModel
        {
            Id = user.Id,
            Username = user.Username,
            Role = user.Role
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            TempData["Error"] = "Пользователь не найден.";
            return RedirectToPage("./Index");
        }

        // Используем встроенное свойство User для получения текущего пользователя
        var currentUserId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
        if (user.Id == currentUserId)
        {
            TempData["Error"] = "Вы не можете удалить собственную учётную запись.";
            return RedirectToPage("./Index");
        }

        var adminCount = await _context.Users.CountAsync(u => u.Role == "Admin");
        if (user.Role == "Admin" && adminCount <= 1)
        {
            TempData["Error"] = "Нельзя удалить последнего администратора.";
            return RedirectToPage("./Index");
        }

        try
        {
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Пользователь «{user.Username}» удалён.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при удалении пользователя {UserId}", id);
            TempData["Error"] = "Не удалось удалить пользователя.";
        }

        return RedirectToPage("./Index");
    }
}