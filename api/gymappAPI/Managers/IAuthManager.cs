using gymappAPI.DTOs;
using gymappAPI.Models;

namespace gymappAPI.Managers;

public interface IAuthManager
{ 
    Task<User?> RegisterAsync(SignUpCredentials request);
    Task<TokenResponseDto?> LoginAsync(LoginCredentials request);
    Task<TokenResponseDto?> RefreshTokensAsync(RefreshTokenRequestDto request);
    Task LogoutAsync(RefreshTokenRequestDto request);
    
}