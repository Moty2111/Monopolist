using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Monoplist.ViewModels;
using Monoplist.Data;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace Monoplist.Pages.Reports;

[Authorize(Roles = "Admin,Manager,Seller")]
public class IndexModel : PageModel
{
    private readonly AppDbContext _context;

    public IndexModel(AppDbContext context)
    {
        _context = context;
    }

    public List<ReportCardViewModel> Reports { get; set; } = new();

    // Свойства для персонализации
    public string Language { get; set; } = "ru";
    public bool CompactMode { get; set; }
    public bool Animations { get; set; } = true;
    public string Theme { get; set; } = "light";
    public string CustomColor { get; set; } = "#FF6B00";

    public async Task OnGetAsync()
    {
        await LoadUserSettings();

        Reports = new List<ReportCardViewModel>
        {
            new()
            {
                Id = "/Reports/SalesReport",
                Title = Language == "ru" ? "Отчет по продажам" : Language == "en" ? "Sales report" : "Сату есебі",
                Description = Language == "ru" ? "Анализ продаж по дням, категориям и способам оплаты" : Language == "en" ? "Sales analysis by day, category and payment method" : "Күндер, санаттар және төлем тәсілдері бойынша сату талдауы",
                Icon = "fa-chart-line",
                Color = "#FF6B00",
                AvailableFormats = new() { "PDF", "Excel", "CSV", "Word" }
            },
            new()
            {
                Id = "/Reports/ProductsReport",
                Title = Language == "ru" ? "Отчет по товарам" : Language == "en" ? "Products report" : "Тауарлар есебі",
                Description = Language == "ru" ? "Состояние запасов, остатки, товары с низким запасом" : Language == "en" ? "Inventory status, stock levels, low stock items" : "Қор жағдайы, қалдықтар, аз қор тауарлары",
                Icon = "fa-box",
                Color = "#3b82f6",
                AvailableFormats = new() { "PDF", "Excel", "CSV", "Word" }
            },
            new()
            {
                Id = "/Reports/CustomersReport",
                Title = Language == "ru" ? "Отчет по клиентам" : Language == "en" ? "Customers report" : "Клиенттер есебі",
                Description = Language == "ru" ? "Активность клиентов, новые регистрации, топ покупателей" : Language == "en" ? "Customer activity, new registrations, top buyers" : "Клиенттер белсенділігі, жаңа тіркелулер, үздік сатып алушылар",
                Icon = "fa-users",
                Color = "#10b981",
                AvailableFormats = new() { "PDF", "Excel", "CSV", "Word" }
            },
            new()
            {
                Id = "/Reports/WarehouseReport",
                Title = Language == "ru" ? "Отчет по складам" : Language == "en" ? "Warehouse report" : "Қоймалар есебі",
                Description = Language == "ru" ? "Загрузка складов, распределение товаров" : Language == "en" ? "Warehouse occupancy, product distribution" : "Қоймалардың жүктелуі, тауарларды бөлу",
                Icon = "fa-warehouse",
                Color = "#8b5cf6",
                AvailableFormats = new() { "PDF", "Excel", "CSV", "Word" }
            }
        };
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
}