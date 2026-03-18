using System.Collections.Generic;

namespace Monoplist.ViewModels
{
    public class DashboardViewModel
    {
        // Основные показатели
        public decimal TotalRevenue { get; set; }
        public int ProductsInStock { get; set; }
        public int TotalCustomers { get; set; }
        public int NewOrders { get; set; }

        // Списки для таблиц
        public List<OrderSummaryViewModel> LatestOrders { get; set; } = new();
        public List<LowStockProductViewModel> LowStockProducts { get; set; } = new();

        // Данные для графиков и виджетов
        public List<DailySalesViewModel> SalesData { get; set; } = new();
        public List<CategorySalesViewModel> CategorySales { get; set; } = new();
        public decimal Forecast { get; set; }

        // Новый виджет: топ-5 товаров по продажам
        public List<TopProductViewModel> TopProducts { get; set; } = new();
    }

    // Модель для топ-товаров
    public class TopProductViewModel
    {
        public string Name { get; set; } = string.Empty;
        public int TotalSold { get; set; }
        public decimal TotalRevenue { get; set; }
        public string Unit { get; set; } = string.Empty;
    }
}