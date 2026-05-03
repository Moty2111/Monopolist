using System.ComponentModel.DataAnnotations;

namespace Monopolist.ViewModels.User;


/// Модель для передачи данных при смене роли.

public class ChangeRoleViewModel
{
    [Required]
    public int UserId { get; set; }

    [Required]
    [StringLength(20)]
    public string Role { get; set; } = string.Empty;
}