using System.ComponentModel.DataAnnotations;

namespace Monoplist.ViewModels;

public class SupplierCreateViewModel
{
    [Required(ErrorMessage = "Название поставщика обязательно.")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "От 2 до 200 символов.")]
    [Display(Name = "Название")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Максимум 500 символов.")]
    [Display(Name = "Контактная информация")]
    public string? ContactInfo { get; set; }
}