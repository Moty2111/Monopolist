using System.ComponentModel.DataAnnotations;

namespace Monopolist.ViewModels.User;

/// <summary>
/// Модель для передачи данных при смене роли.
/// </summary>
public class ChangeRoleViewModel
{
    [Required]
    public int UserId { get; set; }

    [Required]
    [StringLength(20)]
    public string Role { get; set; } = string.Empty;
}