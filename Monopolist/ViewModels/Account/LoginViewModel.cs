using System.ComponentModel.DataAnnotations;

namespace Monoplist.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Имя пользователя обязательно.")]
    [Display(Name = "Логин")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Пароль обязателен.")]
    [DataType(DataType.Password)]
    [Display(Name = "Пароль")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Запомнить меня")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}