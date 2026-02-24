using System.ComponentModel.DataAnnotations;

namespace Monoplist.ViewModels;

public class ChangePasswordViewModel
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Новый пароль обязателен.")]
    [StringLength(100, MinimumLength = 4, ErrorMessage = "Пароль должен быть не менее 4 символов.")]
    [DataType(DataType.Password)]
    [Display(Name = "Новый пароль")]
    public string NewPassword { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Подтверждение пароля")]
    [Compare("NewPassword", ErrorMessage = "Пароли не совпадают.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}