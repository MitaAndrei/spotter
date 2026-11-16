using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using gymappAPI.Configuration;
using gymappAPI.Data;
using gymappAPI.DTOs;
using gymappAPI.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Npgsql;

namespace gymappAPI.Managers;

public class AuthManager : IAuthManager
{
    private readonly JwtSettings _jwt;
    private readonly AppDbContext _context;

    // Tabs of the same browser share one refresh token. If they refresh at the same moment, the
    // second request arrives with a token the first just used; accepting it briefly avoids
    // logging both tabs out.
    private static readonly TimeSpan RefreshTokenReuseGracePeriod = TimeSpan.FromSeconds(30);
    
    public AuthManager(IOptions<JwtSettings> jwtOptions, AppDbContext context)
    {
        _jwt = jwtOptions.Value;
        _context = context;
    }
    
    public async Task<TokenResponseDto?> LoginAsync(LoginCredentials request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
        if (user is null)
        {
            return null;
        }
        if (new PasswordHasher<User>().VerifyHashedPassword(user, user.PasswordHash, request.Password)
                == PasswordVerificationResult.Failed)
        {
            return null;
        }

        return await CreateTokenResponse(user);
    }

    public async Task<User?> RegisterAsync(SignUpCredentials request)
    {
        if (await _context.Users.AnyAsync(u => u.Username == request.Username))
        {
            return null;
        }

        var user = new User();
        var hashedPassword = new PasswordHasher<User>()
                .HashPassword(user, request.Password);

        user.Username = request.Username;
        user.PasswordHash = hashedPassword;

        _context.Users.Add(user);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Someone registered the same username between the check above and this insert.
            return null;
        }

        return user;
    }
        
    public async Task<TokenResponseDto?> RefreshTokensAsync(RefreshTokenRequestDto request)
    {
        var tokenHash = HashToken(request.RefreshToken);
        var storedToken = await _context.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash);

        var now = DateTime.UtcNow;
        if (storedToken is null || storedToken.ExpiresAt <= now)
            return null;

        // Rotation: a used token is replaced by a new one and stops working after the grace period.
        if (storedToken.UsedAt is { } usedAt && now - usedAt > RefreshTokenReuseGracePeriod)
            return null;

        storedToken.UsedAt ??= now;
        return await CreateTokenResponse(storedToken.User);
    }

    public async Task LogoutAsync(RefreshTokenRequestDto request)
    {
        var tokenHash = HashToken(request.RefreshToken);
        await _context.RefreshTokens.Where(t => t.TokenHash == tokenHash).ExecuteDeleteAsync();
    }
    
    private async Task<TokenResponseDto> CreateTokenResponse(User user)
    {
        return new TokenResponseDto
        {
            AccessToken = CreateToken(user),
            RefreshToken = await GenerateAndSaveRefreshTokenAsync(user)
        };
    }

    private static string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    private async Task<string> GenerateAndSaveRefreshTokenAsync(User user)
    {
        // Housekeeping: drop this user's expired tokens (devices that never came back)
        // and used tokens whose grace period is over.
        var now = DateTime.UtcNow;
        var usedCutoff = now - RefreshTokenReuseGracePeriod;
        await _context.RefreshTokens
            .Where(t => t.UserId == user.Id && (t.ExpiresAt <= now || (t.UsedAt != null && t.UsedAt < usedCutoff)))
            .ExecuteDeleteAsync();

        var refreshToken = GenerateRefreshToken();
        _context.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = HashToken(refreshToken),
            ExpiresAt = now.AddDays(_jwt.RefreshTokenDays)
        });
        await _context.SaveChangesAsync();
        return refreshToken;
    }

    private static string HashToken(string token)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
        
    private string CreateToken(User user)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
        };
        
        var token = new JwtSecurityToken(_jwt.Issuer,
                _jwt.Audience,
                claims,
                expires: DateTime.UtcNow.AddMinutes(_jwt.AccessTokenMinutes),
                signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}