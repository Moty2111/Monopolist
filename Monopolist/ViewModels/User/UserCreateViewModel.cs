using System.ComponentModel.DataAnnotations;

namespace Monoplist.ViewModels;

public class UserCreateViewModel
{
    [Required(ErrorMessage = "Имя пользователя обязательно.")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "От 3 до 50 символов.")]
    [Display(Name = "Имя пользователя")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Пароль обязателен.")]
    [StringLength(100, MinimumLength = 4, ErrorMessage = "Пароль должен быть не менее 4 символов.")]
    [DataType(DataType.Password)]
    [Display(Name = "Пароль")]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Подтверждение пароля")]
    [Compare("Password", ErrorMessage = "Пароли не совпадают.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Выберите роль.")]
    [Display(Name = "Роль")]
    public string Role { get; set; } = "Seller";
}