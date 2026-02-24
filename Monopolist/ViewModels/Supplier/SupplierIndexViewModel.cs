namespace Monoplist.ViewModels;

public class SupplierIndexViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ContactInfo { get; set; } = string.Empty;
    public int ProductsCount { get; set; }
}