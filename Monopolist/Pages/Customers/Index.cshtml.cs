// Pages/Customers/Index.cshtml.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Monoplist.Data;
using Monoplist.Models;
using System.Security.Claims;

namespace Monoplist.Pages.Customers;

[Authorize(Roles = "Admin,Manager,Seller")]
public class IndexModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(AppDbContext context, ILogger<IndexModel> logger)
    {
        _context = context;
        _logger = logger;
    }

    [BindProperty(SupportsGet = true)]
    public string? SearchString { get; set; }

    // Параметры сортировки
    [BindProperty(SupportsGet = true)]
    public string? SortField { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SortOrder { get; set; } // "asc" или "desc"

    public IList<Customer> Customers { get; set; } = new List<Customer>();

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
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                Language = user.Language ?? "ru";
                CompactMode = user.CompactMode;
                Animations = user.Animations;
                Theme = user.Theme ?? "light";
                CustomColor = user.CustomColor ?? "#FF6B00";
            }

            // Устанавливаем значения по умолчанию для сортировки
            SortField = string.IsNullOrEmpty(SortField) ? "FullName" : SortField;
            SortOrder = string.IsNullOrEmpty(SortOrder) ? "asc" : SortOrder;

            var query = _context.Customers.AsQueryable();

            // Фильтрация
            if (!string.IsNullOrEmpty(SearchString))
            {
                query = query.Where(c =>
                    EF.Functions.Like(c.FullName, $"%{SearchString}%") ||
                    EF.Functions.Like(c.Phone, $"%{SearchString}%") ||
                    EF.Functions.Like(c.Email, $"%{SearchString}%"));
            }

            // Сортировка
            query = SortField switch
            {
                "Phone" => SortOrder == "asc" ? query.OrderBy(c => c.Phone) : query.OrderByDescending(c => c.Phone),
                "Email" => SortOrder == "asc" ? query.OrderBy(c => c.Email) : query.OrderByDescending(c => c.Email),
                "Discount" => SortOrder == "asc" ? query.OrderBy(c => c.Discount) : query.OrderByDescending(c => c.Discount),
                "RegistrationDate" => SortOrder == "asc" ? query.OrderBy(c => c.RegistrationDate) : query.OrderByDescending(c => c.RegistrationDate),
                _ => SortOrder == "asc" ? query.OrderBy(c => c.FullName) : query.OrderByDescending(c => c.FullName)
            };

            Customers = await query.ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при загрузке списка клиентов");
            TempData["Error"] = GetLocalizedMessage("Не удалось загрузить список клиентов.", "Failed to load customers.", "Клиенттер тізімін жүктеу мүмкін болмады.");
        }
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
}