namespace gymappAPI.Models;

/// <summary>One per logged-in device. Only a hash is stored; the token itself lives on the client.</summary>
public class RefreshToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    // When the token was exchanged for a new one; it stays valid for a short grace period after that.
    public DateTime? UsedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
