using gymappAPI.DTOs;
using gymappAPI.Models;

namespace gymappAPI.Managers;

public interface IUserManager
{
    Task<User?> GetUserByIdAsync(Guid id);
    
    // The logged-in user. Throws UnauthorizedAccessException (401) if there isn't one.
    Task<User> GetCurrentUserAsync();
    
    Task<UserDto[]> SearchUsersAsync(string searchString);
    
    Task<User?> GetUserByUsernameAsync(string username);
    
    Task<UserProfileDto?> GetProfileByUsernameAsync(string username);
    
    Task UpdateProfileImageAsync(User user, string? fileKey);
}