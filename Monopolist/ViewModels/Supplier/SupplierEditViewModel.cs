using System.ComponentModel.DataAnnotations;

namespace Monopolist.ViewModels.Supplier;

public class SupplierEditViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Название поставщика обязательно")]
    [StringLength(200, ErrorMessage = "Максимум 200 символов")]
    [Display(Name = "Название")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Максимум 500 символов")]
    [Display(Name = "Контактная информация")]
    public string? ContactInfo { get; set; }
}