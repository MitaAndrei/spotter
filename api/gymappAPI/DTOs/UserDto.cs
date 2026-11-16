namespace gymappAPI.DTOs;

public class UserDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? ProfileImageKey { get; set; }
    public string? ProfileImageUrl { get; set; }
}