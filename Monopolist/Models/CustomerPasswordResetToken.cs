using System;

namespace Monoplist.Models;

public class CustomerPasswordResetToken
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; }

    public virtual Customer Customer { get; set; } = null!;
}