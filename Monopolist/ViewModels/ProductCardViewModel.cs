// ViewModels/ProductCardViewModel.cs
namespace Monoplist.ViewModels;

public class ProductCardViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Article { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Unit { get; set; } = "шт";
    public decimal SalePrice { get; set; }
    public int CurrentStock { get; set; }
    public string? ImageUrl { get; set; }
    public string? Description { get; set; }
}