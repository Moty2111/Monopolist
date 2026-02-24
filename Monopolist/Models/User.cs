using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Monoplist.Models;

[Index(nameof(Username), IsUnique = true)]
public class User
{
    public int Id { get; set; }

    [Required, StringLength(50, MinimumLength = 3)]
    public string Username { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 4)]
    public string Password { get; set; } = string.Empty;

    [Required, StringLength(20)]
    public string Role { get; set; } = "Seller";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}