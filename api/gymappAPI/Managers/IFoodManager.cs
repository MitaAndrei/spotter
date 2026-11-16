using gymappAPI.DTOs;
using gymappAPI.Models;

namespace gymappAPI.Managers;

public interface IFoodManager
{
    Task CreateAsync(Models.Food food);
    
    Task CreateLoggedFoodsAsync(LoggedFood[] loggedFoods);
    
    Task<Models.Food[]> GetAllFoodAsync();

    Task<Models.Food?> GetFoodByIdAsync(Guid foodId);
    
    Task<LoggedFood[]> ConvertToLoggedFoodsAsync(FoodGramsPair[] foods, DateTime date);
    
    Task<LoggedFood[]> GetAllInDateRangeAsync(Guid userId, DateTime start, DateTime end);
    
    Task<LoggedFood[]> GetAllForDateAsync(DateTime date);
    Task<bool> DeleteLoggedFoodAsync(Guid id);
    Task<bool> EditLoggedFoodGramsAsync(Guid id, double grams);
}