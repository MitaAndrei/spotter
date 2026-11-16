using System.ComponentModel.DataAnnotations;

namespace gymappAPI.DTOs;

public class SignUpCredentials
{
    [StringLength(50, MinimumLength = 3)]
    public string Username { get; set; } = string.Empty;

    [StringLength(100, MinimumLength = 3)]
    public string Password { get; set; } = string.Empty;
}