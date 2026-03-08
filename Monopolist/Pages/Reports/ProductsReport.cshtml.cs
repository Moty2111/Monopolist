using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Monoplist.Data;
using Monoplist.ViewModels;

namespace Monoplist.Pages.Reports;

[Authorize(Roles = "Admin,Manager,Seller")]
public class ProductsReportModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly ILogger<ProductsReportModel> _logger;

    public ProductsReportModel(AppDbContext context, ILogger<ProductsReportModel> logger)
    {
        _context = context;
        _logger = logger;
    }

    public ProductsReportViewModel Report { get; set; } = new();

    public async Task OnGetAsync()
    {
        await LoadReportData();
    }

    private async Task LoadReportData()
    {
        try
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Warehouse)
                .ToListAsync();

            Report.TotalProducts = products.Count;
            Report.LowStockCount = products.Count(p => p.CurrentStock > 0 && p.CurrentStock < p.MinimumStock);
            Report.OutOfStockCount = products.Count(p => p.CurrentStock == 0);
            Report.TotalInventoryValue = products.Sum(p => p.CurrentStock * p.PurchasePrice);

            // Топ товаров по стоимости запасов
            Report.TopProducts = products
                .OrderByDescending(p => p.CurrentStock * p.PurchasePrice)
                .Take(10)
                .Select(p => new ProductStockViewModel
                {
                    Id = p.Id,
                    Name = p.Name,
                    Article = p.Article ?? "-",
                    Category = p.Category?.Name ?? "Без категории",
                    CurrentStock = p.CurrentStock,
                    MinimumStock = p.MinimumStock,
                    PurchasePrice = p.PurchasePrice,
                    SalePrice = p.SalePrice
                })
                .ToList();

            // Товары с низким остатком
            Report.LowStockProducts = products
                .Where(p => p.CurrentStock > 0 && p.CurrentStock < p.MinimumStock)
                .OrderBy(p => (double)p.CurrentStock / p.MinimumStock)
                .Take(20)
                .Select(p => new ProductStockViewModel
                {
                    Id = p.Id,
                    Name = p.Name,
                    Article = p.Article ?? "-",
                    Category = p.Category?.Name ?? "Без категории",
                    CurrentStock = p.CurrentStock,
                    MinimumStock = p.MinimumStock,
                    PurchasePrice = p.PurchasePrice,
                    SalePrice = p.SalePrice
                })
                .ToList();

            // Статистика по категориям
            Report.CategoryStock = products
                .GroupBy(p => p.Category?.Name ?? "Без категории")
                .Select(g => new CategoryStockViewModel
                {
                    CategoryName = g.Key,
                    ProductsCount = g.Count(),
                    TotalStock = g.Sum(p => p.CurrentStock),
                    TotalValue = g.Sum(p => p.CurrentStock * p.PurchasePrice)
                })
                .OrderByDescending(c => c.TotalValue)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при загрузке отчета по товарам");
            TempData["Error"] = "Не удалось загрузить данные отчета.";
        }
    }
}