using Monoplist.ViewModels;

namespace Monoplist.ViewModels;

public class DashboardViewModel
{
    public decimal TotalRevenue { get; set; }
    public int ProductsInStock { get; set; }
    public int TotalCustomers { get; set; }
    public int NewOrders { get; set; }
    public List<OrderSummaryViewModel> LatestOrders { get; set; } = new();
    public List<LowStockProductViewModel> LowStockProducts { get; set; } = new();
}