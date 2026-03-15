using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Monoplist.Data;
using Monoplist.ViewModels;
using System.Security.Claims;

namespace Monoplist.Pages.Reports;

[Authorize(Roles = "Admin,Manager,Seller")]
public class SalesReportModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly ILogger<SalesReportModel> _logger;

    public SalesReportModel(AppDbContext context, ILogger<SalesReportModel> logger)
    {
        _context = context;
        _logger = logger;
    }

    [BindProperty]
    public SalesReportViewModel Report { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public DateTime? StartDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? EndDate { get; set; }

    // Свойства для персонализации
    public string Language { get; set; } = "ru";
    public bool CompactMode { get; set; }
    public bool Animations { get; set; } = true;
    public string Theme { get; set; } = "light";
    public string CustomColor { get; set; } = "#FF6B00";

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadUserSettings();

        Report.StartDate = StartDate ?? DateTime.Now.AddMonths(-1).Date;
        Report.EndDate = EndDate ?? DateTime.Now.Date;

        await LoadReportData();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadUserSettings();
        await LoadReportData();
        return Page();
    }

    private async Task LoadReportData()
    {
        try
        {
            var endDate = Report.EndDate.AddDays(1).AddSeconds(-1);

            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                        .ThenInclude(p => p.Category)
                .Include(o => o.Customer)
                .Where(o => o.OrderDate >= Report.StartDate && o.OrderDate <= endDate)
                .ToListAsync();

            if (!orders.Any()) return;

            Report.TotalOrders = orders.Count;
            Report.TotalRevenue = orders.Sum(o => o.TotalAmount);
            Report.AverageOrderValue = Report.TotalOrders > 0 ? Report.TotalRevenue / Report.TotalOrders : 0;

            Report.DailySales = orders
                .GroupBy(o => o.OrderDate.Date)
                .Select(g => new DailySalesViewModel
                {
                    Date = g.Key,
                    DayName = g.Key.ToString("dd.MM"),
                    Revenue = g.Sum(o => o.TotalAmount),
                    OrdersCount = g.Count()
                })
                .OrderBy(d => d.Date)
                .ToList();

            var categorySales = new Dictionary<string, (decimal Total, int Count)>();

            foreach (var order in orders)
            {
                foreach (var item in order.OrderItems)
                {
                    var categoryName = item.Product?.Category?.Name ?? "Без категории";
                    var total = item.Quantity * item.PriceAtSale;

                    if (categorySales.ContainsKey(categoryName))
                    {
                        var current = categorySales[categoryName];
                        categorySales[categoryName] = (current.Total + total, current.Count + item.Quantity);
                    }
                    else
                    {
                        categorySales[categoryName] = (total, item.Quantity);
                    }
                }
            }

            var totalSales = categorySales.Sum(c => c.Value.Total);
            Report.CategorySales = categorySales
                .Select(c => new CategorySalesViewModel
                {
                    CategoryName = c.Key,
                    TotalAmount = c.Value.Total,
                    ItemsSold = c.Value.Count,
                    Percentage = totalSales > 0 ? (double)(c.Value.Total / totalSales * 100) : 0
                })
                .OrderByDescending(c => c.TotalAmount)
                .ToList();

            Report.PaymentMethods = orders
                .GroupBy(o => o.PaymentMethod ?? "Не указан")
                .Select(g => new PaymentMethodViewModel
                {
                    Method = g.Key,
                    MethodDisplay = g.Key switch
                    {
                        "Card" => Language == "ru" ? "Карта" : Language == "en" ? "Card" : "Карта",
                        "Cash" => Language == "ru" ? "Наличные" : Language == "en" ? "Cash" : "Қолма-қол",
                        "Credit" => Language == "ru" ? "Кредит" : Language == "en" ? "Credit" : "Несие",
                        _ => g.Key
                    },
                    Count = g.Count(),
                    Total = g.Sum(o => o.TotalAmount)
                })
                .OrderByDescending(p => p.Total)
                .ToList();

            Report.RecentOrders = await _context.Orders
                .Include(o => o.Customer)
                .Where(o => o.OrderDate >= Report.StartDate && o.OrderDate <= endDate)
                .OrderByDescending(o => o.OrderDate)
                .Take(50)
                .Select(o => new OrderInfoViewModel
                {
                    OrderDate = o.OrderDate,
                    OrderNumber = o.OrderNumber,
                    CustomerName = o.Customer != null ? o.Customer.FullName : "Неизвестно",
                    PaymentMethod = o.PaymentMethod ?? "Не указан",
                    TotalAmount = o.TotalAmount
                })
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при загрузке отчета по продажам");
            TempData["Error"] = GetLocalizedMessage("Не удалось загрузить данные отчета.", "Failed to load report data.", "Есеп деректерін жүктеу мүмкін болмады.");
        }
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