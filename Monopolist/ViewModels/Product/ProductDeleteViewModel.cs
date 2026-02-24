namespace Monoplist.ViewModels;

public class ProductDeleteViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Article { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal SalePrice { get; set; }
    public int CurrentStock { get; set; }
    public string SupplierName { get; set; } = string.Empty;
}