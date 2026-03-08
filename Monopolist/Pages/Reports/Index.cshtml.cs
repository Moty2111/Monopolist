using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Monoplist.ViewModels;

namespace Monoplist.Pages.Reports;

[Authorize(Roles = "Admin,Manager,Seller")]
public class IndexModel : PageModel
{
    public List<ReportCardViewModel> Reports { get; set; } = new();

    public void OnGet()
    {
        Reports = new List<ReportCardViewModel>
        {
            new()
            {
                Id = "/Reports/SalesReport", // полный путь к странице
                Title = "Отчет по продажам",
                Description = "Анализ продаж по дням, категориям и способам оплаты",
                Icon = "fa-chart-line",
                Color = "#FF6B00",
                AvailableFormats = new() { "PDF", "Excel", "CSV", "График" }
            },
            new()
            {
                Id = "/Reports/ProductsReport",
                Title = "Отчет по товарам",
                Description = "Состояние запасов, остатки, товары с низким запасом",
                Icon = "fa-box",
                Color = "#3b82f6",
                AvailableFormats = new() { "PDF", "Excel", "CSV", "График" }
            },
            new()
            {
                Id = "/Reports/CustomersReport",
                Title = "Отчет по клиентам",
                Description = "Активность клиентов, новые регистрации, топ покупателей",
                Icon = "fa-users",
                Color = "#10b981",
                AvailableFormats = new() { "PDF", "Excel", "CSV", "График" }
            },
            new()
            {
                Id = "/Reports/WarehouseReport",
                Title = "Отчет по складам",
                Description = "Загрузка складов, распределение товаров",
                Icon = "fa-warehouse",
                Color = "#8b5cf6",
                AvailableFormats = new() { "PDF", "Excel", "CSV", "График" }
            }
        };
    }
}