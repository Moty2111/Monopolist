using System.ComponentModel.DataAnnotations;

namespace Monoplist.ViewModels;

public class ReportCardViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public List<string> AvailableFormats { get; set; } = new();
}

public class SalesReportViewModel
{
    [Display(Name = "Дата начала")]
    public DateTime StartDate { get; set; } = DateTime.Now.AddMonths(-1).Date;

    [Display(Name = "Дата окончания")]
    public DateTime EndDate { get; set; } = DateTime.Now.Date;

    public decimal TotalRevenue { get; set; }
    public int TotalOrders { get; set; }
    public decimal AverageOrderValue { get; set; }
    public List<DailySalesViewModel> DailySales { get; set; } = new();
    public List<CategorySalesViewModel> CategorySales { get; set; } = new();
    public List<PaymentMethodViewModel> PaymentMethods { get; set; } = new();
    public List<OrderInfoViewModel> RecentOrders { get; set; } = new();
}

public class DailySalesViewModel
{
    public DateTime Date { get; set; }
    public decimal Revenue { get; set; }
    public int OrdersCount { get; set; }
    public string DayName { get; set; } = string.Empty;

    // Алиас для совместимости с главной панелью
    public decimal Total => Revenue;
}

public class CategorySalesViewModel
{
    public string CategoryName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public int ItemsSold { get; set; }
    public double Percentage { get; set; }

    // Алиас для совместимости с главной панелью
    public int TotalSold => ItemsSold;
}

public class PaymentMethodViewModel
{
    public string Method { get; set; } = string.Empty;
    public string MethodDisplay { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Total { get; set; }
}

public class OrderInfoViewModel
{
    public DateTime OrderDate { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
}

public class ProductsReportViewModel
{
    public int TotalProducts { get; set; }
    public int LowStockCount { get; set; }
    public int OutOfStockCount { get; set; }
    public decimal TotalInventoryValue { get; set; }
    public List<ProductStockViewModel> TopProducts { get; set; } = new();
    public List<ProductStockViewModel> LowStockProducts { get; set; } = new();
    public List<CategoryStockViewModel> CategoryStock { get; set; } = new();
}

public class ProductStockViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Article { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int CurrentStock { get; set; }
    public int MinimumStock { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal SalePrice { get; set; }
    public decimal StockValue => CurrentStock * PurchasePrice;
    public string Status => CurrentStock == 0 ? "Отсутствует" :
                            CurrentStock < MinimumStock ? "Малый остаток" : "Норма";
}

public class CategoryStockViewModel
{
    public string CategoryName { get; set; } = string.Empty;
    public int ProductsCount { get; set; }
    public int TotalStock { get; set; }
    public decimal TotalValue { get; set; }
}

public class CustomersReportViewModel
{
    public int TotalCustomers { get; set; }
    public int NewCustomersThisMonth { get; set; }
    public int ActiveCustomers { get; set; }
    public List<TopCustomerViewModel> TopCustomers { get; set; } = new();
    public List<CustomerRegistrationViewModel> Registrations { get; set; } = new();
}

public class TopCustomerViewModel
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public decimal TotalSpent { get; set; }
    public int OrdersCount { get; set; }
    public DateTime LastOrderDate { get; set; }
}

public class CustomerRegistrationViewModel
{
    public string Period { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class WarehouseReportViewModel
{
    public int TotalWarehouses { get; set; }
    public int TotalCapacity { get; set; }
    public int CurrentOccupancy { get; set; }
    public double OccupancyPercent { get; set; }
    public List<WarehouseOccupancyViewModel> Warehouses { get; set; } = new();
    public List<ProductLocationViewModel> ProductLocations { get; set; } = new();
}

public class WarehouseOccupancyViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public int CurrentOccupancy { get; set; }
    public int ProductsCount { get; set; }
    public double OccupancyPercent => Capacity > 0 ? (CurrentOccupancy * 100.0 / Capacity) : 0;
}

public class ProductLocationViewModel
{
    public string ProductName { get; set; } = string.Empty;
    public string Article { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public int Stock { get; set; }
    public string Unit { get; set; } = string.Empty;
}