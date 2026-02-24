namespace Monoplist.ViewModels;

public class SupplierDetailsViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ContactInfo { get; set; } = string.Empty;
    public List<SupplierProductViewModel> Products { get; set; } = new();
}

public class SupplierProductViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int CurrentStock { get; set; }
}