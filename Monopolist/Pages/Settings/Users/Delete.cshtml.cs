using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Monoplist.Data;

namespace Monoplist.Pages.Settings.Users;

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

    /// <summary>
    /// Модель для отображения информации о пользователе перед удалением.
    /// </summary>
    public class UserInfoModel
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public UserInfoModel UserInfo { get; set; } = new();

    /// <summary>
    /// Загрузка данных пользователя для подтверждения удаления.
    /// </summary>
    /// <param name="id">Идентификатор пользователя.</param>
    /// <returns>Страница с подтверждением или редирект при ошибке.</returns>
    public async Task<IActionResult> OnGetAsync(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            TempData["Error"] = "Пользователь не найден.";
            return RedirectToPage("./Index");
        }

        // Запрет на удаление самого себя
        var currentUserId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
        if (user.Id == currentUserId)
        {
            TempData["Error"] = "Вы не можете удалить собственную учётную запись.";
            return RedirectToPage("./Index");
        }

        UserInfo.Id = user.Id;
        UserInfo.Username = user.Username;
        UserInfo.Role = user.Role;
        UserInfo.CreatedAt = user.CreatedAt;

        return Page();
    }

    /// <summary>
    /// Обработка подтверждения удаления.
    /// </summary>
    /// <param name="id">Идентификатор пользователя.</param>
    /// <returns>Редирект на список пользователей с сообщением.</returns>
    public async Task<IActionResult> OnPostAsync(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            TempData["Error"] = "Пользователь не найден.";
            return RedirectToPage("./Index");
        }

        // Повторная проверка на удаление самого себя (на случай, если запрос подделан)
        var currentUserId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
        if (user.Id == currentUserId)
        {
            TempData["Error"] = "Вы не можете удалить собственную учётную запись.";
            return RedirectToPage("./Index");
        }

        // Проверка на последнего администратора
        if (user.Role == "Admin")
        {
            var adminCount = await _context.Users.CountAsync(u => u.Role == "Admin");
            if (adminCount <= 1)
            {
                TempData["Error"] = "Нельзя удалить последнего администратора.";
                return RedirectToPage("./Index");
            }
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