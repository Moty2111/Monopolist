using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Monoplist.Data;
using Monoplist.ViewModels;
using System.Security.Claims;

namespace Monoplist.Pages.Reports;

[Authorize(Roles = "Admin,Manager,Seller")]
public class CustomersReportModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly ILogger<CustomersReportModel> _logger;

    public CustomersReportModel(AppDbContext context, ILogger<CustomersReportModel> logger)
    {
        _context = context;
        _logger = logger;
    }

    public CustomersReportViewModel Report { get; set; } = new();

    // Свойства для персонализации
    public string Language { get; set; } = "ru";
    public bool CompactMode { get; set; }
    public bool Animations { get; set; } = true;
    public string Theme { get; set; } = "light";
    public string CustomColor { get; set; } = "#FF6B00";

    public async Task OnGetAsync()
    {
        await LoadUserSettings();
        await LoadReportData();
    }

    private async Task LoadReportData()
    {
        try
        {
            var customers = await _context.Customers
                .Include(c => c.Orders)
                .ToListAsync();

            Report.TotalCustomers = customers.Count;
            Report.NewCustomersThisMonth = customers.Count(c => c.RegistrationDate >= DateTime.Now.AddMonths(-1));
            Report.ActiveCustomers = customers.Count(c => c.Orders != null && c.Orders.Any(o => o.OrderDate >= DateTime.Now.AddMonths(-3)));

            // Топ клиентов по сумме заказов
            Report.TopCustomers = customers
                .Where(c => c.Orders != null && c.Orders.Any())
                .Select(c => new TopCustomerViewModel
                {
                    Id = c.Id,
                    FullName = c.FullName,
                    Phone = c.Phone ?? "-",
                    Email = c.Email ?? "-",
                    TotalSpent = c.Orders!.Sum(o => o.TotalAmount),
                    OrdersCount = c.Orders!.Count,
                    LastOrderDate = c.Orders!.Max(o => o.OrderDate)
                })
                .OrderByDescending(c => c.TotalSpent)
                .Take(10)
                .ToList();

            // Регистрации по месяцам (последние 6 месяцев)
            var sixMonthsAgo = DateTime.Now.AddMonths(-6);
            var registrations = customers
                .Where(c => c.RegistrationDate >= sixMonthsAgo)
                .GroupBy(c => new { c.RegistrationDate.Year, c.RegistrationDate.Month })
                .Select(g => new CustomerRegistrationViewModel
                {
                    Period = $"{g.Key.Year}-{g.Key.Month:D2}",
                    Count = g.Count()
                })
                .OrderBy(g => g.Period)
                .ToList();

            Report.Registrations = registrations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при загрузке отчета по клиентам");
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