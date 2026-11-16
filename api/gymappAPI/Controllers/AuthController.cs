using gymappAPI.DTOs;
using gymappAPI.Managers;
using gymappAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace gymappAPI.Controllers;

[Route("api/auth")]
[ApiController]
public class AuthController : ControllerBase
{
    public const string AuthRateLimitPolicy = "auth";

    private readonly IAuthManager _authManager;

    public AuthController(IAuthManager authManager)
    {
        _authManager = authManager;
    }
    
    [HttpPost("register")]
    [EnableRateLimiting(AuthRateLimitPolicy)]
    public async Task<IActionResult> Register(SignUpCredentials request)
    {
        var user = await _authManager.RegisterAsync(request);

        if (user is null)
            return BadRequest( new { message = "Username already exists" } );

        return Ok();
    }
    
    [HttpPost("login")]
    [EnableRateLimiting(AuthRateLimitPolicy)]
    public async Task<ActionResult<TokenResponseDto>> Login(LoginCredentials request)
    {
        var tokenResponse = await _authManager.LoginAsync(request);
        if (tokenResponse is null)
            return BadRequest(new { message = "Invalid username or password."});

        return Ok(tokenResponse);
    }
    
    [HttpPost("refresh")]
    public async Task<ActionResult<TokenResponseDto>> RefreshToken(RefreshTokenRequestDto request)
    {
        var tokenResponse = await _authManager.RefreshTokensAsync(request);
        if (tokenResponse is null || tokenResponse.AccessToken is null || tokenResponse.RefreshToken is null)
            return Unauthorized("Invalid refresh token.");

        return Ok(tokenResponse);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(RefreshTokenRequestDto request)
    {
        await _authManager.LogoutAsync(request);
        return NoContent();
    }
}
