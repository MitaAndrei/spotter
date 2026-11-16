namespace gymappAPI.DTOs;

public class UserProfileDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? ProfileImageKey { get; set; }
    public DateTime CreatedAt { get; set; }
}
