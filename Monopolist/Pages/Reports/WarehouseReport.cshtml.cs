using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Monoplist.Data;
using Monoplist.ViewModels;

namespace Monoplist.Pages.Reports;

[Authorize(Roles = "Admin,Manager,Seller")]
public class WarehouseReportModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly ILogger<WarehouseReportModel> _logger;

    public WarehouseReportModel(AppDbContext context, ILogger<WarehouseReportModel> logger)
    {
        _context = context;
        _logger = logger;
    }

    public WarehouseReportViewModel Report { get; set; } = new();

    public async Task OnGetAsync()
    {
        await LoadReportData();
    }

    private async Task LoadReportData()
    {
        try
        {
            var warehouses = await _context.Warehouses
                .Include(w => w.Products)
                .ToListAsync();

            Report.TotalWarehouses = warehouses.Count;
            Report.TotalCapacity = warehouses.Sum(w => w.Capacity);
            Report.CurrentOccupancy = warehouses.Sum(w => w.Products?.Sum(p => p.CurrentStock) ?? 0);
            Report.OccupancyPercent = Report.TotalCapacity > 0 ? (Report.CurrentOccupancy * 100.0 / Report.TotalCapacity) : 0;

            Report.Warehouses = warehouses.Select(w => new WarehouseOccupancyViewModel
            {
                Id = w.Id,
                Name = w.Name,
                Location = w.Location ?? "Не указано",
                Capacity = w.Capacity,
                CurrentOccupancy = w.Products?.Sum(p => p.CurrentStock) ?? 0,
                ProductsCount = w.Products?.Count ?? 0
            }).OrderBy(w => w.Name).ToList();

            // Распределение товаров по складам (первые 100 для производительности)
            Report.ProductLocations = await _context.Products
                .Include(p => p.Warehouse)
                .Where(p => p.Warehouse != null)
                .Select(p => new ProductLocationViewModel
                {
                    ProductName = p.Name,
                    Article = p.Article ?? "-",
                    WarehouseName = p.Warehouse!.Name,
                    Stock = p.CurrentStock,
                    Unit = p.Unit
                })
                .OrderBy(p => p.WarehouseName)
                .ThenBy(p => p.ProductName)
                .Take(100)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при загрузке отчета по складам");
            TempData["Error"] = "Не удалось загрузить данные отчета.";
        }
    }
}