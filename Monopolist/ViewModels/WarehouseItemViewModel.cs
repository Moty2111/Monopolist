using System.ComponentModel.DataAnnotations;

namespace Monoplist.ViewModels;

public class WarehouseViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? ImageUrl { get; set; }
    public string? Description { get; set; }
    public int Capacity { get; set; }
    public int CurrentOccupancy { get; set; }
    public int ProductsCount { get; set; }
    public List<string> ProductNames { get; set; } = new();
}

public class WarehouseDetailViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? ImageUrl { get; set; }
    public string? Description { get; set; }
    public int Capacity { get; set; }
    public int CurrentOccupancy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<ProductInfoViewModel> Products { get; set; } = new();
}

public class ProductInfoViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Article { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Supplier { get; set; } = string.Empty;
    public int CurrentStock { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal PurchasePrice { get; set; }
    public decimal SalePrice { get; set; }
}

public class WarehouseEditViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Название склада обязательно")]
    [Display(Name = "Название")]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Местоположение")]
    [StringLength(200)]
    public string? Location { get; set; }

    [Display(Name = "URL изображения")]
    [Url(ErrorMessage = "Введите корректный URL")]
    public string? ImageUrl { get; set; }

    [Display(Name = "Описание")]
    [StringLength(500)]
    public string? Description { get; set; }

    [Display(Name = "Вместимость")]
    [Range(1, 10000, ErrorMessage = "Вместимость должна быть от 1 до 10000")]
    public int Capacity { get; set; }

    // Список ID выбранных товаров
    public List<int> SelectedProductIds { get; set; } = new();

    // Все доступные товары для выбора
    public List<ProductSelectItem> AvailableProducts { get; set; } = new();
}

public class ProductSelectItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Article { get; set; }
    public int CurrentStock { get; set; }
    public string Unit { get; set; } = string.Empty;
    public bool IsSelected { get; set; }
}