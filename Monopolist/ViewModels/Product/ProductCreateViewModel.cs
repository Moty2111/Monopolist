using System.ComponentModel.DataAnnotations;

namespace Monoplist.ViewModels;

public class ProductCreateViewModel
{
    [Required(ErrorMessage = "Наименование обязательно.")]
    [StringLength(200, MinimumLength = 3, ErrorMessage = "От 3 до 200 символов.")]
    [Display(Name = "Наименование")]
    public string Name { get; set; } = string.Empty;

    [StringLength(50, ErrorMessage = "Максимум 50 символов.")]
    [Display(Name = "Артикул")]
    public string? Article { get; set; }

    [Required(ErrorMessage = "Выберите категорию.")]
    [Display(Name = "Категория")]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Единица измерения обязательна.")]
    [StringLength(20, ErrorMessage = "Максимум 20 символов.")]
    [Display(Name = "Единица измерения")]
    public string Unit { get; set; } = "шт";

    [Required(ErrorMessage = "Закупочная цена обязательна.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Цена должна быть больше 0.")]
    [Display(Name = "Цена закупки")]
    public decimal PurchasePrice { get; set; }

    [Required(ErrorMessage = "Цена продажи обязательна.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Цена должна быть больше 0.")]
    [Display(Name = "Цена продажи")]
    public decimal SalePrice { get; set; }

    [Required(ErrorMessage = "Остаток обязателен.")]
    [Range(0, int.MaxValue, ErrorMessage = "Остаток не может быть отрицательным.")]
    [Display(Name = "Текущий остаток")]
    public int CurrentStock { get; set; }

    [Display(Name = "Поставщик")]
    public int? SupplierId { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Минимальный запас не может быть отрицательным.")]
    [Display(Name = "Минимальный запас")]
    public int MinimumStock { get; set; } = 10;
}