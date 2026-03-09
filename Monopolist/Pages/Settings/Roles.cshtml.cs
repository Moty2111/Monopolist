// Pages/Settings/Roles.cshtml.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Monoplist.Data;
using Monoplist.ViewModels;
using System.Security.Claims;

namespace Monoplist.Pages.Settings;

[Authorize(Roles = "Admin")]
public class RolesModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly ILogger<RolesModel> _logger;

    public RolesModel(AppDbContext context, ILogger<RolesModel> logger)
    {
        _context = context;
        _logger = logger;
    }

    public List<RoleViewModel> Roles { get; set; } = new();

    // Свойства для персонализации
    public string Language { get; set; } = "ru";
    public bool CompactMode { get; set; }
    public bool Animations { get; set; } = true;
    public string Theme { get; set; } = "light";
    public string CustomColor { get; set; } = "#FF6B00";

    public async Task OnGetAsync()
    {
        try
        {
            // Загружаем настройки текущего пользователя
            var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
            var currentUser = await _context.Users.FindAsync(userId);
            if (currentUser != null)
            {
                Language = currentUser.Language ?? "ru";
                CompactMode = currentUser.CompactMode;
                Animations = currentUser.Animations;
                Theme = currentUser.Theme ?? "light";
                CustomColor = currentUser.CustomColor ?? "#FF6B00";
            }

            // Получаем все уникальные роли из таблицы Users
            var roleNames = await _context.Users
                .Select(u => u.Role)
                .Distinct()
                .ToListAsync();

            var roles = new List<RoleViewModel>();

            foreach (var roleName in roleNames)
            {
                var userCount = await _context.Users.CountAsync(u => u.Role == roleName);

                // Для демонстрации добавим предопределенные разрешения в зависимости от роли
                var permissions = new List<string>();
                switch (roleName.ToLower())
                {
                    case "admin":
                        permissions = new List<string> { "Полный доступ", "Управление пользователями", "Все отчеты" };
                        break;
                    case "manager":
                        permissions = new List<string> { "Управление заказами", "Управление клиентами", "Просмотр отчетов" };
                        break;
                    case "seller":
                        permissions = new List<string> { "Создание заказов", "Просмотр товаров", "Просмотр клиентов" };
                        break;
                    default:
                        permissions = new List<string> { "Доступ к системе" };
                        break;
                }

                roles.Add(new RoleViewModel
                {
                    Name = roleName,
                    UserCount = userCount,
                    Permissions = permissions
                });
            }

            // Сортируем по имени
            Roles = roles.OrderBy(r => r.Name).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при загрузке ролей");
            TempData["Error"] = "Не удалось загрузить данные ролей.";
        }
    }
}