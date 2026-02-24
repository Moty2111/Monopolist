namespace Monoplist.ViewModels;

public class LowStockProductViewModel
{
    public string Name { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public int CurrentStock { get; set; }
    public int MinimumStock { get; set; }
}