using Monoplist.Models;

public class UserSession
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string SessionId { get; set; } = string.Empty;
    public string? DeviceInfo { get; set; }
    public string? BrowserInfo { get; set; }
    public string? IpAddress { get; set; }
    public DateTime LoginTime { get; set; }
    public DateTime LastActivityTime { get; set; }
    public bool IsActive { get; set; } = true;
}