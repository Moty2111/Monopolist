using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Monoplist.ViewModels;
using Monoplist.Data;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Monoplist.Pages;

[Authorize]
public class IndexModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(AppDbContext context, ILogger<IndexModel> logger)
    {
        _context = context;
        _logger = logger;
    }

    public DashboardViewModel DashboardData { get; set; } = new();

    public async Task OnGetAsync()
    {
        try
        {
            // Загружаем данные из БД
            var totalRevenue = await _context.Orders
                .Where(o => o.Status == "Completed")
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

            var productsInStock = await _context.Products
                .SumAsync(p => (int?)p.CurrentStock) ?? 0;

            var totalCustomers = await _context.Customers.CountAsync();

            var newOrders = await _context.Orders
                .CountAsync(o => o.OrderDate >= DateTime.UtcNow.AddDays(-7));

            var latestOrders = await _context.Orders
                .Include(o => o.Customer)
                .OrderByDescending(o => o.OrderDate)
                .Take(5)
                .Select(o => new OrderSummaryViewModel
                {
                    OrderNumber = o.OrderNumber,
                    CustomerName = o.Customer != null ? o.Customer.FullName : "Неизвестно",
                    TotalAmount = o.TotalAmount,
                    Status = o.Status
                })
                .ToListAsync();

            var lowStockProducts = await _context.Products
                .Where(p => p.CurrentStock <= p.MinimumStock)
                .Include(p => p.Category)
                .OrderBy(p => p.CurrentStock)
                .Take(5)
                .Select(p => new LowStockProductViewModel
                {
                    Name = p.Name,
                    CategoryName = p.Category != null ? p.Category.Name : "Без категории",
                    CurrentStock = p.CurrentStock,
                    MinimumStock = p.MinimumStock
                })
                .ToListAsync();

            DashboardData.TotalRevenue = totalRevenue;
            DashboardData.ProductsInStock = productsInStock;
            DashboardData.TotalCustomers = totalCustomers;
            DashboardData.NewOrders = newOrders;
            DashboardData.LatestOrders = latestOrders;
            DashboardData.LowStockProducts = lowStockProducts;

            // Если списки пусты (нет данных), подставляем тестовые и показываем предупреждение
            bool usingSampleData = false;
            if (!DashboardData.LatestOrders.Any())
            {
                DashboardData.LatestOrders = GetSampleOrders();
                usingSampleData = true;
            }

            if (!DashboardData.LowStockProducts.Any())
            {
                DashboardData.LowStockProducts = GetSampleLowStockProducts();
                usingSampleData = true;
            }

            if (usingSampleData)
            {
                TempData["Warning"] = "Некоторые данные отсутствуют в БД. Показаны примеры.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при загрузке дашборда");
            // При ошибке полностью заполняем тестовыми данными
            DashboardData.TotalRevenue = 1_245_000;
            DashboardData.ProductsInStock = 3_245;
            DashboardData.TotalCustomers = 128;
            DashboardData.NewOrders = 18;
            DashboardData.LatestOrders = GetSampleOrders();
            DashboardData.LowStockProducts = GetSampleLowStockProducts();

            TempData["Error"] = "Не удалось загрузить данные из БД. Показаны тестовые данные.";
        }
    }

    // Тестовые данные для демонстрации
    private List<OrderSummaryViewModel> GetSampleOrders()
    {
        return new List<OrderSummaryViewModel>
        {
            new OrderSummaryViewModel { OrderNumber = "ЗАКАЗ-001", CustomerName = "ООО СтройМаркет", TotalAmount = 45000, Status = "Completed" },
            new OrderSummaryViewModel { OrderNumber = "ЗАКАЗ-002", CustomerName = "ИП Петров", TotalAmount = 12800, Status = "Pending" },
            new OrderSummaryViewModel { OrderNumber = "ЗАКАЗ-003", CustomerName = "ООО ДомСтрой", TotalAmount = 89000, Status = "Completed" },
            new OrderSummaryViewModel { OrderNumber = "ЗАКАЗ-004", CustomerName = "АО ЖБИ", TotalAmount = 23400, Status = "Cancelled" },
            new OrderSummaryViewModel { OrderNumber = "ЗАКАЗ-005", CustomerName = "ООО РемонтСервис", TotalAmount = 56700, Status = "Pending" }
        };
    }

    private List<LowStockProductViewModel> GetSampleLowStockProducts()
    {
        return new List<LowStockProductViewModel>
        {
            new LowStockProductViewModel { Name = "Цемент М500", CategoryName = "Сыпучие материалы", CurrentStock = 10, MinimumStock = 50 },
            new LowStockProductViewModel { Name = "Кирпич красный", CategoryName = "Стеновые материалы", CurrentStock = 200, MinimumStock = 500 },
            new LowStockProductViewModel { Name = "Арматура 12 мм", CategoryName = "Металлопрокат", CurrentStock = 8, MinimumStock = 20 },
            new LowStockProductViewModel { Name = "Гвозди 100мм", CategoryName = "Метизы", CurrentStock = 2, MinimumStock = 15 },
            new LowStockProductViewModel { Name = "Пена монтажная", CategoryName = "Химия", CurrentStock = 3, MinimumStock = 10 }
        };
    }
}