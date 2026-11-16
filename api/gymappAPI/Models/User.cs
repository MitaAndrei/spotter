using System.Text.Json.Serialization;

namespace gymappAPI.Models;

public class User
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    [JsonIgnore]
    public string PasswordHash { get; set; } = string.Empty;
    public string? ProfileImageKey { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}