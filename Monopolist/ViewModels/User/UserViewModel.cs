namespace Monopolist.ViewModels.User;

/// <summary>
/// Модель для отображения пользователя в списке (без пароля).
/// </summary>
public class UserViewModel
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}