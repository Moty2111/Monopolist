namespace Monoplist.ViewModels;

public class SupplierDeleteViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ContactInfo { get; set; } = string.Empty;
    public int ProductsCount { get; set; }
    public bool HasProducts { get; set; }
}