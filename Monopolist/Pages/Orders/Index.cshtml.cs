// Pages/Orders/Index.cshtml.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Monoplist.Data;
using Monoplist.ViewModels;
using System.Security.Claims;

namespace Monoplist.Pages.Orders;

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
    public string? SortOrder { get; set; }

    public IList<OrderIndexViewModel> Orders { get; set; } = new List<OrderIndexViewModel>();

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
            SortField = string.IsNullOrEmpty(SortField) ? "OrderDate" : SortField;
            SortOrder = string.IsNullOrEmpty(SortOrder) ? "desc" : SortOrder; // по умолчанию по убыванию даты

            var query = _context.Orders
                .Include(o => o.Customer)
                .AsQueryable();

            if (!string.IsNullOrEmpty(SearchString))
            {
                query = query.Where(o =>
                    EF.Functions.Like(o.OrderNumber, $"%{SearchString}%") ||
                    (o.Customer != null && EF.Functions.Like(o.Customer.FullName, $"%{SearchString}%")));
            }

            // Сортировка
            query = SortField switch
            {
                "OrderNumber" => SortOrder == "asc"
                    ? query.OrderBy(o => o.OrderNumber)
                    : query.OrderByDescending(o => o.OrderNumber),
                "CustomerName" => SortOrder == "asc"
                    ? query.OrderBy(o => o.Customer.FullName)
                    : query.OrderByDescending(o => o.Customer.FullName),
                "TotalAmount" => SortOrder == "asc"
                    ? query.OrderBy(o => o.TotalAmount)
                    : query.OrderByDescending(o => o.TotalAmount),
                "Status" => SortOrder == "asc"
                    ? query.OrderBy(o => o.Status)
                    : query.OrderByDescending(o => o.Status),
                "PaymentMethod" => SortOrder == "asc"
                    ? query.OrderBy(o => o.PaymentMethod)
                    : query.OrderByDescending(o => o.PaymentMethod),
                _ => SortOrder == "asc"
                    ? query.OrderBy(o => o.OrderDate)
                    : query.OrderByDescending(o => o.OrderDate)
            };

            Orders = await query
                .Select(o => new OrderIndexViewModel
                {
                    Id = o.Id,
                    OrderNumber = o.OrderNumber,
                    CustomerName = o.Customer != null ? o.Customer.FullName : GetLocalizedMessage("Неизвестно", "Unknown", "Белгісіз"),
                    OrderDate = o.OrderDate,
                    TotalAmount = o.TotalAmount,
                    Status = o.Status,
                    PaymentMethod = o.PaymentMethod
                })
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при загрузке списка заказов");
            TempData["Error"] = GetLocalizedMessage("Не удалось загрузить список заказов.", "Failed to load orders.", "Тапсырыстар тізімін жүктеу мүмкін болмады.");
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