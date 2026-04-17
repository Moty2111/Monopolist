using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Monoplist.ViewModels;
using Monoplist.Data;
using Monoplist.Models;
using System.Security.Claims;
using System;
using System.Collections.Generic;
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

    // Свойства для персонализации
    public string Language { get; set; } = "ru";
    public bool CompactMode { get; set; }
    public bool Animations { get; set; } = true;
    public string Theme { get; set; } = "light";
    public string CustomColor { get; set; } = "#FF6B00";

    public async Task OnGetAsync()
    {
        await LoadUserSettings();

        try
        {
            // Основные показатели
            var totalRevenue = await _context.Orders
                .Where(o => o.Status == "Completed")
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

            var productsInStock = await _context.Products
                .SumAsync(p => (int?)p.CurrentStock) ?? 0;

            var totalCustomers = await _context.Customers.CountAsync();

            var newOrders = await _context.Orders
                .CountAsync(o => o.OrderDate >= DateTime.UtcNow.AddDays(-7));

            // Последние заказы
            var latestOrders = await _context.Orders
                .Include(o => o.Customer)
                .OrderByDescending(o => o.OrderDate)
                .Take(5)
                .Select(o => new OrderSummaryViewModel
                {
                    OrderNumber = o.OrderNumber,
                    CustomerName = o.Customer != null ? o.Customer.FullName : GetLocalizedMessage("Неизвестно", "Unknown", "Белгісіз"),
                    TotalAmount = o.TotalAmount,
                    Status = o.Status
                })
                .ToListAsync();

            // Товары с низким остатком
            var lowStockProducts = await _context.Products
                .Where(p => p.CurrentStock <= p.MinimumStock)
                .Include(p => p.Category)
                .OrderBy(p => p.CurrentStock)
                .Take(5)
                .Select(p => new LowStockProductViewModel
                {
                    Name = p.Name,
                    CategoryName = p.Category != null ? p.Category.Name : GetLocalizedMessage("Без категории", "Uncategorized", "Санатсыз"),
                    CurrentStock = p.CurrentStock,
                    MinimumStock = p.MinimumStock
                })
                .ToListAsync();

            // Данные для графика продаж по дням (последние 7 дней)
            var salesData = new List<DailySalesViewModel>();
            for (int i = 6; i >= 0; i--)
            {
                var date = DateTime.UtcNow.Date.AddDays(-i);
                var dailyTotal = await _context.Orders
                    .Where(o => o.OrderDate.Date == date && o.Status == "Completed")
                    .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

                var ordersCount = await _context.Orders
                    .CountAsync(o => o.OrderDate.Date == date && o.Status == "Completed");

                salesData.Add(new DailySalesViewModel
                {
                    Date = date,
                    Revenue = dailyTotal,
                    OrdersCount = ordersCount,
                    DayName = date.ToString("dddd", new System.Globalization.CultureInfo(
                        Language == "ru" ? "ru-RU" : Language == "en" ? "en-US" : "kk-KZ"))
                });
            }

            // Данные для графика популярных категорий (топ-5)
            var categorySales = await _context.OrderItems
                .Include(oi => oi.Product)
                    .ThenInclude(p => p.Category)
                .Where(oi => oi.Order.Status == "Completed")
                .GroupBy(oi => oi.Product.Category)
                .Select(g => new CategorySalesViewModel
                {
                    CategoryName = g.Key != null ? g.Key.Name : GetLocalizedMessage("Без категории", "Uncategorized", "Санатсыз"),
                    ItemsSold = g.Sum(oi => oi.Quantity),
                    TotalAmount = g.Sum(oi => oi.Quantity * oi.PriceAtSale)
                })
                .OrderByDescending(c => c.ItemsSold)
                .Take(5)
                .ToListAsync();

            // Вычисляем процент для каждой категории
            var totalSold = categorySales.Sum(c => c.ItemsSold);
            foreach (var cat in categorySales)
            {
                cat.Percentage = totalSold > 0 ? (double)cat.ItemsSold / totalSold * 100 : 0;
            }

            // Прогноз продаж на следующую неделю (простая линейная аппроксимация)
            decimal forecast = 0;
            if (salesData.Count > 1)
            {
                var lastThree = salesData.Where(s => s.Date >= DateTime.UtcNow.Date.AddDays(-2)).ToList();
                if (lastThree.Count > 1)
                {
                    var avgIncrease = (lastThree.Last().Revenue - lastThree.First().Revenue) / (lastThree.Count - 1);
                    forecast = salesData.Last().Revenue + avgIncrease * 7;
                    if (forecast < 0) forecast = 0;
                }
            }

            // Топ-5 товаров по продажам (только завершённые заказы)
            var topProducts = await _context.OrderItems
                .Include(oi => oi.Product)
                .Where(oi => oi.Order.Status == "Completed")
                .GroupBy(oi => oi.Product)
                .Select(g => new TopProductViewModel
                {
                    Name = g.Key.Name,
                    TotalSold = g.Sum(oi => oi.Quantity),
                    TotalRevenue = g.Sum(oi => oi.Quantity * oi.PriceAtSale),
                    Unit = g.Key.Unit ?? "шт"
                })
                .OrderByDescending(tp => tp.TotalSold)
                .Take(5)
                .ToListAsync();

            DashboardData.TotalRevenue = totalRevenue;
            DashboardData.ProductsInStock = productsInStock;
            DashboardData.TotalCustomers = totalCustomers;
            DashboardData.NewOrders = newOrders;
            DashboardData.LatestOrders = latestOrders;
            DashboardData.LowStockProducts = lowStockProducts;
            DashboardData.SalesData = salesData;
            DashboardData.CategorySales = categorySales;
            DashboardData.Forecast = forecast;
            DashboardData.TopProducts = topProducts;
            DashboardData.IsUsingSampleData = false;

            // Если какие-то данные отсутствуют, подставляем демо-данные
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

            if (!DashboardData.SalesData.Any(d => d.Revenue > 0))
            {
                DashboardData.SalesData = GetSampleSalesData();
                usingSampleData = true;
            }

            if (!DashboardData.CategorySales.Any())
            {
                DashboardData.CategorySales = GetSampleCategorySales();
                usingSampleData = true;
            }

            if (!DashboardData.TopProducts.Any())
            {
                DashboardData.TopProducts = GetSampleTopProducts();
                usingSampleData = true;
            }

            DashboardData.IsUsingSampleData = usingSampleData;
            if (usingSampleData)
            {
                TempData["Warning"] = GetLocalizedMessage(
                    "Некоторые данные отсутствуют в БД. Показаны тестовые значения (отмечены *).",
                    "Some data is missing in the database. Test values are shown (marked *).",
                    "ДБ-де кейбір деректер жоқ. Сынақ мәндері көрсетілген (* белгісімен).");
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
            DashboardData.SalesData = GetSampleSalesData();
            DashboardData.CategorySales = GetSampleCategorySales();
            DashboardData.Forecast = 45000;
            DashboardData.TopProducts = GetSampleTopProducts();
            DashboardData.IsUsingSampleData = true;

            TempData["Error"] = GetLocalizedMessage(
                "Не удалось загрузить данные из БД. Показаны тестовые данные.",
                "Failed to load data from the database. Test data is shown.",
                "ДБ-ден деректерді жүктеу мүмкін болмады. Сынақ деректері көрсетілген.");
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
            new LowStockProductViewModel { Name = "Цемент М500 *", CategoryName = "Сыпучие материалы", CurrentStock = 10, MinimumStock = 50 },
            new LowStockProductViewModel { Name = "Кирпич красный *", CategoryName = "Стеновые материалы", CurrentStock = 200, MinimumStock = 500 },
            new LowStockProductViewModel { Name = "Арматура 12 мм *", CategoryName = "Металлопрокат", CurrentStock = 8, MinimumStock = 20 },
            new LowStockProductViewModel { Name = "Гвозди 100мм *", CategoryName = "Метизы", CurrentStock = 2, MinimumStock = 15 },
            new LowStockProductViewModel { Name = "Пена монтажная *", CategoryName = "Химия", CurrentStock = 3, MinimumStock = 10 }
        };
    }

    private List<DailySalesViewModel> GetSampleSalesData()
    {
        var today = DateTime.UtcNow.Date;
        var culture = new System.Globalization.CultureInfo(Language == "ru" ? "ru-RU" : Language == "en" ? "en-US" : "kk-KZ");
        return new List<DailySalesViewModel>
        {
            new DailySalesViewModel { Date = today.AddDays(-6), Revenue = 12000, OrdersCount = 5, DayName = today.AddDays(-6).ToString("dddd", culture) },
            new DailySalesViewModel { Date = today.AddDays(-5), Revenue = 15000, OrdersCount = 6, DayName = today.AddDays(-5).ToString("dddd", culture) },
            new DailySalesViewModel { Date = today.AddDays(-4), Revenue = 11000, OrdersCount = 4, DayName = today.AddDays(-4).ToString("dddd", culture) },
            new DailySalesViewModel { Date = today.AddDays(-3), Revenue = 18000, OrdersCount = 7, DayName = today.AddDays(-3).ToString("dddd", culture) },
            new DailySalesViewModel { Date = today.AddDays(-2), Revenue = 22000, OrdersCount = 9, DayName = today.AddDays(-2).ToString("dddd", culture) },
            new DailySalesViewModel { Date = today.AddDays(-1), Revenue = 19000, OrdersCount = 8, DayName = today.AddDays(-1).ToString("dddd", culture) },
            new DailySalesViewModel { Date = today, Revenue = 21000, OrdersCount = 8, DayName = today.ToString("dddd", culture) }
        };
    }

    private List<CategorySalesViewModel> GetSampleCategorySales()
    {
        return new List<CategorySalesViewModel>
        {
            new CategorySalesViewModel { CategoryName = "Сыпучие материалы *", ItemsSold = 450, TotalAmount = 450 * 350, Percentage = 37.5 },
            new CategorySalesViewModel { CategoryName = "Отделочные материалы *", ItemsSold = 320, TotalAmount = 320 * 500, Percentage = 26.7 },
            new CategorySalesViewModel { CategoryName = "Крепёж *", ItemsSold = 210, TotalAmount = 210 * 180, Percentage = 17.5 },
            new CategorySalesViewModel { CategoryName = "Лакокрасочные *", ItemsSold = 150, TotalAmount = 150 * 1200, Percentage = 12.5 },
            new CategorySalesViewModel { CategoryName = "Инструменты *", ItemsSold = 90, TotalAmount = 90 * 800, Percentage = 7.5 }
        };
    }

    private List<TopProductViewModel> GetSampleTopProducts()
    {
        return new List<TopProductViewModel>
        {
            new TopProductViewModel { Name = "Саморезы 4.2х75 *", TotalSold = 30, TotalRevenue = 30 * 180, Unit = "уп" },
            new TopProductViewModel { Name = "Плитка керамическая *", TotalSold = 21, TotalRevenue = 21 * 500, Unit = "шт" },
            new TopProductViewModel { Name = "Краска белая 10л *", TotalSold = 13, TotalRevenue = 13 * 1200, Unit = "шт" },
            new TopProductViewModel { Name = "Цемент M500 50кг *", TotalSold = 10, TotalRevenue = 10 * 350, Unit = "шт" },
            new TopProductViewModel { Name = "Гипсокартон 12.5мм *", TotalSold = 8, TotalRevenue = 8 * 550, Unit = "шт" }
        };
    }
}