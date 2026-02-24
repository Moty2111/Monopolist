using System.ComponentModel.DataAnnotations;

namespace Monoplist.ViewModels;

public class UserEditViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Имя пользователя обязательно.")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "От 3 до 50 символов.")]
    [Display(Name = "Имя пользователя")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Выберите роль.")]
    [Display(Name = "Роль")]
    public string Role { get; set; } = "Seller";
}