// ViewModels/SupplierViewModels.cs
using System.ComponentModel.DataAnnotations;

namespace Monoplist.ViewModels;

public class SupplierIndexViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ContactInfo { get; set; } = string.Empty;
    public int ProductsCount { get; set; }
}

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

public class SupplierCreateViewModel
{
    [Required(ErrorMessage = "Название обязательно")]
    [StringLength(200, MinimumLength = 2)]
    [Display(Name = "Название")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Контактная информация")]
    [StringLength(500)]
    public string? ContactInfo { get; set; }

    public List<int> SelectedProductIds { get; set; } = new();
    public List<SupplierProductSelectItem> AvailableProducts { get; set; } = new();
}

public class SupplierEditViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Название обязательно")]
    [StringLength(200, MinimumLength = 2)]
    [Display(Name = "Название")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Контактная информация")]
    [StringLength(500)]
    public string? ContactInfo { get; set; }

    public List<int> SelectedProductIds { get; set; } = new();
    public List<SupplierProductSelectItem> AvailableProducts { get; set; } = new();
}

public class SupplierDeleteViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ContactInfo { get; set; } = string.Empty;
    public int ProductsCount { get; set; }
    public bool HasProducts { get; set; }
}

// Переименовано для избежания конфликтов
public class SupplierProductSelectItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Article { get; set; }
    public int CurrentStock { get; set; }
    public string Unit { get; set; } = string.Empty;
    public bool IsSelected { get; set; }
}