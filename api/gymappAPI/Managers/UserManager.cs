using System.Security.Claims;
using gymappAPI.Data;
using gymappAPI.DTOs;
using gymappAPI.Models;
using gymappAPI.Services;
using Microsoft.EntityFrameworkCore;

namespace gymappAPI.Managers;

public class UserManager: IUserManager
{
    private readonly AppDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IS3Service _s3Service;
    private readonly ICacheService _cache;

    private const int MaxSearchResults = 20;
    private static readonly TimeSpan ProfileTtl = TimeSpan.FromMinutes(15);


    public UserManager(AppDbContext context, IHttpContextAccessor httpContextAccessor, IS3Service s3Service,
        ICacheService cache)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
        _s3Service = s3Service;
        _cache = cache;

    }
    
    public async Task<User?> GetUserByIdAsync(Guid id)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);

        return user;
    }

    public async Task<User> GetCurrentUserAsync()
    {
        var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException();

        // A valid token for an account that no longer exists.
        return await GetUserByIdAsync(userId) ?? throw new UnauthorizedAccessException();
    }

    public async Task<UserDto[]> SearchUsersAsync(string searchString)
    {
        var tolower = searchString.ToLower();
        var users = await _context.Users
            .Where(u => u.Username.ToLower().Contains(tolower))
            .OrderBy(u => u.Username)
            .Take(MaxSearchResults)
            .Select(user => new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                ProfileImageKey = user.ProfileImageKey,
                ProfileImageUrl = null
            })
            .ToArrayAsync();
        
        var urlTasks = users
            .Where(f => !string.IsNullOrEmpty(f.ProfileImageKey))
            .Select(async user =>
            {
                user.ProfileImageUrl = await _s3Service.GetPreSignedUrlAsync(user.ProfileImageKey!);
            });

        await Task.WhenAll(urlTasks);
        
        return users;
    }

    public async Task<User?> GetUserByUsernameAsync(string username)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        return user;
    }

    public async Task<UserProfileDto?> GetProfileByUsernameAsync(string username)
    {
        return await _cache.GetOrSetAsync(CacheKeys.UserProfile(username),
            () => _context.Users
                .Where(u => u.Username == username)
                .Select(u => new UserProfileDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    ProfileImageKey = u.ProfileImageKey,
                    CreatedAt = u.CreatedAt
                })
                .FirstOrDefaultAsync(),
            ProfileTtl);
    }

    public async Task UpdateProfileImageAsync(User user, string? fileKey)
    {
        user.ProfileImageKey = fileKey;
        await _context.SaveChangesAsync();
        await _cache.RemoveAsync(CacheKeys.UserProfile(user.Username));
    }
}