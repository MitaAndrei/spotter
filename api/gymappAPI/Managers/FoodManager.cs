using gymappAPI.Data;
using gymappAPI.DTOs;
using gymappAPI.Errors;
using gymappAPI.Models;
using gymappAPI.Services;
using Microsoft.EntityFrameworkCore;

namespace gymappAPI.Managers;

public class FoodManager : IFoodManager
{

    private readonly AppDbContext _context;
    private readonly IUserManager _userManager;
    private readonly ICacheService _cache;

    private static readonly TimeSpan FoodCatalogTtl = TimeSpan.FromHours(1);
    
    public FoodManager(AppDbContext context, IUserManager userManager, ICacheService cache)
    {
        _context = context;
        _userManager = userManager;
        _cache = cache;
    }
    public async Task CreateAsync(Models.Food food)
    {
        _context.Foods.Add(food);
        await _context.SaveChangesAsync();
        await _cache.RemoveAsync(CacheKeys.FoodCatalog);
    }

    public async Task CreateLoggedFoodsAsync(LoggedFood[] loggedFoods)
    {
        _context.LoggedFoods.AddRange(loggedFoods);
        await _context.SaveChangesAsync();
    }

    public async Task<LoggedFood[]> ConvertToLoggedFoodsAsync(FoodGramsPair[] foods, DateTime date)
    {
        var user = await _userManager.GetCurrentUserAsync();
        
        var foodIds = foods.Select(f => f.Food.Id).Distinct().ToArray();
        var foodsById = await _context.Foods
            .Where(f => foodIds.Contains(f.Id))
            .ToDictionaryAsync(f => f.Id);

        return foods.Select(f => new LoggedFood
        {
            Id = Guid.NewGuid(),
            Date = date.Date,
            Food = foodsById.GetValueOrDefault(f.Food.Id)
                   ?? throw new NotFoundException($"Food {f.Food.Id} not found"),
            Grams = f.Grams,
            User = user
        }).ToArray();
    }

    public async Task<LoggedFood[]> GetAllInDateRangeAsync(Guid userId, DateTime start, DateTime end)
    {
        var user = await _userManager.GetUserByIdAsync(userId);
        // start and end are calendar days, and end is included.
        var from = start.Date;
        var toExclusive = end.Date.AddDays(1);
        return await _context.LoggedFoods.Include(f => f.Food)
            .Where(f => 
                f.User == user &&
                f.Date >= from &&
                f.Date < toExclusive).ToArrayAsync();
    }

    public async Task<LoggedFood[]> GetAllForDateAsync(DateTime date)
    {
        var user = await _userManager.GetCurrentUserAsync();
        return await GetAllInDateRangeAsync(user.Id, date, date);
    }

    public async Task<bool> DeleteLoggedFoodAsync(Guid id)
    {
        var loggedFood = await FindOwnLoggedFoodAsync(id);
        if (loggedFood is null)
            return false;

        _context.LoggedFoods.Remove(loggedFood);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> EditLoggedFoodGramsAsync(Guid id, double grams)
    {
        var loggedFood = await FindOwnLoggedFoodAsync(id);
        if (loggedFood is null)
            return false;

        loggedFood.Grams = grams;
        await _context.SaveChangesAsync();
        return true;
    }

    private async Task<LoggedFood?> FindOwnLoggedFoodAsync(Guid id)
    {
        var user = await _userManager.GetCurrentUserAsync();
        return await _context.LoggedFoods.FirstOrDefaultAsync(f => f.Id == id && f.User.Id == user.Id);
    }

    public async Task<Models.Food?> GetFoodByIdAsync(Guid foodId)
    {
        return await _context.Foods.FindAsync(foodId);
    }

    public async Task<Models.Food[]> GetAllFoodAsync()
    {
        return await _cache.GetOrSetAsync(CacheKeys.FoodCatalog,
            () => _context.Foods.AsNoTracking().ToArrayAsync(), FoodCatalogTtl);
    }
}